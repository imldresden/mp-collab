using IMLD.MixedReality.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using static IMLD.MixedReality.Network.NetworkService;

namespace IMLD.MixedReality.Network
{
    public class NetworkService : MonoBehaviour, INetworkService
    {
        public NetworkServiceDescription.ServiceType ServiceType
        {
            get { return _serviceDescription.Type; }
            set { _serviceDescription.Type = value; }
        }

        public string ServiceData
        {
            get { return _serviceDescription.Data; }
            set { _serviceDescription.Data = value; }
        }

        public NetworkServiceDescription ServiceDescription
        {
            get { return _serviceDescription; }
        }

        public INetworkFilter NetworkFilter { get; set; }

        public INetworkService.NetworkServiceRole Role { get; private set; } = INetworkService.NetworkServiceRole.UNDEFINED;

        public float RequestedLatency { get; set; }
        public float ServerLatency { get; private set; }
        public float ServerTimeOffset { get; private set; }

        public float ConnectionTimeout = 4f;

        public INetworkService.NetworkServiceStatus Status {get; private set; } = INetworkService.NetworkServiceStatus.DISCONNECTED;

        private NetworkTransport _transport;
        private Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>> _messageHandlers = new Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>>();
        private NetworkServiceDescription _serviceDescription = new NetworkServiceDescription { HostName = "Unknown Host", IP = "", Port = 0, Type = NetworkServiceDescription.ServiceType.UNDEFINED };
        private ISessionManager _sessionManager;
        private List<MessageContainer.MessageType> _unknownMessageTypes = new List<MessageContainer.MessageType>();

        [SerializeField]
        private ConnectionInfo _serverStatus;

        private List<ConnectionInfo> _connections = new List<ConnectionInfo>();
        //private Dictionary<Guid, ConnectionInfo> _connectedClients = new Dictionary<Guid, ConnectionInfo>();

        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;
        public event ClientEventHandler ClientReappeared;
        public event ClientEventHandler ClientDisappeared;

        public event EventHandler ConnectedToServer;
        public event EventHandler DisconnectedFromServer;

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

        public void SendMessage(IMessage message)
        {
            if (_transport == null)
            {
                Debug.LogError("Cannot send message, socket is null.");
                return;
            }

            if (_transport.NumberOfPeers == 0)
            {
                return;
            }

            var container = message.Pack();

            SendMessage(container);
        }

        public void SendMessage(MessageContainer container)
        {
            if (_transport == null)
            {
                Debug.LogError("Cannot send message, socket is null.");
                return;
            }

            if (_transport.NumberOfPeers == 0)
            {
                return;
            }

            if (Role == INetworkService.NetworkServiceRole.SERVER)
            {
                //_transport.SendToAll(container);
                foreach(var clientInfo in _connections)
                {
                    if (clientInfo.Status == ConnectionStatus.CONNECTED)
                    {
                        _transport.SendToClient(container, clientInfo.EndPoint);
                    }
                }
            }
            else if (Role == INetworkService.NetworkServiceRole.CLIENT)
            {
                _transport.SendToServer(container);
            }
        }

