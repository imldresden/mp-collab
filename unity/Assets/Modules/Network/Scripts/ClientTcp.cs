using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class ClientTcp : Client
    {
        private Socket _socket;
        private bool _isOpen;
        //private readonly int _bufferSize = 65536;
        private SocketAsyncEventArgsPool _pool;
        private readonly int _bufferSize = 1000000;
        //private readonly int _bufferSize = 8192;

        /// <summary>
        /// Indicates if the client is connected to a server.
        /// </summary>
        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        #region Events
        /// <summary>
        /// Called, when the client successfully connected to a server.
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Called, when the client successfully received data from the server.
        /// </summary>
        public event ByteDataHandler DataReceived;
        /// <summary>
        /// Called, when the client was disconnected from a server.
        /// </summary>
        public event EventHandler Disconnected;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the ClientTcp class.
        /// </summary>
        /// <param name="ipAddress">The if address of the server, to which the client should connect.</param>
        /// <param name="port">The port of the server, to which the client should connect.</param>
        public ClientTcp(string ipAddress, int port)
            : base(ipAddress, port)
        {
            _isOpen = false;
            _socket = null;
            _pool = new SocketAsyncEventArgsPool(5);
        }
        #endregion

        #region Private Methods
        private void Connect_Completed(object sender, SocketAsyncEventArgs args)
        {
            args.Completed -= Connect_Completed;

            if (args.SocketError != SocketError.Success)
            {
                Debug.Log("ClientTcp - ERROR, " + args.SocketError);
                Disconnected?.Invoke(this, EventArgs.Empty);
                args.Dispose();
                return;
            }

            args.Dispose();

            _isOpen = true;
            Connected?.Invoke(this, EventArgs.Empty);

            //var receiveArgs = new SocketAsyncEventArgs();
            //receiveArgs.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            var receiveArgs = _pool.Rent();
            receiveArgs.SetBuffer(ArrayPool<byte>.Shared.Rent(_bufferSize), 0, _bufferSize);

            receiveArgs.Completed += Receive_Completed;

            if(!_socket.ReceiveAsync(receiveArgs))
            {
                Receive_Completed(this, receiveArgs);
            }
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Debug.Log("ClientTcp - ERROR receiving data: " + args.SocketError);
                args.Completed -= Receive_Completed;

                //args.Dispose();
                ArrayPool<byte>.Shared.Return(args.Buffer);
                _pool.Return(args);

                Close();
                return;
            }
            if (args.BytesTransferred > 0)
            {
                byte[] msg = new byte[args.BytesTransferred];
                Array.Copy(args.Buffer, 0, msg, 0, args.BytesTransferred);

                //var msg = new byte[args.BytesTransferred];
                //args.MemoryBuffer.Slice(args.Offset, args.BytesTransferred).CopyTo(msg); // create copy of data because we need to reuse the args

                OnDataReceived((IPEndPoint)_socket.RemoteEndPoint, msg);
            }

            // if the connection has since been terminated, don't start a new receive operation, but dispose the args to free the resources, etc.
            if (!_isOpen)
            {
                args.Completed -= Receive_Completed;

                ArrayPool<byte>.Shared.Return(args.Buffer);
                _pool.Return(args);
                //args.Dispose();

                return;
            }

            try
            {
                if (!_socket.ReceiveAsync(args))
                {
                    Receive_Completed(this, args);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("ClientTcp - ERROR receiving data:\n\t" + ex.Message);
                Close();
            }
        }

        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                Debug.Log("ClientTcp - ERROR sending data: " + e.SocketError);
            e.Completed -= Send_Completed;
            
            _pool.Return(e);
            //e.Dispose();
        }

        protected virtual void OnDataReceived(IPEndPoint remoteEndPoint, byte[] data)
        {
            if (DataReceived != null)
                DataReceived(this, remoteEndPoint, data);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Tries to open the connection to the server.
        /// Note: If the method return true, this doesn't mean the connection itself was succesful, only that the atempt has been started.
        /// Subscribe to the Connected event to be notified if the connection to the server has been successfully established.
        /// </summary>
        /// <returns>true if the connection attempt has been successfully started, otherwise false.</returns>
        public override bool Open()
        {
            if (_socket != null && _socket.Connected)
            {
                return false;
            }
                
            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            args.Completed += Connect_Completed;
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true;
                _socket.ConnectAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ClientTcp - ERROR, could not connect to device:\n" + e.Message);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Send the specified data to the server. Only works then the client is connected.
        /// </summary>
        /// <param name="data">The data that should be send.</param>
        /// <returns>true if the data has been send, otherwise false.</returns>
        public override bool Send(byte[] data)
        {
            if (!_isOpen)
            {
                return false;
            }

            //var args = new SocketAsyncEventArgs();
            var args = _pool.Rent();

            args.SetBuffer(data);
            args.Completed += Send_Completed;
            try
            {
                _socket.SendAsync(args);
            }
            catch (Exception e)
            {
                Debug.Log("ClientTcp - ERROR while sending data:\n" + e.Message);

                args.Completed -= Send_Completed;

                _pool.Return(args);
                //args.Dispose();

                return false;
            }
            return true;
        }
        /// <summary>
        /// Terminate the connection to the server and free all used resources.
        /// </summary>
        public override void Close()
        {
            _isOpen = false;
            if (_socket != null && _socket.Connected)
            {
                _socket.Kill();
                _socket = null;
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
