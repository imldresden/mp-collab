using System;
using System.Net;
using System.Threading.Tasks;
using static IMLD.MixedReality.Network.NetworkService;

namespace IMLD.MixedReality.Network
{
    public delegate void ClientEventHandler(object sender, ConnectionInfo client);

    [Serializable]
    public class ConnectionInfo
    {
        public IPEndPoint EndPoint;
        public ConnectionStatus Status;
        public long LastSeen;
        public Guid Id;

        public ConnectionInfo(IPEndPoint endpoint)
        {
            EndPoint = endpoint;
            Status = ConnectionStatus.IS_CONNECTING;
            LastSeen = DateTime.UtcNow.Ticks;
            Id = Guid.Empty;
        }
    }

    public enum ConnectionStatus
    {
        UNKNOWN,
        DISCONNECTED,
        IS_CONNECTING,
        CONNECTED,
        MISSING
    }

    public interface INetworkService
    {
        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;
        public event ClientEventHandler ClientReappeared;
        public event ClientEventHandler ClientDisappeared;
        public event EventHandler ConnectedToServer;
        public event EventHandler DisconnectedFromServer;
        public NetworkServiceRole Role { get; }
        public NetworkServiceDescription.ServiceType ServiceType { get; set; }
        public NetworkServiceDescription ServiceDescription { get; }
        public NetworkServiceStatus Status { get; }
        public bool StartAsClient(NetworkServiceDescription serviceDescription, bool useMessageQueue = true);
        public bool StartAsServer(bool useMessageQueue = true);
        public void Destroy();
        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler);
        public bool UnregisterMessageHandler(MessageContainer.MessageType messageType);
        public void SendMessage(IMessage message);
        public void SendMessage(MessageContainer message);
        public void UpdateRoom(RoomDescription room);

        /// <summary>
        /// The estimated latency of the server connection (half RTT) in seconds
        /// </summary>
        public float ServerLatency { get; }

        /// <summary>
        /// The estimated time offset of the server in seconds
        /// </summary>
        public float ServerTimeOffset { get; }

        /// <summary>
        /// The total network latency in seconds that the network service should simulate by adding additional wait times
        /// </summary>
        public float RequestedLatency { get; set; }

        public INetworkFilter NetworkFilter { get; set; }

        public enum NetworkServiceRole
        {
            UNDEFINED,
            CLIENT,
            SERVER
        }

        [Flags]
        public enum NetworkServiceFilter
        {
            SAME_ROOM_ID = 1,
            DIFFERENT_ROOM_ID = 2,
            APP_STATE = 4,
            ALL = SAME_ROOM_ID | DIFFERENT_ROOM_ID | APP_STATE,
            DEFAULT = DIFFERENT_ROOM_ID | APP_STATE
        }

        public enum NetworkServiceStatus
        {
            UNDEFINED,
            DISCONNECTED,
            CONNECTING,
            CONNECTED
        }

    }
}