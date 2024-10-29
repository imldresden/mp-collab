using System;

namespace IMLD.MixedReality.Network
{
    public interface INetworkClient
    {
        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        public event MessageEventHandler MessageReceived;

        /// <summary>
        /// Event raised when the client has successfully connected to a server.
        /// </summary>
        public event EventHandler ConnectedToServer;

        /// <summary>
        /// Gets a value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused { get; }

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public bool IsConnected { get; }

        public NetworkServiceDescription ServiceDescription { get; }

        /// <summary>
        /// Pauses the handling of network messages.
        /// </summary>
        public void Pause();

        /// <summary>
        /// Restarts the handling of network messages.
        /// </summary>
        public void Unpause();

        /// <summary>
        /// Starts listening for servers.
        /// </summary>
        /// <returns><see langword="true"/> if the client started listening for announcements, <see langword="false"/> otherwise.</returns>
        public bool StartListening(int port);

        /// <summary>
        /// Stops listening for servers.
        /// </summary>
        public void StopListening();

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        public bool ConnectToServer(string ip, int port);

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(MessageContainer message);
    }
}