using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace IMLD.MixedReality.Network
{
    public delegate void MessageEventHandler(object sender, MessageContainer message);

    public interface INetworkServer
    {
        /// <summary>
        /// Event raised when a client connected.
        /// </summary>
        public event SocketEventHandler ClientConnectionEstablished;

        /// <summary>
        /// Event raised when a client disconnected.
        /// </summary>
        public event SocketEventHandler ClientConnectionClosed;

        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        public event MessageEventHandler MessageReceived;

        /// <summary>
        /// Gets a value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused { get; }

        /// <summary>
        /// Gets a value indicating the port that the server is running on.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets a value indicating the server name.
        /// </summary>
        public string ServerName { get; }

        public string Network { get; set; }

        /// <summary>
        /// Gets a value indicating the server IPs.
        /// </summary>
        public IReadOnlyList<string> ServerIPs { get; }

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
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="message">The message to announce the server with.</param>
        /// <returns><see langword="true"/> if the server started successfully, <see langword="false"/> otherwise.</returns>
        public bool StartServer(NetworkServiceDescription serviceDescription);

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer();

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToAll(MessageContainer message);

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="endpoint">The client to send the message to.</param>
        public void SendToClient(MessageContainer message, IPEndPoint endpoint);

    }
}