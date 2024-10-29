//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace IMLD.MixedReality.Network
//{
//    public interface INetworkManager
//    {
//        /// <summary>
//        /// Event raised when connected or disconnected.
//        /// </summary>
//        public event EventHandler<EventArgs> ConnectionStatusChanged;

//        /// <summary>
//        /// Event raised when the list of sessions changes.
//        /// </summary>
//        public event EventHandler<EventArgs> SessionListChanged;

//        public Dictionary<string, SessionInfo> SessionList { get; }

//        public int ClientCounter { get; }

//        public bool IsConnected { get; }

//        public bool IsServer { get; set; }

//        public SessionInfo Session { get; }

//        public NetworkServiceDescription.ServiceType ServiceType { get; }

//        public void JoinSession(SessionInfo session);

//        public void Pause();

//        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler);

//        public void SendMessage(IMessage message);

//        public bool StartAsServer();

//        public void Unpause();

//        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType);

//        public bool StartAsClient();

//    }
//}