        public bool StartAsClient(NetworkServiceDescription serviceDescription, bool useMessageQueue = true)
        {
            // Check prerequisites
            if (!enabled)
            {
                Debug.Log("Component disabled, cannot connect to server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }

            if (_transport == null)
            {
                Debug.Log("Network transport not ready, cannot connect to server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }
            _transport.UseMessageQueue = useMessageQueue;
            bool result = _transport.ConnectToServer(serviceDescription.IP, serviceDescription.Port);

            if (result)
            {
                Role = INetworkService.NetworkServiceRole.CLIENT;
                _serviceDescription = serviceDescription;
                Status = INetworkService.NetworkServiceStatus.CONNECTING;
            }
            else
            {
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                Status = INetworkService.NetworkServiceStatus.DISCONNECTED;
            }

            _serverStatus = new ConnectionInfo(new IPEndPoint(IPAddress.Parse(serviceDescription.IP), serviceDescription.Port))
            {
                Status = ConnectionStatus.IS_CONNECTING,
                Id = serviceDescription.ServiceId
            };

            return result;
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

            if (_transport == null)
            {
                Debug.Log("Network transport not ready, cannot start server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }

            if (_sessionManager == null)
            {
                Debug.Log("No Network Service Manager found, cannot start server!");
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
                return false;
            }

            // Load service id for service type from config, if available
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null && config.TryLoad<string>("ServiceId_" + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType), out var serviceId))
            {
                _serviceDescription.ServiceId = Guid.Parse(serviceId);
            }
            else
            {
                _serviceDescription.ServiceId = Guid.NewGuid();
                if (config != null)
                {
                    config.Save("ServiceId_" + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType), _serviceDescription.ServiceId.ToString());
                }
            }

            // Start server
            Debug.Log("Starting as server");
            _serviceDescription.RoomId = _sessionManager.Room.Id;
            _serviceDescription.Data = ServiceData;
            _serviceDescription.SessionId = _sessionManager.SessionId;
            _transport.UseMessageQueue = useMessageQueue;
            bool result = _transport.StartServer(_serviceDescription);

            // Update service description
            if (result)
            {
                _serviceDescription = _transport.ServiceDescription;
                Role = INetworkService.NetworkServiceRole.SERVER;
            }
            else
            {
                Role = INetworkService.NetworkServiceRole.UNDEFINED;
            }

            // Setup handling ping requests
            RegisterMessageHandler(MessageContainer.MessageType.PING, HandlePingRequest);

            // Setup connect/disconnect message handling
            RegisterMessageHandler(MessageContainer.MessageType.CONNECT_TO_SERVER, HandleConnectMessage);
            RegisterMessageHandler(MessageContainer.MessageType.DISCONNECT_FROM_SERVER, HandleDisconnectMessage);

            // Start coroutine to check stale connections
            StartCoroutine(CheckClientConnections());

            return result;
        }

        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType)
        {
            return _messageHandlers.Remove(messageType);
        }

        public void UpdateRoom(RoomDescription room)
        {
            _serviceDescription.RoomId = room.Id;
            _transport.ServiceDescription.RoomId = room.Id;
        }

        void Awake()
        {
            // register network server/client
            if (gameObject.TryGetComponent<NetworkTransport>(out NetworkTransport component))
            {
                _transport = component;
            }
            else
            {
                _transport = gameObject.AddComponent<NetworkTransport>();
            }

            // add callbacks
            _transport.ConnectedToServer += OnConnectedToServer;
            _transport.DisconnectedFromServer += OnDisconnectedFromServer;
            _transport.MessageReceived += OnMessageReceived;
            _transport.ClientConnectionEstablished += OnClientConnectionEstablished;
            _transport.ClientConnectionClosed += OnClientConnectionClosed;

            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();
            _sessionManager.RoomJoined += OnRoomJoined;
            _sessionManager.RoomLeft += OnRoomLeft;
            _sessionManager.SessionJoined += OnSessionJoined;
            _sessionManager.SessionLeft += OnSessionLeft;
        }

        private void OnSessionJoined(object sender, EventArgs e)
        {
            if (_sessionManager != null)
            {
                _serviceDescription.SessionId = _sessionManager.SessionId;
            }

            if (_transport != null)
            {
                _transport.ServiceDescription.SessionId = _sessionManager.SessionId;
            }
        }

        private void OnSessionLeft(object sender, EventArgs e)
        {
            if (_sessionManager != null)
            {
                _serviceDescription.SessionId = _sessionManager.SessionId;
            }

            if (_transport != null)
            {
                _transport.ServiceDescription.SessionId = _sessionManager.SessionId;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        private void OnRoomJoined(object sender, RoomEventArgs e)
        {
            if (_sessionManager != null)
            {
                _serviceDescription.RoomId = e.Room.Id;
            }

            if (_transport != null)
            {
                _transport.ServiceDescription.RoomId = _serviceDescription.RoomId;
            }
        }

        private void OnRoomLeft(object sender, EventArgs e)
        {
            if (_sessionManager != null)
            {
                _serviceDescription.RoomId = -1;
            }

            if (_transport != null)
            {
                _transport.ServiceDescription.RoomId = _serviceDescription.RoomId;
            }
        }

        private void OnDestroy()
        {
            Destroy();
        }
        public void Drop()
        {
            _transport.Drop();
        }
        public void Destroy()
        {
            if (this?.NetworkFilter != null)
            {
                NetworkFilter.Dispose();
                NetworkFilter = null;
            }

            if (this?._transport != null)
            {
                Destroy(_transport);
                _transport = null;
            }

            if (this?.gameObject != null)
            {
                Destroy(gameObject);
            }

            ConnectedToServer = null;
            DisconnectedFromServer = null;
        }

        private void OnApplicationQuit()
        {
            Destroy();
        }

        private async void OnMessageReceived(object sender, MessageContainer message)
        {
            // update connection info
            UpdateConnectionInfo(message.Sender);

            // filter message
            if (NetworkFilter != null)
            {
                NetworkFilter.FilterMessage(this, ref message);
                if (message == null)
                {
                    return;
                }
            }

            // handle message
            if (RequestedLatency > ServerLatency && message.Type != MessageContainer.MessageType.PING)
            {
                //Debug.Log(Time.realtimeSinceStartup + ": Delay message by " + (int)(1000*(RequestedLatency - ServerLatency)) + " ms...");
                await HandleMessageDelayed(sender, message, RequestedLatency - ServerLatency);
                //Debug.Log(Time.realtimeSinceStartup + ": Message handled.");
            }
            else
            {
                await HandleMessage(sender, message);
            }
        }

        private void UpdateConnectionInfo(IPEndPoint sender)
        {
            // connection is the server we are connected to
            if (_serverStatus != null && _serverStatus.EndPoint != null && _serverStatus.EndPoint.Equals(sender))
            {
                // update server status
                _serverStatus.Status = ConnectionStatus.CONNECTED;
                _serverStatus.LastSeen = DateTime.UtcNow.Ticks;
                return;
            }

            // check all connected clients
            ConnectionInfo clientInfo = null;
            clientInfo = _connections?.Find(x => x.EndPoint.Equals(sender));

            if (clientInfo != null)
            {
                if (clientInfo.Status == ConnectionStatus.MISSING)
                {
                    HandleReappearingClient(clientInfo);
                }
                else
                {
                    clientInfo.LastSeen = DateTime.UtcNow.Ticks;
                }
            }
        }

        private async Task HandleMessage(object sender, MessageContainer message)
        {
            if (_messageHandlers != null)
            {
                Func<MessageContainer, Task> callback;
                if (_messageHandlers.TryGetValue(message.Type, out callback) && callback != null)
                {
                    message.Offset = ServerTimeOffset;
                    message.ConnectionLatency = ServerLatency;
                    await callback(message);
                }
                else
                {
                    if (_unknownMessageTypes.Contains(message.Type) == false)
                    {
                        Debug.LogWarning("Unknown message: " + message.Type.ToString() +
                        ". No further warning for this type of message will be logged by this Network Service.");
                        _unknownMessageTypes.Add(message.Type);
                    }
                }
            }
        }

        private IEnumerator CheckServerConnection()
        {
            while (true)
            {
                SendMessage(new MessagePing());
                //Debug.Log("Requesting ping from " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType));
                //long ticks = DateTime.UtcNow.Ticks;

                //if ((ticks - _serverStatus.LastSeen) / (long)TimeSpan.TicksPerSecond > ConnectionTimeout)
                //{
                //    HandleDisappearingServer(_serverStatus);
                //}

                yield return new WaitForSeconds(1f);
            }
        }

        private void PrintClientList()
        {
            string output = "Server " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType) + ":\n";

            if (_connections?.Count > 0)
            {
                foreach (var client in _connections)
                {
                    output += client.Id + " " + Enum.GetName(typeof(ClientStatus), client.Status) + "\n";
                }
                Debug.Log(output);
            }
        }

        private IEnumerator CheckClientConnections()
        {
            if (_connections == null)
            {
                yield return null;
            }

            while (true)
            {
                long ticks = DateTime.UtcNow.Ticks;
                foreach (var clientInfo in _connections.FindAll(x => x.Status == ConnectionStatus.CONNECTED))
                {
                    if ((ticks - clientInfo.LastSeen) / (long)TimeSpan.TicksPerSecond > ConnectionTimeout)
                    {
                        HandleDisappearingClient(clientInfo);;
                    }
                }

                PrintClientList();

                yield return new WaitForSeconds(2f);
            }

        }

        private async Task HandleMessageDelayed(object sender, MessageContainer message, float latency)
        {
            //Debug.Log(Time.realtimeSinceStartup + ": Start delaying...");
            await Task.Delay((int)(latency * 1000));
            //Debug.Log(Time.realtimeSinceStartup + ": Finished delaying");
            await HandleMessage(sender, message);
        }

        private Task HandlePingRequest(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.PING)
            {
                //Debug.Log("Ping request at " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType) + " from " + container.Sender);
                // answer ping request with updated ping message
                SendMessage(new MessagePing(MessagePing.Unpack(container)));
            }

            return Task.CompletedTask;
        }

        private Task HandlePingResponse(MessageContainer container)
        {
            
            if (container?.Type == MessageContainer.MessageType.PING)
            {
                //Debug.Log("Ping response at " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), ServiceType) + " from " + container.Sender);
                var Message = MessagePing.Unpack(container);

                // compute round-trip time
                float HalfRoundTripTime = (DateTime.UtcNow.Ticks - Message.TicksRequest) / 2.0f;

                // update server connection latency as half of the round-trip time
                ServerLatency = HalfRoundTripTime / TimeSpan.TicksPerSecond;

                // update server-relative time offset
                ServerTimeOffset = (Message.TicksRequest - Message.TicksResponse + HalfRoundTripTime) / TimeSpan.TicksPerSecond;

                //Debug.Log(ServiceDescription.Data + " Latency: " + (int)(ServerLatency * 1000) + "ms, Offset: " + (int)(ServerTimeOffset * 1000) + "ms");
            }

            return Task.CompletedTask;
        }

        private Task HandleConnectMessage(MessageContainer container)
        {
            if (container?.Type != MessageContainer.MessageType.CONNECT_TO_SERVER)
            {
                return Task.CompletedTask;
            }
            
            if (Role != INetworkService.NetworkServiceRole.SERVER)
            {
                return Task.CompletedTask;
            }

            var message = MessageConnectToServer.Unpack(container);
            var clientInfo = _connections?.Find(x => x.EndPoint.Equals(container.Sender));
            if (clientInfo != null)
            {
                clientInfo.LastSeen = DateTime.UtcNow.Ticks;
                clientInfo.Status = ConnectionStatus.CONNECTED;
                clientInfo.Id = message.ClientId;
                //_connections.Remove(clientInfo);
                //_connectedClients[clientInfo.Id] = clientInfo;
            }
            else
            {
                clientInfo = new ConnectionInfo(container.Sender);
                clientInfo.LastSeen = DateTime.UtcNow.Ticks;
                clientInfo.Status = ConnectionStatus.CONNECTED;
                clientInfo.Id = message.ClientId;
                _connections.Add(clientInfo);
            }

            ClientConnected?.Invoke(this, clientInfo);

            return Task.CompletedTask;
        }

        private Task HandleDisconnectMessage(MessageContainer container)
        {
            if (container?.Type != MessageContainer.MessageType.DISCONNECT_FROM_SERVER)
            {
                return Task.CompletedTask;
            }

            if (Role != INetworkService.NetworkServiceRole.SERVER)
            {
                return Task.CompletedTask;
            }

            var message = MessageDisconnectFromServer.Unpack(container);
            var clientInfo = _connections.Find(x => x.Id.Equals(message.ClientId));
            //var success = _connectedClients.TryGetValue(message.ClientId, out var clientInfo);
            if (clientInfo != null)
            {
                clientInfo.LastSeen = DateTime.UtcNow.Ticks;
                HandleDisconnectingClient(clientInfo);
            }

            return Task.CompletedTask;
        }

        private void HandleDisappearingClient(ConnectionInfo clientInfo)
        {
            if (clientInfo != null)
            {
                Debug.LogWarning("Client missing: " + clientInfo.Id + " Last seen at " + new DateTime(clientInfo.LastSeen, DateTimeKind.Utc));

                if (clientInfo.Status == ConnectionStatus.CONNECTED)
                {
                    clientInfo.Status = ConnectionStatus.MISSING;
                    ClientDisappeared?.Invoke(this, clientInfo);
                }
                else if (clientInfo.Status == ConnectionStatus.IS_CONNECTING)
                {
                    HandleDisconnectingClient(clientInfo);
                }
            }
        }

        private void HandleReappearingClient(ConnectionInfo clientInfo)
        {
            if (clientInfo != null)
            {
                Debug.LogWarning("Client reappeared: " + clientInfo.Id + " Last seen at " + new DateTime(clientInfo.LastSeen, DateTimeKind.Utc));

                clientInfo.LastSeen = DateTime.UtcNow.Ticks;
                if (clientInfo.Status == ConnectionStatus.MISSING)
                {
                    clientInfo.Status = ConnectionStatus.CONNECTED;
                    ClientReappeared?.Invoke(this, clientInfo);
                }
            }
        }

        private void HandleDisconnectingClient(ConnectionInfo clientInfo)
        {
            if (clientInfo != null)
            {
                Debug.LogWarning("Client disconnected: " + clientInfo.Id + " Last seen at " + new DateTime(clientInfo.LastSeen, DateTimeKind.Utc));

                clientInfo.Status = ConnectionStatus.DISCONNECTED;
                //_connectedClients?.Remove(clientInfo.Id);
                _connections?.Remove(clientInfo);
                // ToDo: _transport.DisconnectClient(client.Socket);
                ClientDisconnected?.Invoke(this, clientInfo);
            }
        }

        //private void HandleDisappearingServer(ConnectionInfo serverInfo)
        //{
        //    if (serverInfo != null)
        //    {
        //        Debug.LogWarning("Server missing: " + serverInfo.Id + " Last seen at " + new DateTime(serverInfo.LastSeen, DateTimeKind.Utc));

        //        if (serverInfo.Status == ConnectionStatus.CONNECTED)
        //        {
        //            serverInfo.Status = ConnectionStatus.MISSING;
        //            //ServerDisappeared?.Invoke(this, serverInfo);
        //        }
        //        else if (serverInfo.Status == ConnectionStatus.IS_CONNECTING)
        //        {
        //            HandleDisconnectingServer(serverInfo);
        //        }
        //    }
        //}

        //private void HandleReappearingServer(ConnectionInfo serverInfo)
        //{
        //    if (serverInfo != null)
        //    {
        //        Debug.LogWarning("Server reappeared: " + serverInfo.Id + " Last seen at " + new DateTime(serverInfo.LastSeen, DateTimeKind.Utc));

        //        serverInfo.LastSeen = DateTime.UtcNow.Ticks;
        //        serverInfo.Status = ConnectionStatus.CONNECTED;
        //    }
        //}

        //private void HandleDisconnectingServer(ConnectionInfo serverInfo)
        //{
        //    if (serverInfo != null)
        //    {
        //        Debug.LogWarning("Server disconnected: " + serverInfo.Id + " Last seen at " + new DateTime(serverInfo.LastSeen, DateTimeKind.Utc));

        //        serverInfo.Status = ConnectionStatus.DISCONNECTED;
        //        //_connectedClients?.Remove(serverInfo.Id);
        //        //_connections?.Remove(serverInfo);
        //        // ToDo: _transport.DisconnectClient(client.Socket);
        //        //ClientDisconnected?.Invoke(this, serverInfo);
        //    }
        //}

        /// <summary>
        /// Called when a client establishes a socket connection. This does not mean that the client is ready to receive data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="client"></param>
        private void OnClientConnectionEstablished(object sender, Socket socket)
        {
            var clientInfo = _connections.Find(x => x.EndPoint.Equals(socket.RemoteEndPoint as IPEndPoint));
            if (clientInfo != null)
            {
                HandleReappearingClient(clientInfo);
            }
            else
            {
                _connections?.Add(new ConnectionInfo(socket.RemoteEndPoint as IPEndPoint));
            }
        }

        /// <summary>
        /// Called when the socket connection of a client is closed. It is possible that the client will try to reconnect later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="client"></param>
        private void OnClientConnectionClosed(object sender, Socket socket)
        {
            ConnectionInfo clientInfo = null;
            clientInfo = _connections?.Find(x => x.EndPoint.Equals(socket.RemoteEndPoint as IPEndPoint));

            HandleDisappearingClient(clientInfo);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006:Incorrect message signature", Justification = "Not the same method")]
        private void OnConnectedToServer(object sender, EventArgs args)
        {
            // set status
            Status = INetworkService.NetworkServiceStatus.CONNECTED;
            if (_serverStatus != null)
            {
                _serverStatus.Status = ConnectionStatus.CONNECTED;
                //if (_serverStatus.Status == ConnectionStatus.MISSING || _serverStatus.Status == ConnectionStatus.DISCONNECTED)
                //{
                //    HandleReappearingServer(_serverStatus);
                //}
                //else
                //{
                //    _serverStatus.Status = ConnectionStatus.CONNECTED;
                //}
            }           

            // send connection confirmation
            SendMessage(new MessageConnectToServer(ServiceLocator.Instance.Get<INetworkServiceManager>().ClientId));

            // start latency measurement
            RegisterMessageHandler(MessageContainer.MessageType.PING, HandlePingResponse);
            StartCoroutine(CheckServerConnection());

            ConnectedToServer?.Invoke(this, args);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006:Incorrect message signature", Justification = "Not the same method")]
        private void OnDisconnectedFromServer(object sender, EventArgs args)
        {
            if (Status != INetworkService.NetworkServiceStatus.DISCONNECTED)
            {
                // set status
                Status = INetworkService.NetworkServiceStatus.DISCONNECTED;

                //stop latency measurement
                UnregisterMessageHandler(MessageContainer.MessageType.PING);
                StopCoroutine(CheckServerConnection());

                DisconnectedFromServer?.Invoke(this, args);
            }

            //// try to reconnect
            //if (_transport != null)
            //{
            //    StartAsClient(ServiceDescription, _transport.UseMessageQueue);
            //}

        }
    }
}