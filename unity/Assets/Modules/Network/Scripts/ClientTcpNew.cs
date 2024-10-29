using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IMLD.MixedReality.Network
{
    public class ClientTcpNew: NetworkBase, IDisposable
    {
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

        /// <summary>
        /// The remote ip address to which the data should be send.
        /// </summary>
        public string Address
        {
            get { return _hostEndPoint.Address.ToString(); }
        }
        /// <summary>
        /// The remote port to which the data should be send.
        /// </summary>
        public int Port
        {
            get { return _hostEndPoint.Port; }
        }
        /// <summary>
        /// Indicates if the sender is ready to send data to the remote connection.
        /// </summary>
        public bool IsConnected { get { return _clientSocket.Connected; } }

        /// <summary>
        /// Create a new instance of this client.
        /// </summary>
        /// <param name="ipAddress">The if address of the server, to which the client should connect.</param>
        /// <param name="port">The port of the server, to which the client should connect.</param>
        public ClientTcpNew(string ipAdress, int port) : this(new IPEndPoint(IPAddress.Parse(ipAdress), port)) { }

        public ClientTcpNew(IPEndPoint hostEndPoint)
        {
            _hostEndPoint = hostEndPoint;
        }

        /// <summary>
        /// Tries to open the socket connection and, depending on the used protocol, tries to establish a connection with the remote host.
        /// </summary>
        /// <returns><value>true</value> if the socket was opened successfully, otherwise <value>false</value>.</returns>
        public void Connect()
        {
            _autoSendEvent = new AutoResetEvent(false);
            _sendingQueue = new BlockingCollection<byte[]>();
            _receivedMessageQueue = new BlockingCollection<byte[]>();
            _clientSocket = new Socket(this._hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _cancellationTokenSource = new CancellationTokenSource();
            _sendMessageWorker = new Task(SendQueueMessage, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);

            _sendEventArgs = new SocketAsyncEventArgs();
            _sendEventArgs.UserToken = this._clientSocket;
            _sendEventArgs.RemoteEndPoint = this._hostEndPoint;
            _sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

            _receiveEventArgs = new SocketAsyncEventArgs();
            _receiveEventArgs.UserToken = _clientSocket;
            _receiveEventArgs.RemoteEndPoint = this._hostEndPoint;
            _receiveEventArgs.SetBuffer(new Byte[bufferSize], 0, bufferSize);
            _receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);

            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.UserToken = this._clientSocket;
            connectArgs.RemoteEndPoint = this._hostEndPoint;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

            _clientSocket.ConnectAsync(connectArgs);
        }

        /// <summary>
        /// Tries to send the specified data to the remote host.
        /// </summary>
        /// <param name="data">The data which should be send.</param>
        /// <returns><value>true</value> if the sending operation was successful, otherwise <value>false</value>.</returns>
        public bool Send(byte[] message)
        {
            _sendingQueue.Add(message);
            return true;
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                if (_clientSocket != null && _clientSocket.Connected)
                {
                    _clientSocket?.Shutdown(SocketShutdown.Both);
                }
            }
            finally
            {
                _clientSocket?.Close();
                _clientSocket?.Dispose();
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect()
        {
            _clientSocket?.Disconnect(false);
        }


        /// <summary>
        /// Free all resources used by this Client.
        /// </summary>
        public void Dispose()
        {
            Close();
        }




        private int bufferSize = 60000;
        private const int MessageHeaderSize = 4;

        private string _ipAddress;
        private int _port;

        private Socket _clientSocket;
        private IPEndPoint _hostEndPoint;
        private AutoResetEvent _autoSendEvent;
        private SocketAsyncEventArgs _sendEventArgs;
        private SocketAsyncEventArgs _receiveEventArgs;
        private BlockingCollection<byte[]> _sendingQueue;
        private BlockingCollection<byte[]> _receivedMessageQueue;
        private Task _sendMessageWorker;
        private CancellationTokenSource _cancellationTokenSource;

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            SocketError errorCode = e.SocketError;
            if (errorCode != SocketError.Success)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                return;
                //throw new SocketException((Int32)errorCode);
            }
            Connected?.Invoke(this, EventArgs.Empty);
            _sendMessageWorker.Start();

            if (!_clientSocket.ReceiveAsync(_receiveEventArgs))
            {
                ProcessReceive(_receiveEventArgs);
            }
        }
        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            SocketError errorCode = e.SocketError;
            if (errorCode != SocketError.Success)
            {
                ProcessError(e);
            }
            else
            {
                _autoSendEvent.Set();
            }
        }
        private void SendQueueMessage()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _clientSocket != null)
            {
                var message = _sendingQueue.Take();
                if (message != null)
                {
                    _sendEventArgs.SetBuffer(message, 0, message.Length);
                    _clientSocket.SendAsync(_sendEventArgs);
                    _autoSendEvent.WaitOne();
                }
            }
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                Socket socket = args.UserToken as Socket;

                byte[] msg = new byte[args.BytesTransferred];
                Buffer.BlockCopy(args.Buffer, 0, msg, 0, args.BytesTransferred);
                //OnDataReceived((IPEndPoint)socket.RemoteEndPoint, msg);
                DataReceived?.Invoke(this, (IPEndPoint)socket.RemoteEndPoint, msg);

                if (socket != null && !socket.ReceiveAsync(args))
                {
                    ProcessReceive(args);
                }
            }
            else
            {
                ProcessError(args);
            }
        }

        //private void ProcessReceivedMessage()
        //{
        //    while (true)
        //    {
        //        var message = receivedMessageQueue.Take();
        //        if (message != null)
        //        {
        //            // ToDo do someting with the message
        //        }
        //    }
        //}

        private void ProcessError(SocketAsyncEventArgs e)
        {
            Close();
            //Socket s = e.UserToken as Socket;
            //if (s.Connected)
            //{
            //    // close the socket associated with the client
            //    try
            //    {
            //        s.Shutdown(SocketShutdown.Both);
            //    }
            //    catch (Exception)
            //    {
            //        // throws if client process has already closed
            //    }
            //    finally
            //    {
            //        if (s.Connected)
            //        {
            //            s.Close();
            //        }
            //    }
            //}

            //Disconnected?.Invoke(this, EventArgs.Empty);

            ////// Throw the SocketException
            ////throw new SocketException((Int32)e.SocketError);
        }

        #region IDisposable Members



        #endregion
    }
}
