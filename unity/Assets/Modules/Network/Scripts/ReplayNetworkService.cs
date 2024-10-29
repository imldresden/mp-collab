using IMLD.MixedReality.Core;
using IMLD.MixedReality.Utils;
using Microsoft.MixedReality.OpenXR.Remoting;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    /// <summary>
    /// ToDo: The idea here is that the ReplayNetworkService can do two things:
    /// 1. When used as a client, it can be used as a drop-in replacement for the regular NetworkService,
    /// reading "network" packets from file instead of receiving them over network. This way, a local replay session can be supported without changing all receivers.
    /// 2. When used as a server, it transmits pre-recorded data to the connected clients, allowing networked replay sessions.
    /// Again, the receiving side does not need to be changed.
    /// </summary>
    public class ReplayNetworkService : MonoBehaviour, INetworkService
    {
        public NetworkServiceDescription.ServiceType ServiceType
        {
            get { return _serviceDescription.Type; }
            set { _serviceDescription.Type = value; }
        }

        public NetworkServiceDescription ServiceDescription
        {
            get { return _serviceDescription; }
        }

        public string ServiceData
        {
            get { return _serviceDescription.Data; }
            private set { _serviceDescription.Data = value; }
        }

        public INetworkService.NetworkServiceRole Role { get; private set; } = INetworkService.NetworkServiceRole.UNDEFINED;

        public long FirstTimestamp { get { return _firstTimestampHeader; } }
        public long LastTimestamp { get { return _lastTimestampHeader; } }

        private const int LENGTH_LONG = 8;
        private const int LENGTH_INT = 4;
        private const string MAGIC_STRING = "IML!";
        private const int VERSION = 1;

        private NetworkTransport _socket;
        private Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>> _messageHandlers = new Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>>();
        private NetworkServiceDescription _serviceDescription = new NetworkServiceDescription { HostName = "Unknown Host", IP = "", Port = 0, Type = NetworkServiceDescription.ServiceType.UNDEFINED };
        private ISessionManager _sessionManager;
        private List<MessageContainer.MessageType> _unknownMessageTypes = new List<MessageContainer.MessageType>();

        private FileStream _fileStream;
        private long _firstTimestampHeader;
        private long _lastTimestampHeader;
        private long _firstTimestampFile = 0;
        private long _latestTimestampFile = 0;
        private long _absoluteTime = 0;
        private bool _clientIsRunning = false;
        
        private MessageContainer _messageCache;
        private bool _messageSent;
        private Task _readerTask;
        private long _prevTime = 0;
        private PlaybackControl _playbackControl;

        private ConcurrentQueue<MessageContainerWrapper> _messageQueue = new ConcurrentQueue<MessageContainerWrapper>();

        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;
        public event ClientEventHandler ClientReappeared;
        public event ClientEventHandler ClientDisappeared;
        public event EventHandler ConnectedToServer;
        public event EventHandler DisconnectedFromServer;

        public INetworkFilter NetworkFilter { get; set; }

        public float ServerLatency => throw new NotImplementedException(); // ToDo: I don't know yet how to handle this in a replay

        public float ServerTimeOffset => throw new NotImplementedException(); // ToDo: I don't know yet how to handle this in a replay

        public INetworkService.NetworkServiceStatus Status { get; set; } = INetworkService.NetworkServiceStatus.UNDEFINED;
        public float RequestedLatency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private void OnDestroy()
        {
            Destroy();
        }

        private void Start()
        {
            _playbackControl = ServiceLocator.Instance.Get<PlaybackControl>();
        }

        public void Destroy()
        {
            _clientIsRunning = false;
            if (Status != INetworkService.NetworkServiceStatus.UNDEFINED)
            {
                Status = INetworkService.NetworkServiceStatus.DISCONNECTED;
            }

            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream = null;
            }

            if (NetworkFilter != null)
            {
                NetworkFilter.Dispose();
                NetworkFilter = null;
            }

            if (_socket != null)
            {
                _socket.StopServer();
                Destroy(_socket);
                _socket = null;
            }

            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler)
        {
            try
            {
                _messageHandlers[messageType] = messageHandler;
            }
            catch (Exception exp)
            {
                Debug.LogError("Registering message handler failed! Original error message: " + exp.Message);
                return false;
            }
            return true;
        }

        public void SendMessage(IMessage message) { } // this service only sends messages internally

        public bool StartAsClient(NetworkServiceDescription serviceDescription, bool useMessageQueue = true)
        {
            // Check prerequisites
            if (!enabled)
            {
                Debug.Log("Component disabled!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                Status = INetworkService.NetworkServiceStatus.DISCONNECTED;
                return false;
            }

            if (_fileStream == null || _fileStream.CanRead == false)
            {
                Debug.Log("Cannot read from file");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                Status = INetworkService.NetworkServiceStatus.DISCONNECTED;
                return false;
            }   

            if (_playbackControl == null )
            {
                Debug.Log("No playback control found");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                Status = INetworkService.NetworkServiceStatus.DISCONNECTED;
                return false;
            }

            _clientIsRunning = true;
            Role = INetworkService.NetworkServiceRole.CLIENT;
            Status = INetworkService.NetworkServiceStatus.CONNECTED;
            _readerTask = Task.Run(() => ReadMessagesTask());

            return true;
        }

        public bool StartAsServer(bool useMessageQueue = true)
        {
            // Check prerequisites
            if (!enabled)
            {
                Debug.Log("Component disabled, cannot start server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }

            if (_socket == null)
            {
                Debug.Log("Network transport not ready, cannot start server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }
            
            if (_playbackControl == null)
            {
                Debug.Log("No playback control found");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }

            if (_sessionManager == null)
            {
                Debug.Log("No Network Service Manager found, cannot start server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }

            // Start server
            Debug.Log("Starting as server");
            _serviceDescription.RoomId = _sessionManager.Room.Id;
            _serviceDescription.Data = ServiceData;
            _serviceDescription.ServiceId = Guid.NewGuid();
            _serviceDescription.SessionId = _sessionManager.SessionId;
            bool result = _socket.StartServer(_serviceDescription);

            // Update service description
            if (result)
            {
                _serviceDescription = _socket.ServiceDescription;
                Role = INetworkService.NetworkServiceRole.SERVER;
            }
            else
            {
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
            }

            return result;
        }

        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType) { throw new NotImplementedException(); }

        public void UpdateRoom(RoomDescription room)
        {
            throw new NotImplementedException();
        }

        public void SetFile(string filePath)
        {
            _playbackControl = ServiceLocator.Instance.Get<PlaybackControl>();

            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream = null;
            }

            if (System.IO.File.Exists(filePath))
            {
                _fileStream = File.OpenRead(filePath);
            }

            InitializeFromFileHeader();
        }

        async void Update()
        {
            if (_clientIsRunning == false)
            {
                return;
            }

            if (_firstTimestampFile == 0)
            {
                if (_messageQueue.TryPeek(out var wrapper))
                {
                    _firstTimestampFile = wrapper.Timestamp; // first timestamp in file
                }
                else
                {
                    return;
                }
            }

            _absoluteTime = _playbackControl.AbsoluteTimestamp;

            while (_latestTimestampFile <= _absoluteTime)
            {
                if (_messageQueue.TryPeek(out var wrapper))
                {
                    if (wrapper.Timestamp <= _absoluteTime && _messageQueue.TryDequeue(out wrapper))
                    {
                        _latestTimestampFile = wrapper.Timestamp;
                        Debug.Log(Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType) + "out: " + _messageQueue.Count);
                        HandleMessage(wrapper.Message);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            //Debug.Log("frame time exceeded: " + timestamp + " >= " + targetTimestamp);

            //if (_clientIsRunning == false)
            //{
            //    return;
            //}

            //long targetTimestamp = GetTimestamp();
            //Debug.Log("Sending " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType),ServiceType) + " messages between " + new DateTime(_timestamp).ToLongTimeString() + "." + new DateTime(_timestamp).Millisecond + " and " + new DateTime(targetTimestamp).ToLongTimeString() + "." + new DateTime(targetTimestamp).Millisecond);


        }

        private void HandleMessage(MessageContainer message)
        {
            Debug.Log("Read message from file of type " + Enum.GetName(typeof(MessageContainer.MessageType), message.Type) + ", timestamp: " + new DateTime(_latestTimestampFile).ToLongTimeString() + "." + new DateTime(_latestTimestampFile).Millisecond);

            if (Role == INetworkService.NetworkServiceRole.SERVER)
            {
                SendMessage(message);
            }
            else if (Role == INetworkService.NetworkServiceRole.CLIENT)
            {
                if (_messageHandlers != null)
                {
                    Func<MessageContainer, Task> callback;
                    if (_messageHandlers.TryGetValue(message.Type, out callback) && callback != null)
                    {
                        callback(message);
                    }
                    else
                    {
                        if (_unknownMessageTypes.Contains(message.Type) == false)
                        {
                            Debug.LogWarning("Unknown message: " + message.Type.ToString() + ". No further warning for this type of message will be locked by this Network Service.");
                            _unknownMessageTypes.Add(message.Type);
                        }
                    }
                }
            }
        }

        private void ReadMessagesTask()
        {
            while(_clientIsRunning)
            {
                if (_messageQueue.Count < 10)
                {
                    try
                    {
                        long timestamp;
                        var message = ReadMessageFromFile(out timestamp);
                        _messageQueue.Enqueue(new MessageContainerWrapper(message, timestamp));
                        Debug.Log(Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType) + "in: " + _messageQueue.Count);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
        }

        public void SendMessage(MessageContainer container)
        {
            if (_socket == null)
            {
                Debug.LogError("Cannot send message, socket is null.");
                return;
            }

            if (_socket.NumberOfPeers == 0)
            {
                return;
            }

            if (Role == INetworkService.NetworkServiceRole.SERVER)
            {
                _socket.SendToAll(container);
            }
            else
            {
                Debug.LogWarning("Sending message failed. ReplayNetworkService can only send messages as server.");
            }
        }

        private void InitializeFromFileHeader()
        {
            if (_fileStream != null && _fileStream.CanRead)
            {
                // read magic string
                string magicNumber = ReadStringFromStream(4);
                if (magicNumber == null || magicNumber != MAGIC_STRING)
                {
                    Debug.LogError("Wrong file format: Unsupported format.");
                    return;
                }

                // read version number
                int version = ReadIntFromStream();
                if (version != VERSION)
                {
                    Debug.LogError("Wrong file format: Unsupported version.");
                    return;
                }

                // read first timestamp
                _firstTimestampHeader = ReadLongFromStream();
                if (_firstTimestampHeader == 0L)
                {
                    Debug.LogError("Wrong file format: Corrupted header.");
                    return;
                }

                // read last timestamp
                _lastTimestampHeader = ReadLongFromStream();
                if (_lastTimestampHeader == 0L)
                {
                    Debug.LogError("Wrong file format: Corrupted header.");
                    return;
                }

                // read session id
                int length = ReadIntFromStream();
                ServiceDescription.SessionId = Guid.Parse(ReadStringFromStream(length));

                // read service id
                length = ReadIntFromStream();
                ServiceDescription.ServiceId = Guid.Parse(ReadStringFromStream(length));

                // read service type
                length = ReadIntFromStream();
                ServiceType = (NetworkServiceDescription.ServiceType)ReadIntFromStream();

                // read room id
                length = ReadIntFromStream();
                ServiceDescription.RoomId = ReadIntFromStream();

                // read service auxilliary data
                length = ReadIntFromStream();
                ServiceDescription.Data = ReadStringFromStream(length);

                if (_playbackControl != null)
                {
                    _playbackControl.FirstTimestamp = Math.Min(_playbackControl.FirstTimestamp, _firstTimestampHeader);
                    _playbackControl.LastTimestamp = Math.Max(_playbackControl.LastTimestamp, _lastTimestampHeader);
                }
            }
        }

        private MessageContainer ReadMessageFromFile(out long timestamp)
        {
            // read timestamp
            timestamp = ReadLongFromStream();

            // read message type
            MessageContainer.MessageType type = (MessageContainer.MessageType)ReadIntFromStream();

            // read message length
            int length = ReadIntFromStream();

            // read message data
            byte[] message = ReadBytesFromStream(length);
            
            return MessageContainer.Deserialize(null, message, (byte)type, timestamp);
        }

        private int ReadIntFromStream()
        {
            byte[] bytesInt = new byte[LENGTH_INT];
            int bytesRead = _fileStream.Read(bytesInt, 0, LENGTH_INT);
            if (bytesRead != LENGTH_INT)
            {
                throw new Exception("Unexpected End of File");
            }

            return BitConverter.ToInt32(bytesInt, 0);
        }

        private long ReadLongFromStream()
        {
            byte[] bytesLong = new byte[LENGTH_LONG];
            int bytesRead = _fileStream.Read(bytesLong, 0, LENGTH_LONG);
            if (bytesRead != LENGTH_LONG)
            {
                throw new Exception("Unexpected End of File");
            }

            return BitConverter.ToInt64(bytesLong, 0);
        }

        private string ReadStringFromStream(int length)
        {
            return Encoding.UTF8.GetString(ReadBytesFromStream(length));
        }

        private byte[] ReadBytesFromStream(int length)
        {
            byte[] bytes = new byte[length];
            int bytesRead = _fileStream.Read(bytes, 0, length);
            if (bytesRead != length)
            {
                throw new Exception("Unexpected End of File");
            }

            return bytes;
        }

        public class MessageContainerWrapper
        {
            public MessageContainer Message;
            public long Timestamp;

            public MessageContainerWrapper(MessageContainer message, long timestamp)
            {
                Message = message;
                Timestamp = timestamp;
            }
        }
    }
}