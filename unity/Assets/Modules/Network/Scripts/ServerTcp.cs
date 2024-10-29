using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public delegate void SocketEventHandler(object sender, Socket socket);
    public class ServerTcp : Server
    {
        #region Private Fields
        private readonly int _bufferSize;

        private Socket _listener;
        private bool _isListening;
        private List<Socket> _clients;
        private SocketAsyncEventArgsPool _pool;
        #endregion

        #region Events
        /// <summary>
        /// Called, whenever a new client connected.
        /// </summary>
        public event SocketEventHandler ClientConnected;
        /// <summary>
        /// Called, whenever a client disconnected.
        /// </summary>
        public event SocketEventHandler ClientDisconnected;
        #endregion

        #region Public Properties
        /// <summary>
        /// Indicates if the server is currently running an listening for new connections.
        /// </summary>
        public override bool IsListening
        {
            get { return _isListening; }
        }

        public IPEndPoint IPEndPoint { get { return _listener.LocalEndPoint as IPEndPoint; } }

        /// <summary>
        /// The number of currently connected clients.
        /// </summary>
        public int NumberOfConnections
        {
            get { return _clients.Count; }
        }
        /// <summary>
        /// A read-only list of all currently connected clients.
        /// </summary>
        public ReadOnlyCollection<Socket> Clients { get; private set; }
        #endregion

        #region Constructors
        public ServerTcp(int port, int bufferSize = 1000000)
            : base(port)
        {
            _isListening = false;
            _bufferSize = bufferSize;
            _clients = new List<Socket>();
            Clients = new ReadOnlyCollection<Socket>(_clients);
            _pool = new SocketAsyncEventArgsPool(10);
        }
        #endregion

        #region Private Methods
        private void Accept_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                if (args.SocketError != SocketError.OperationAborted)
                    Debug.Log("ServerTcp - ERROR '" + args.SocketError.ToString() + "'");
                return;
            }

            // Prepare for data being transmitted by the newly accepted connection
            //var clientArgs = new SocketAsyncEventArgs();
            //clientArgs.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            var clientArgs = _pool.Rent();
            clientArgs.SetBuffer(ArrayPool<byte>.Shared.Rent(_bufferSize), 0, _bufferSize);

            clientArgs.Completed += Receive_Completed;
            _clients.Add(args.AcceptSocket);
            if (ClientConnected != null)
                ClientConnected(this, args.AcceptSocket);
            try
            {
                // receiveAsync might return synchronous, so we handle that too by calling Receive_Completed manually
                if (!args.AcceptSocket.ReceiveAsync(clientArgs))
                    Receive_Completed(args.AcceptSocket, clientArgs);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ClientReceive ERROR:\n" + e.Message);
                DisconnectClient(args.AcceptSocket);
            }
            // Continue listening for other connections
            try
            {
                args.AcceptSocket = null;
                _listener.AcceptAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - Accept ERROR:\n" + e.Message);
                Stop();
            }
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs args)
        {
            var socket = (Socket)sender;
            if (args.SocketError != SocketError.Success)
            {
                Debug.Log("ServerTcp - Socket ERROR '" + args.SocketError.ToString() + "', connection to client terminated");
                args.Completed -= Receive_Completed;

                ArrayPool<byte>.Shared.Return(args.Buffer);
                _pool.Return(args);
                //args.Dispose();

                DisconnectClient(socket);
                return;
            }

            // if the connection was terminated at the other side, so terminate this side to
            if (args.BytesTransferred == 0)
            {
                DisconnectClient(socket);
                return;
            }

            byte[] msg = new byte[args.BytesTransferred];
            Array.Copy(args.Buffer, 0, msg, 0, args.BytesTransferred);
            //byte[] msg = new byte[args.BytesTransferred];
            //args.MemoryBuffer.Slice(args.Offset, args.BytesTransferred).CopyTo(msg); // create copy of data because we need to reuse the args
           
            OnDataReceived((IPEndPoint)socket.RemoteEndPoint, msg);

            // Continue receiving data from this socket
            try
            {
                // receiveAsync might return synchronous, so we handle that too by calling Receive_Completed manually
                if (!socket.ReceiveAsync(args))
                    Receive_Completed(socket, args);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ERROR, connection to client closed:\n\t" + e.Message);
                args.Completed -= Receive_Completed;

                ArrayPool<byte>.Shared.Return(args.Buffer);
                _pool.Return(args);
                //args.Dispose();

                DisconnectClient(socket);
            }
        }

        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                Debug.Log("ServerTcp - ERROR sending data to client: " + e.SocketError);
            e.Completed -= Send_Completed;
            
            _pool.Return(e);
            //e.Dispose();
        }

        private void DisconnectClient(Socket client)
        {
            if (!_clients.Contains(client))
                return;
            if (ClientDisconnected != null)
                ClientDisconnected(this, client);
            client.Kill();
            _clients.Remove(client);
        }
        #endregion

        #region Public Methods
        public override bool Start()
        {
            Stop();
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.NoDelay = true;
                _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
                _listener.Listen(100);

                var args = new SocketAsyncEventArgs();
                args.Completed += Accept_Completed;
                _listener.AcceptAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ERROR, Could not start server:\n\t" + e.Message);
                return false;
            }
            _isListening = true;
            return true;
        }

        public override void Stop()
        {
            _isListening = false;

            foreach (var client in _clients)
                client.Kill();
            _clients.Clear();
            if (_listener != null)
            {
                _listener.Kill();
                _listener = null;
            }
        }

        public void SendToClient(Socket client, byte[] data)
        {
            //var dataArray = data.ToArray();
            //var args = new SocketAsyncEventArgs();

            var args = _pool.Rent();
            args.SetBuffer(data);

            args.Completed += Send_Completed;
            try
            {
                if (!client.SendAsync(args))
                    Send_Completed(client, args);
            }
            catch (Exception e)
            {
                Debug.Log("ServerTcp - ERROR, Could not send data to client:\n\t" + e.Message);
                args.Completed -= Send_Completed;

                _pool.Return(args);
                //args.Dispose();
            }
        }

        public override void Dispose()
        {
            Stop();
        }
        #endregion
    }
}
