// ------------------------------------------------------------------------------------
// <copyright file="NetworkTransport.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using IMLD.MixedReality.Core;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

namespace IMLD.MixedReality.Network
{
    /// <summary>
    /// This Unity component serves as a layer between the high-level, application specific <see cref="NetworkManager"/> and the low-level network classes.
    /// </summary>
    public class NetworkTransport : MonoBehaviour, INetworkClient, INetworkServer
    {
        //public delegate void SocketEventHandler(object sender, IPEndPoint endpoint);

        public event SocketEventHandler ClientConnectionEstablished;
        public event SocketEventHandler ClientConnectionClosed;
        public event MessageEventHandler MessageReceived;
        public event EventHandler ConnectedToServer;
        public event EventHandler DisconnectedFromServer;

        public int NumberOfPeers
        {
            get
            {
                if (_server != null)
                {
                    return _server.NumberOfConnections;
                }
                else if (IsConnected)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool UseMessageQueue = true;

        private ServerTcpNew _server;
        public string Network { get; set; }
        public NetworkServiceDescription ServiceDescription { get; set; } = new NetworkServiceDescription();

        private const int MESSAGE_HEADER_LENGTH = MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH + MESSAGE_TIMESTAMP_LENGTH;
        private const int MESSAGE_SIZE_LENGTH = sizeof(int);
        private const int MESSAGE_TYPE_LENGTH = sizeof(byte);
        private const int MESSAGE_TIMESTAMP_LENGTH = sizeof(long);

        private ClientTcpNew _client;
        private ServerUdp _listener;
        private ClientStatus _connectionStatus = ClientStatus.DISCONNECTED;
        private int _port;
        private List<ClientUdp> _announcers = new List<ClientUdp>();
        private string _serverName = "Server";
        private readonly ConcurrentQueue<MessageContainer> _messageQueue = new ConcurrentQueue<MessageContainer>();
        private readonly Dictionary<MessageContainer.MessageType, RingBuffer<MessageContainer>> _messageBuffers = new Dictionary<MessageContainer.MessageType, RingBuffer<MessageContainer>>();
        private readonly ConcurrentQueue<Socket> _clientConnectionQueue = new ConcurrentQueue<Socket>();
        private readonly ConcurrentQueue<Socket> _clientDisconnectionQueue = new ConcurrentQueue<Socket>();
        private readonly Dictionary<IPEndPoint, EndPointState> _endPointStates = new Dictionary<IPEndPoint, EndPointState>();
        private readonly Dictionary<string, string> _broadcastIPs = new Dictionary<string, string>();


        /// <summary>
        /// Gets a value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_client != null && _client.IsConnected)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating the port that the server is running on.
        /// </summary>
        public int Port { get { return _port; } }

        /// <summary>
        /// Gets a value indicating the server name.
        /// </summary>
        public string ServerName { get { return _serverName; } }

        /// <summary>
        /// Gets a value indicating the server IPs.
        /// </summary>
        public IReadOnlyList<string> ServerIPs { get { return _broadcastIPs.Values.ToList().AsReadOnly(); } }

        public bool ReconnectAutomatically = false;

        /// <summary>
        /// Pauses the handling of network messages.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Restarts the handling of network messages.
        /// </summary>
        public void Unpause()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Starts listening for servers.
        /// </summary>
        /// <returns><see langword="true"/> if the client started listening for announcements, <see langword="false"/> otherwise.</returns>
        public bool StartListening(int port)
        {
            // listen for server announcements on broadcast
            Debug.Log("searching for servers on port " + port);
            _listener = new ServerUdp(port);
            _listener.DataReceived += OnBroadcastDataReceived;
            return _listener.Start();
        }

        public void Drop()
        {
            _client?.Close();
            StopServer();
        }


        /// <summary>
        /// Stops listening for servers.
        /// </summary>
        public void StopListening()
        {
            _listener?.Stop();
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        public bool ConnectToServer(string ip, int port)
        {
            Debug.LogWarning("Connecting!");
            if (_connectionStatus == ClientStatus.IS_CONNECTING)
            {
                return false;
            }

            if (_connectionStatus == ClientStatus.CONNECTED)
            {
                _client.Close();
            }

            _client = new ClientTcpNew(ip, port);
            Debug.Log("Connecting to server at " + ip);
            _client.Connected += OnConnectedToServer;
            _client.DataReceived += OnDataReceived;
            _client.Disconnected += OnDisconnectedFromServer;
            _connectionStatus = ClientStatus.IS_CONNECTING;
            _client.Connect();
            return true;
        }



        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(MessageContainer message)
        {
            _client.Send(message.Serialize());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="message">The message to announce the server with.</param>
        /// <returns><see langword="true"/> if the server started successfully, <see langword="false"/> otherwise.</returns>
        public bool StartServer(NetworkServiceDescription serviceDescription)
        {
            ServiceDescription.RoomId = serviceDescription.RoomId;
            ServiceDescription.Type = serviceDescription.Type;
            ServiceDescription.HostName = ServerName;
            ServiceDescription.Port = 0;
            ServiceDescription.IP = "";
            ServiceDescription.Data = serviceDescription.Data;
            ServiceDescription.ServiceId = serviceDescription.ServiceId;
            ServiceDescription.SessionId = serviceDescription.SessionId;

            // setup server
            _server = new ServerTcpNew(1000_000);
            _server.ClientConnected += OnClientConnected;
            _server.ClientDisconnected += OnClientDisconnected;
            ////Server.DataReceived += OnDataReceived;
            _server.DataReceived += OnDataReceivedAtServer;

            // start server
            bool success = _server.Start();
            if (success == false)
            {
                Debug.Log("Failed to start server!");
                return false;
            }

            ServiceDescription.IP = _server.Address.ToString();
            ServiceDescription.Port = _server.Port;

            Debug.Log("Started server!");

            // announce server via broadcast
            success = false;
            foreach (var item in _broadcastIPs)
            {
                var announcer = new ClientUdp(item.Key, 11338);
                if (!announcer.Open())
                {
                    Debug.Log("Failed to start announcing on " + item.Key + "!");
                }
                else
                {
                    _announcers.Add(announcer);
                    Debug.Log("Started announcing on " + item.Key + "!");
                    success = true;
                }
            }

            if (success == false)
            {
                Debug.LogError("Failed to start announcing server!");
                return false;
            }

            InvokeRepeating(nameof(AnnounceServer), 1.0f, 2.0f);
            return true;
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToAll(MessageContainer message)
        {
            byte[] envelope = message.Serialize();
            foreach (var client in _server.Clients)
            {
                if (client.Connected)
                {
                    _server.SendToClient(client.RemoteEndPoint as IPEndPoint, envelope);
                }
            }
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="client">The client to send the message to.</param>
        public void SendToClient(MessageContainer message, IPEndPoint endpoint)
        {
            byte[] envelope = message.Serialize();
            
            _server.SendToClient(endpoint, envelope);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (_announcers != null && _announcers.Count != 0)
            {
                CancelInvoke("AnnounceServer");
                foreach (var announcer in _announcers)
                {
                    announcer?.Close();
                    announcer?.Dispose();
                }
                _announcers.Clear();
            }

            _server?.Stop();
            _server = null;
        }

        public void InjectData(object sender, IPEndPoint endpoint, byte[] data)
        {
            OnDataReceived(sender, endpoint, data);
        }

        private void Awake()
        {
            // compute local & broadcast ip and look up server name
            CollectNetworkInfo(); // platform dependent, might not work in all configurations

            // create listen server for server announcements
            //listener = new ServerUdp(11338);
            //listener.DataReceived += OnBroadcastDataReceived;
        }

        private void Update()
        {            
            if (_connectionStatus == ClientStatus.JUST_CONNECTED)
            {
                _connectionStatus = ClientStatus.CONNECTED;
                ConnectedToServer?.Invoke(this, EventArgs.Empty);
            }

            if (_connectionStatus == ClientStatus.JUST_DISCONNECTED)
            {
                _connectionStatus = ClientStatus.DISCONNECTED;
                DisconnectedFromServer?.Invoke(this, EventArgs.Empty);
            }

            MessageContainer message;
            if (!IsPaused)
            {
                while (_messageQueue.TryDequeue(out message))
                {
                    MessageReceived?.Invoke(this, message);
                }
                foreach (var kvp in _messageBuffers)
                {
                    if (kvp.Value.Count != 0)
                    {
                        message = kvp.Value.Get();
                        if (message != null)
                        {
                            MessageReceived?.Invoke(this, message);
                        }
                    }
                }
            }
            

            Socket client;
            while (_clientConnectionQueue.TryDequeue(out client))
            {
                ClientConnectionEstablished?.Invoke(this, client);
            }

            while (_clientDisconnectionQueue.TryDequeue(out client))
            {
                ClientConnectionClosed?.Invoke(this, client);
            }
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            Debug.Log("Connected to server!");
            _connectionStatus = ClientStatus.JUST_CONNECTED;
        }

        private void OnDisconnectedFromServer(object sender, EventArgs e)
        {
            Debug.Log("Disconnected from server!");
            _connectionStatus = ClientStatus.JUST_DISCONNECTED;
            //if (ReconnectAutomatically)
            //{
            //    if (_client != null)
            //    {
            //        _client.Connect();
            //        _connectionStatus = ClientStatus.IS_CONNECTING;
            //    }
            //}
            
        }

        private void OnBroadcastDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            _messageQueue.Enqueue(MessageContainer.Deserialize(remoteEndPoint, data));
        }

        // called by InvokeRepeating
        private void AnnounceServer()
        {
            foreach (var announcer in _announcers)
            {
                if (announcer.IsOpen)
                {
                    string IP = ServiceDescription.IP;
                    ServiceDescription.IP = _broadcastIPs[announcer.IpAddress];
                    var Message = new MessageAnnouncement(ServiceDescription);
                    announcer.Send(Message.Pack().Serialize());
                    ServiceDescription.IP = IP;
                    Debug.Log("Announcing server at " + announcer.IpAddress + " with message " + ServiceDescription.Description);
                }
                else
                {
                    announcer.Open();
                }
            }
        }

        private void OnDataReceivedAtServer(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            // dispatch received data to all other clients (but not the original sender)
            if (_server != null)
            {
                // only if we have a server
                Dispatch(remoteEndPoint, data);
            }

            OnDataReceived(sender, remoteEndPoint, data);
        }

        private void OnDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            // get current EndPointState from the collection or create a new one
            EndPointState state;
            if (_endPointStates.ContainsKey(remoteEndPoint))
            {
                state = _endPointStates[remoteEndPoint];
            }
            else
            {
                state = new EndPointState();
            }

            int currentByte = 0;
            int dataLength = data.Length;

            // iterate through all data in the buffer
            while(currentByte < dataLength)
            {
                // check current state
                if (state.ParseState == ParseState.NEW)
                {
                    // start reading new message

                    // update state
                    state.ParseState = ParseState.HEADER;

                    // create header buffer
                    state.HeaderBuffer = new byte[MESSAGE_HEADER_LENGTH];
                }
                else if (state.ParseState == ParseState.HEADER)
                {
                    // continue reading header

                    // computer number of bytes to read: until end of buffer or end of header
                    int numBytesToRead = Math.Min(dataLength - currentByte, MESSAGE_HEADER_LENGTH - state.HeaderBytesRead);

                    // read header bytes into buffer
                    Buffer.BlockCopy(data, currentByte, state.HeaderBuffer, state.HeaderBytesRead, numBytesToRead);

                    // update indices
                    state.HeaderBytesRead += numBytesToRead;
                    currentByte += numBytesToRead;

                    // check if header is read
                    if (state.HeaderBytesRead == MESSAGE_HEADER_LENGTH)
                    {
                        // update state
                        state.ParseState = ParseState.BODY;

                        // parse header info
                        state.MessageSize = BitConverter.ToInt32(state.HeaderBuffer, 0);
                        state.MessageType = state.HeaderBuffer[MESSAGE_SIZE_LENGTH];
                        state.MessageTimestamp = BitConverter.ToInt64(state.HeaderBuffer, MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH);

                        // create body buffer
                        state.MessageBuffer = new byte[state.MessageSize];
                    }
                }
                else if (state.ParseState == ParseState.BODY)
                {
                    // continue reading body

                    // computer number of bytes to read: until end of buffer or end of body
                    int numBytesToRead = Math.Min(dataLength - currentByte, state.MessageSize - state.MessageBytesRead);

                    // read body bytes into buffer
                    Buffer.BlockCopy(data, currentByte, state.MessageBuffer, state.MessageBytesRead, numBytesToRead);

                    // update indices
                    state.MessageBytesRead += numBytesToRead;
                    currentByte += numBytesToRead;

                    // check if body is read
                    if (state.MessageBytesRead == state.MessageSize)
                    {
                        // enqueue completed message
                        if (UseMessageQueue)
                        {
                            _messageQueue.Enqueue(MessageContainer.Deserialize(remoteEndPoint, state.MessageBuffer, state.MessageType, state.MessageTimestamp));
                        }
                        else
                        {
                            if (!_messageBuffers.ContainsKey((MessageContainer.MessageType)state.MessageType))
                            {
                                _messageBuffers[(MessageContainer.MessageType)state.MessageType] = new RingBuffer<MessageContainer>(1);
                            }
                            _messageBuffers[(MessageContainer.MessageType)state.MessageType].Put(MessageContainer.Deserialize(remoteEndPoint, state.MessageBuffer, state.MessageType, state.MessageTimestamp));
                        }

                        // reset message state
                        state = new EndPointState();
                    }
                }
            }

            // save current message state to the collection, to pick up reading messages in the next receive event
            _endPointStates[remoteEndPoint] = state;
        }

        private void OnDataReceived2(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            int currentByte = 0;
            int dataLength = data.Length;
            EndPointState state;
            try
            {
                if (_endPointStates.ContainsKey(remoteEndPoint))
                {
                    state = _endPointStates[remoteEndPoint];
                }
                else
                {
                    state = new EndPointState();
                    _endPointStates[remoteEndPoint] = state;
                }

                state.Sender = remoteEndPoint;
                while (currentByte < dataLength)
                {
                    int messageSize;

                    // currently still reading a (large) message?
                    if (state.IsMessageIncomplete)
                    {
                        //Debug.Log("resuming message");

                        // 1. get size of current message
                        messageSize = state.MessageBuffer.Length;

                        // 2. read data
                        // decide how much to read: not more than remaining message size, not more than remaining data size
                        int lengthToRead = Math.Min(messageSize - state.MessageBytesRead, dataLength - currentByte);

                        Array.Copy(data, currentByte, state.MessageBuffer, state.MessageBytesRead, lengthToRead); // copy data from data to message buffer
                        //dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentMessageBuffer.AsSpan(state.CurrentMessageBytesRead)); // copy data from data to message buffer

                        currentByte += lengthToRead; // increase "current byte pointer"
                        state.MessageBytesRead += lengthToRead; // increase amount of message bytes read

                        // 3. decide how to proceed
                        if (state.MessageBytesRead == messageSize)
                        {
                            //Debug.Log("message complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));

                            // Message is completed
                            state.IsMessageIncomplete = false;
                            _messageQueue.Enqueue(MessageContainer.Deserialize(state.Sender, state.MessageBuffer, state.MessageType, state.MessageTimestamp));
                        }
                        else
                        {
                            //Debug.Log("message incomplete, " + state.CurrentMessageBytesRead + " Bytes read");

                            // We did not read the whole message yet
                            state.IsMessageIncomplete = true;
                        }
                    }
                    else if (state.IsHeaderIncomplete)
                    {
                        //Debug.Log("resuming header");

                        // currently still reading a header
                        // decide how much to read: not more than remaining message size, not more than remaining header size
                        int lengthToRead = Math.Min(MESSAGE_HEADER_LENGTH - state.HeaderBytesRead, dataLength - currentByte);

                        Array.Copy(data, currentByte, state.HeaderBuffer, state.HeaderBytesRead, lengthToRead); // read header data into header buffer
                        //dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentHeaderBuffer.AsSpan(state.CurrentHeaderBytesRead)); // read header data into header buffer

                        currentByte += lengthToRead;
                        state.HeaderBytesRead += lengthToRead;
                        if (state.HeaderBytesRead == MESSAGE_HEADER_LENGTH)
                        {
                            // Message header is completed
                            // read size of message from header buffer
                            messageSize = BitConverter.ToInt32(state.HeaderBuffer, 0);
                            state.MessageBuffer = new byte[messageSize];
                            state.MessageBytesRead = 0;
                            
                            //Debug.Log("message size: " + messageSize);

                            // read type of next message
                            state.MessageType = state.HeaderBuffer[MESSAGE_SIZE_LENGTH];

                            // read timestamp of message
                            state.MessageTimestamp = BitConverter.ToInt64(state.HeaderBuffer, MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH);

                            //Debug.Log("header complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));

                            state.IsHeaderIncomplete = false;
                            state.IsMessageIncomplete = true;
                        }
                        else
                        {
                            //Debug.Log("header incomplete");

                            // We did not read the whole header yet
                            state.IsHeaderIncomplete = true;
                        }
                    }
                    else
                    {
                        // start reading a new message

                        //Debug.Log("new message");

                        // 1. check if remaining data sufficient to read message header
                        if (currentByte < dataLength - MESSAGE_HEADER_LENGTH)
                        {
                            // 2. read size of next message
                            messageSize = BitConverter.ToInt32(data, currentByte);
                            state.MessageBuffer = new byte[messageSize];
                            state.MessageBytesRead = 0;
                            currentByte += MESSAGE_SIZE_LENGTH;
                            
                            //Debug.Log("message size: " + messageSize);

                            // 3. read type of next message
                            state.MessageType = data[currentByte];
                            currentByte += MESSAGE_TYPE_LENGTH;

                            // 4. read timestamp of next message
                            state.MessageTimestamp = BitConverter.ToInt64(data, currentByte);
                            currentByte += MESSAGE_TIMESTAMP_LENGTH;

                            //Debug.Log("header complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));

                            // 5. read data
                            // decide how much to read: not more than remaining message size, not more than remaining data size
                            int lengthToRead = Math.Min(messageSize - state.MessageBytesRead, dataLength - currentByte);

                            Array.Copy(data, currentByte, state.MessageBuffer, state.MessageBytesRead, lengthToRead); // copy data from data to message buffer
                            //dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentMessageBuffer.AsSpan(state.CurrentMessageBytesRead));

                            currentByte += lengthToRead; // increase "current byte pointer"
                            state.MessageBytesRead += lengthToRead; // increase amount of message bytes read

                            // 4. decide how to proceed
                            if (state.MessageBytesRead == messageSize)
                            {
                                //Debug.Log("message complete, " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));

                                // Message is completed
                                state.IsMessageIncomplete = false;
                                _messageQueue.Enqueue(MessageContainer.Deserialize(state.Sender, state.MessageBuffer, state.MessageType, state.MessageTimestamp));
                            }
                            else
                            {
                                //Debug.Log("message incomplete, " + state.CurrentMessageBytesRead + " Bytes read");

                                // We did not read the whole message yet
                                state.IsMessageIncomplete = true;
                            }
                        }
                        else
                        {
                            //Debug.Log("header incomplete");

                            // not enough data to read complete header for new message
                            state.HeaderBuffer = new byte[MESSAGE_HEADER_LENGTH]; // create new header data buffer to store a partial message header
                            int lengthToRead = dataLength - currentByte;

                            Array.Copy(data, currentByte, state.HeaderBuffer, 0, lengthToRead); // read header data into header buffer
                            //dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentHeaderBuffer.AsSpan()); // read header data into header buffer

                            currentByte += lengthToRead;
                            state.HeaderBytesRead = lengthToRead;
                            state.IsHeaderIncomplete = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while parsing network data. Message: " + e.Message + "\nInner Exception Message: " + e.InnerException.Message + "\nStack Trace: " + e.StackTrace);
            }
        }

        //private void OnDataReceived(object sender, IPEndPoint remoteEndPoint, Memory<byte> data)
        //{
        //    Span<byte> dataSpan = data.Span;
        //    int currentByte = 0;
        //    int dataLength = dataSpan.Length;
        //    EndPointState state;
        //    try
        //    {
        //        if (_endPointStates.ContainsKey(remoteEndPoint))
        //        {
        //            state = _endPointStates[remoteEndPoint];
        //        }
        //        else
        //        {
        //            state = new EndPointState();
        //            _endPointStates[remoteEndPoint] = state;
        //        }

        //        state.CurrentSender = remoteEndPoint;
        //        while (currentByte < dataLength)
        //        {
        //            int messageSize;

        //            // currently still reading a (large) message?
        //            if (state.IsMessageIncomplete)
        //            {
        //                Debug.Log("resuming message");
        //                // 1. get size of current message
        //                messageSize = state.CurrentMessageBuffer.Length;

        //                // 2. read data
        //                // decide how much to read: not more than remaining message size, not more than remaining data size
        //                int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, dataLength - currentByte);

        //                //Array.Copy(dataArray, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
        //                dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentMessageBuffer.AsSpan(state.CurrentMessageBytesRead)); // copy data from data to message buffer

        //                currentByte += lengthToRead; // increase "current byte pointer"
        //                state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

        //                // 3. decide how to proceed
        //                if (state.CurrentMessageBytesRead == messageSize)
        //                {
        //                    Debug.Log("message complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));
        //                    // Message is completed
        //                    state.IsMessageIncomplete = false;
        //                    _messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType, state.CurrentTimestamp));
        //                }
        //                else
        //                {
        //                    Debug.Log("message incomplete, " + state.CurrentMessageBytesRead + " Bytes read");
        //                    // We did not read the whole message yet
        //                    state.IsMessageIncomplete = true;
        //                }
        //            }
        //            else if (state.IsHeaderIncomplete)
        //            {
        //                Debug.Log("resuming header");
        //                // currently still reading a header
        //                // decide how much to read: not more than remaining message size, not more than remaining header size
        //                int lengthToRead = Math.Min(MESSAGE_HEADER_LENGTH - state.CurrentHeaderBytesRead, dataLength - currentByte);

        //                //Array.Copy(dataArray, currentByte, state.CurrentHeaderBuffer, state.CurrentHeaderBytesRead, lengthToRead); // read header data into header buffer
        //                dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentHeaderBuffer.AsSpan(state.CurrentHeaderBytesRead)); // read header data into header buffer

        //                currentByte += lengthToRead;
        //                state.CurrentHeaderBytesRead += lengthToRead;
        //                if (state.CurrentHeaderBytesRead == MESSAGE_HEADER_LENGTH)
        //                {
        //                    // Message header is completed
        //                    // read size of message from header buffer
        //                    messageSize = BitConverter.ToInt32(state.CurrentHeaderBuffer, 0);
        //                    state.CurrentMessageBuffer = new byte[messageSize];
        //                    state.CurrentMessageBytesRead = 0;
        //                    Debug.Log("message size: " + messageSize);

        //                    // read type of next message
        //                    state.CurrentMessageType = state.CurrentHeaderBuffer[MESSAGE_SIZE_LENGTH];

        //                    // read timestamp of message
        //                    state.CurrentTimestamp = BitConverter.ToInt64(state.CurrentHeaderBuffer, MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH);

        //                    Debug.Log("header complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));

        //                    state.IsHeaderIncomplete = false;
        //                    state.IsMessageIncomplete = true;
        //                }
        //                else
        //                {
        //                    Debug.Log("header incomplete");
        //                    // We did not read the whole header yet
        //                    state.IsHeaderIncomplete = true;
        //                }
        //            }
        //            else
        //            {
        //                // start reading a new message
        //                Debug.Log("new message");
        //                // 1. check if remaining data sufficient to read message header
        //                if (currentByte < dataLength - MESSAGE_HEADER_LENGTH)
        //                {
        //                    // 2. read size of next message
        //                    messageSize = BitConverter.ToInt32(dataSpan.Slice(currentByte));
        //                    state.CurrentMessageBuffer = new byte[messageSize];
        //                    state.CurrentMessageBytesRead = 0;
        //                    currentByte += MESSAGE_SIZE_LENGTH;
        //                    Debug.Log("message size: " + messageSize);

        //                    // 3. read type of next message
        //                    state.CurrentMessageType = dataSpan[currentByte];
        //                    currentByte += MESSAGE_TYPE_LENGTH;

        //                    // 4. read timestamp of next message
        //                    state.CurrentTimestamp = BitConverter.ToInt64(dataSpan.Slice(currentByte));
        //                    currentByte += MESSAGE_TIMESTAMP_LENGTH;

        //                    Debug.Log("header complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));

        //                    // 5. read data
        //                    // decide how much to read: not more than remaining message size, not more than remaining data size
        //                    int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, dataLength - currentByte);

        //                    //Array.Copy(dataArray, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
        //                    dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentMessageBuffer.AsSpan(state.CurrentMessageBytesRead));

        //                    currentByte += lengthToRead; // increase "current byte pointer"
        //                    state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

        //                    // 4. decide how to proceed
        //                    if (state.CurrentMessageBytesRead == messageSize)
        //                    {
        //                        Debug.Log("message complete, " + Enum.GetName(typeof(MessageContainer.MessageType), state.CurrentMessageType));
        //                        // Message is completed
        //                        state.IsMessageIncomplete = false;
        //                        _messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType, state.CurrentTimestamp));
        //                    }
        //                    else
        //                    {
        //                        Debug.Log("message incomplete, " + state.CurrentMessageBytesRead + " Bytes read");
        //                        // We did not read the whole message yet
        //                        state.IsMessageIncomplete = true;
        //                    }
        //                }
        //                else
        //                {
        //                    Debug.Log("header incomplete");
        //                    // not enough data to read complete header for new message
        //                    state.CurrentHeaderBuffer = new byte[MESSAGE_HEADER_LENGTH]; // create new header data buffer to store a partial message header
        //                    int lengthToRead = dataLength - currentByte;

        //                    //Array.Copy(dataArray, currentByte, state.CurrentHeaderBuffer, 0, lengthToRead); // read header data into header buffer
        //                    dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.CurrentHeaderBuffer.AsSpan()); // read header data into header buffer

        //                    currentByte += lengthToRead;
        //                    state.CurrentHeaderBytesRead = lengthToRead;
        //                    state.IsHeaderIncomplete = true;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError("Error while parsing network data. Message: " + e.Message + "\nInner Exception Message: " + e.InnerException.Message + "\nStack Trace: " + e.StackTrace);
        //    }
        //}

        private void Dispatch(IPEndPoint sender, Memory<byte> data)
        {
            var dataArray = data.ToArray();
            foreach (var client in _server.Clients)
            {
                if (sender.Address.ToString().Equals(((IPEndPoint)client.RemoteEndPoint).Address.ToString()))
                {
                    continue;
                }
                else
                {
                    _server.SendToClient(client.RemoteEndPoint as IPEndPoint, dataArray);
                }
            }
        }

        private void OnClientDisconnected(object sender, Socket socket)
        {
            Debug.Log("Client disconnected: " + IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()));
            _clientDisconnectionQueue.Enqueue(socket);
        }

        private void OnClientConnected(object sender, Socket socket)
        {
            Debug.Log("Client connected: " + IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()));
            _clientConnectionQueue.Enqueue(socket);
        }

        private void OnDestroy()
        {
            StopListening();
            StopServer();
            Drop();
        }

#if UNITY_WSA && !UNITY_EDITOR
        private void CollectNetworkInfo()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();

        IEnumerable<HostName> hostnames =
            NetworkInformation.GetHostNames().Where(h =>
                h.IPInformation != null &&
                h.IPInformation.NetworkAdapter != null &&
                h.Type == HostNameType.Ipv4).ToList();

        var hostName = (from h in hostnames
                      where h.IPInformation.NetworkAdapter.NetworkAdapterId == profile.NetworkAdapter.NetworkAdapterId
                      select h).FirstOrDefault();
        byte? prefixLength = hostName.IPInformation.PrefixLength;
        IPAddress ip = IPAddress.Parse(hostName.RawName);
        byte[] ipBytes = ip.GetAddressBytes();
        uint mask = ~(uint.MaxValue >> prefixLength.Value);
        byte[] maskBytes = BitConverter.GetBytes(mask);

        byte[] broadcastIPBytes = new byte[ipBytes.Length];

        for (int i = 0; i < ipBytes.Length; i++)
        {
            broadcastIPBytes[i] = (byte)(ipBytes[i] | ~maskBytes[ipBytes.Length - (i+1)]);
        }

        // Convert the bytes to IP addresses.
        string broadcastIP = new IPAddress(broadcastIPBytes).ToString();
        string localIP = ip.ToString();
        foreach (HostName name in NetworkInformation.GetHostNames())
        {
            if (name.Type == HostNameType.DomainName)
            {
                _serverName = name.DisplayName;
                break;
            }
        }
        _broadcastIPs.Clear();
        _broadcastIPs[broadcastIP] = localIP;
    }
#else

        private void CollectNetworkInfo()
        {
            _serverName = Environment.ExpandEnvironmentVariables("%ComputerName%");
            _broadcastIPs.Clear();

            // 1. get ipv4 addresses
            var IPs = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            // 2. get net mask for local ip
            // get valid interfaces
            var Interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(intf => intf.OperationalStatus == OperationalStatus.Up &&
                (intf.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                intf.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));

            // find interface with matching ipv4 and get the net mask
            IEnumerable<UnicastIPAddressInformation> NetMasks = null;
            foreach (var Interface in Interfaces)
            {
                NetMasks = from inf in Interface.GetIPProperties().UnicastAddresses
                           from IP in IPs
                           where inf.Address.Equals(IP)
                           select inf;
                if (NetMasks != null)
                {
                    IPAddress NetMask = NetMasks.FirstOrDefault().IPv4Mask;
                    IPAddress IP = NetMasks.FirstOrDefault().Address;
                    byte[] MaskBytes = NetMask.GetAddressBytes();
                    byte[] IPBytes = IP.GetAddressBytes();
                    for (int i = 0; i < IPBytes.Length; i++)
                    {
                        IPBytes[i] = (byte)(IPBytes[i] | ~MaskBytes[i]);
                    }

                    string localIP = IP.ToString();
                    string broadcastIP = new IPAddress(IPBytes).ToString();
                    _broadcastIPs[broadcastIP] = localIP;
                }
            }
        }

#endif
    }

    /// <summary>
    /// Helper class used to store the current state of a network endpoint.
    /// </summary>
    internal class EndPointState
    {
        public byte[] MessageBuffer;
        public int MessageBytesRead;
        public bool IsMessageIncomplete = false;
        public IPEndPoint Sender;
        public bool IsHeaderIncomplete = false;
        public byte[] HeaderBuffer;
        public int HeaderBytesRead;
        public ParseState ParseState = ParseState.NEW;
        public int MessageSize;
        public long MessageTimestamp;
        public byte MessageType;
    }

    internal enum ParseState
    {
        NEW,
        HEADER,
        BODY
    }

    internal enum ClientStatus
    {
        DISCONNECTED,
        IS_CONNECTING,
        JUST_CONNECTED,
        CONNECTED,
        JUST_DISCONNECTED
    }
}