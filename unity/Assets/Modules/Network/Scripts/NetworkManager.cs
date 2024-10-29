//using IMLD.MixedReality.Core;
//using System;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace IMLD.MixedReality.Network
//{
//    public class NetworkManager : MonoBehaviour, INetworkManager
//    {
//        /// <summary>
//        /// Event raised when connected or disconnected.
//        /// </summary>
//        public event EventHandler<EventArgs> ConnectionStatusChanged;

//        /// <summary>
//        /// Event raised when the list of sessions changes.
//        /// </summary>
//        public event EventHandler<EventArgs> SessionListChanged;

//        public static NetworkManager Instance = null;

//        public string AnnounceMessage = "TTD";

//        [Tooltip("When started as a client, should automatically connect to the first server found.")]
//        public bool AutomaticallyConnectToServer = true;

//        public int Port = 11338;

//        public Dictionary<string, SessionInfo> SessionList { get; } = new Dictionary<string, SessionInfo>();

//        public int ClientCounter { get; private set; } = 0;

//        public bool IsConnected { get; private set; }

//        public bool IsServer { get; set; }

//        public SessionInfo Session { get; private set; }

//        public NetworkServiceDescription.ServiceType ServiceType => throw new NotImplementedException();

//        private INetworkClient Client;
//        private INetworkServer Server;
//        private Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>> MessageHandlers;

//        public void JoinSession(SessionInfo session)
//        {
//            SessionInfo sessionInfo;
//            if (SessionList.TryGetValue(session.SessionIp, out sessionInfo) == true)
//            {
//                if (Client != null)
//                {
//                    if (Server != null)
//                    {
//                        Server.StopServer();
//                    }
                    
//                    Client.StopListening();
//                    Client.ConnectToServer(session.SessionIp, session.SessionPort);
//                    Session = session;
//                    IsServer = false;
//                }
//            }
//        }

//        public void Pause()
//        {
//            if (Client != null)
//            {
//                Client.Pause();
//            }

//            if (Server != null)
//            {
//                Server.Pause();
//            }
//        }

//        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler)
//        {
//            try
//            {
//                MessageHandlers[messageType] = messageHandler;
//            }
//            catch (Exception exp)
//            {
//                Debug.LogError("Registering message handler failed! Original error message: " + exp.Message);
//                return false;
//            }
//            return true;
//        }

//        public void SendMessage(IMessage message)
//        {
//            SendMessage(message.Pack());
//        }

//        public bool StartAsServer()
//        {
//            if (!enabled)
//            {
//                Debug.Log("Network Manager disabled, cannot start server!");
//                return false;
//            }

//            if (Server == null)
//            {
//                Debug.Log("Network transport not ready, cannot start server!");
//                return false;
//            }

//            Debug.Log("Starting as server");
//            //bool Success = Server.StartServer(Port, AnnounceMessage);
//            bool Success = Server.StartServer(AnnounceMessage, NetworkServiceDescription.ServiceType.APP_STATE);
//            if (Success)
//            {
//                if (Client != null)
//                {
//                    Client.StopListening();
//                }
                
//                IsServer = true;
//                ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
//            }
//            return Success;
//        }

//        public void Unpause()
//        {
//            if (Client != null)
//            {
//                Client.Unpause();
//            }

//            if (Server != null)
//            {
//                Server.Unpause();
//            }
//        }

//        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType)
//        {
//            return MessageHandlers.Remove(messageType);
//        }

//        public bool StartAsClient()
//        {
//            if (!enabled)
//            {
//                Debug.Log("Network Manager disabled, cannot start client!");
//                return false;
//            }

//            if (Client == null)
//            {
//                Debug.Log("Network transport not ready, cannot start client!");
//                return false;
//            }

//            Debug.Log("Starting as client");
//            IsServer = false;
//            bool Success = Client.StartListening(Port);
//            return Success;
//        }

//        private void Awake()
//        {
//            MessageHandlers = new Dictionary<MessageContainer.MessageType, Func<MessageContainer, Task>>();

//            // Singleton pattern implementation
//            if (Instance != null && Instance != this)
//            {
//                Destroy(gameObject);
//            }

//            Instance = this;
//        }

//        private void OnConnectedToServer(object sender, EventArgs args)
//        {
//            IsConnected = true;
//            ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
//        }

//        private async void OnMessageReceived(object sender, MessageContainer message)
//        {
//            if (MessageHandlers != null)
//            {
//                Func<MessageContainer, Task> callback;
//                if (MessageHandlers.TryGetValue(message.Type, out callback) && callback != null)
//                {
//                    await callback(message);
//                }
//                else
//                {
//                    Debug.Log("Unknown message: " + message.Type.ToString() + " with content: " + message.Payload);
//                }
//            }
//        }

//        private void OnClientConnected(object sender, Socket client)
//        {
//            if (!IsServer || Server == null) return;

//            // assign id to client
//            var ClientMessage = new MessageAcceptClient(ClientCounter++);
//            Server.SendToClient(ClientMessage.Pack(), client);

//            // do your own session handling...
//        }

//        private Task OnBroadcastData(MessageContainer obj)
//        {
//            Debug.Log("Received broadcast!");
//            MessageAnnouncement Message = MessageAnnouncement.Unpack(obj); // deserialize message
//            if (Message != null && Message.Service.Description.Equals(AnnounceMessage)) // check if the announcement strings matches
//            {
//                SessionInfo sessionInfo;
//                if (SessionList.TryGetValue(Message.Service.IP, out sessionInfo) == false)
//                {
//                    // add to session list
//                    sessionInfo = new SessionInfo() { SessionName = Message.Service.HostName, SessionIp = Message.Service.IP, SessionPort = Message.Service.Port };
//                    SessionList.Add(Message.Service.IP, sessionInfo);
//                    // trigger event to notify about new session
//                    SessionListChanged?.Invoke(this, EventArgs.Empty);
//                    if (AutomaticallyConnectToServer == true)
//                    {
//                        JoinSession(sessionInfo);
//                    }
//                }
//            }
//            return Task.CompletedTask;
//        }

//        // Start is called before the first frame update
//        private void Start()
//        {
//            // register network server/client
//            Client = ServiceLocator.Instance.Get<INetworkClient>();
//            Server = ServiceLocator.Instance.Get<INetworkServer>();

//            // add callbacks
//            Client.ConnectedToServer += OnConnectedToServer;
//            Client.MessageReceived += OnMessageReceived;
//            Server.ClientConnected += OnClientConnected;
//            //Server.ClientDisconnected += OnClientDisconnected;
//            Server.MessageReceived += OnMessageReceived;

//            // registers callback for announcement handling
//            RegisterMessageHandler(MessageContainer.MessageType.ANNOUNCEMENT, OnBroadcastData);

//            if (AutomaticallyConnectToServer == true)
//            {
//                StartAsClient();
//            }
//        }

//        private void SendMessage(MessageContainer message)
//        {
//            if (IsServer && Server != null)
//            {
//                Server.SendToAll(message);
//            }
//            else if (IsConnected && Client != null)
//            {
//                Client.SendToServer(message);
//            }
//        }
//    }
//}