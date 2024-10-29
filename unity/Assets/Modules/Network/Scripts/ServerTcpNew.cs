using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IMLD.MixedReality.Network
{
    public class ServerTcpNew : NetworkBase
    {
        /// <summary>
        /// Called, whenever a new client connected.
        /// </summary>
        public event SocketEventHandler ClientConnected;
        /// <summary>
        /// Called, whenever a client disconnected.
        /// </summary>
        public event SocketEventHandler ClientDisconnected;

        public event ByteDataHandler DataReceived;

        public int Port
        {
            get { return _localEndpoint.Port; }
        }

        public string Address
        {
            get { return _localEndpoint.Address.ToString(); }
        }

        /// <summary>
        /// Indicates if the server is currently running an listening for new connections.
        /// </summary>
        public bool IsListening
        {
            get { return _isListening;}
        }

        public IReadOnlyList<Socket> Clients
        {
            get { return _connectedSocketList.AsReadOnly(); }
        }

        /// <summary>
        /// The number of currently connected clients.
        /// </summary>
        public int NumberOfConnections
        {
            get { return _connectedSocketCount; }
        }

        public ServerTcpNew(int bufferSize, int maxConnectionCount = 100) : this(0, IPAddress.Any, bufferSize, maxConnectionCount) { }
        public ServerTcpNew(int port, IPAddress address, int bufferSize, int maxConnectionCount = 100)
        {
            _localEndpoint = new IPEndPoint(address, port);
            _maxConnectionCount = maxConnectionCount;
            _bufferSize = bufferSize;
            _socketAsyncReceiveEventArgsPool = new SocketAsyncEventArgsPool(maxConnectionCount);
            _socketAsyncSendEventArgsPool = new SocketAsyncEventArgsPool(maxConnectionCount);
            _acceptedClientsSemaphore = new Semaphore(maxConnectionCount, maxConnectionCount);

            _sendingQueue = new BlockingCollection<MessageData>();
            _cancellationTokenSource = new CancellationTokenSource();
            _sendMessageWorker = new Task(SendQueueMessage, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);

            for (int i = 0; i < maxConnectionCount; i++)
            {
                SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
                socketAsyncEventArgs.Completed += OnIOCompleted;
                socketAsyncEventArgs.SetBuffer(new Byte[bufferSize], 0, bufferSize);
                _socketAsyncReceiveEventArgsPool.Return(socketAsyncEventArgs);
            }

            for (int i = 0; i < maxConnectionCount; i++)
            {
                SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
                socketAsyncEventArgs.Completed += OnIOCompleted;
                socketAsyncEventArgs.SetBuffer(new Byte[bufferSize], 0, bufferSize);
                _socketAsyncSendEventArgsPool.Return(socketAsyncEventArgs);
            }

            waitSendEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Start the server, which will try to listen for incoming data on the specified port.
        /// </summary>
        /// <returns><value>true</value> if the server was successfully started, otherwise <value>false</value>.</returns>
        public bool Start(IPEndPoint localEndPoint)
        {
            if (!_isListening)
            {
                _isListening = true;
                _localEndpoint = localEndPoint;
                _listenSocket = new Socket(_localEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _listenSocket.ReceiveBufferSize = _bufferSize;
                _listenSocket.SendBufferSize = _bufferSize;
                _listenSocket.Bind(_localEndpoint);
                _listenSocket.Listen(_maxConnectionCount);
                _localEndpoint = _listenSocket.LocalEndPoint as IPEndPoint;
                _sendMessageWorker.Start();
                StartAccept(null);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Start()
        {
            return Start(_localEndpoint);
        }

        /// <summary>
        /// Stops the server if it is currently running, freeing the specified port.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            foreach (var socket in _connectedSocketList?.AsReadOnly())
            {
                try
                {
                    if (socket != null && socket.Connected)
                    {
                        socket?.Shutdown(SocketShutdown.Both);
                    }
                }
                finally
                {
                    socket?.Close();
                    socket?.Dispose();
                }
            }

            _connectedSocketList?.Clear();

            try
            {
                if (_listenSocket != null && _listenSocket.Connected)
                {
                    _listenSocket?.Shutdown(SocketShutdown.Both);
                }
            }
            finally
            {
                _listenSocket?.Close();
                _listenSocket?.Dispose();
                _listenSocket = null;
            }
        }

        public void SendToClient(IPEndPoint endpoint, byte[] data)
        {
            var client = _connectedSocketList.Find(x => (x.RemoteEndPoint as IPEndPoint).Equals(endpoint));
            if (client != null)
            {
                _sendingQueue.Add(new MessageData(data, client));
            }
        }

        public void Dispose()
        {
            Stop();
        }


        private IPEndPoint _localEndpoint;
        private bool _isListening = false;
        private const int MessageHeaderSize = 4;
        private int _receivedMessageCount = 0;  //for testing
        private Stopwatch _watch;  //for testing

        private BlockingCollection<MessageData> _sendingQueue;
        //private Thread sendMessageWorker;
        private Task _sendMessageWorker;
        private CancellationTokenSource _cancellationTokenSource;
        private List<Socket> _connectedSocketList = new List<Socket>();

        private static Mutex _mutex = new Mutex();
        private Socket _listenSocket;
        private int _bufferSize;
        private int _connectedSocketCount;
        private int _maxConnectionCount;
        private SocketAsyncEventArgsPool _socketAsyncReceiveEventArgsPool;
        private SocketAsyncEventArgsPool _socketAsyncSendEventArgsPool;
        private Semaphore _acceptedClientsSemaphore;
        private AutoResetEvent waitSendEvent;


        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += (sender, e) => ProcessAccept(e);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            _acceptedClientsSemaphore.WaitOne();
            if (!_listenSocket.AcceptAsync(acceptEventArg))
            {
                ProcessAccept(acceptEventArg);
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                SocketAsyncEventArgs readEventArgs = _socketAsyncReceiveEventArgsPool.Rent();
                if (readEventArgs != null)
                {
                    readEventArgs.UserToken = e.AcceptSocket;
                    Interlocked.Increment(ref _connectedSocketCount);

                    lock (_connectedSocketList)
                    {
                        _connectedSocketList.Add(e.AcceptSocket);
                    }

                    ClientConnected?.Invoke(this, e.AcceptSocket);

                    Console.WriteLine("Client connection accepted. There are {0} clients connected to the server", _connectedSocketCount);
                    if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
                    {
                        ProcessReceive(readEventArgs);
                    }
                }
                else
                {
                    Console.WriteLine("There are no more available sockets to allocate.");
                }
            }
            catch (SocketException ex)
            {
                Socket socket = e.UserToken as Socket;
                Console.WriteLine("Error when processing data received from {0}:\r\n{1}", socket.RemoteEndPoint, ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Accept the next connection request.
            StartAccept(e);
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
                CloseClientSocket(args);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            _socketAsyncSendEventArgsPool.Return(e);
            waitSendEvent.Set();
        }
        private void SendQueueMessage()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _listenSocket != null)
            {
                var messageData = _sendingQueue.Take();
                if (messageData != null)
                {
                    SendMessage(messageData);
                }
            }
        }
        private void SendMessage(MessageData messageData)
        {
            var sendEventArgs = _socketAsyncSendEventArgsPool.Rent();
            if (sendEventArgs != null)
            {
                sendEventArgs.SetBuffer(messageData.Data, 0, messageData.Data.Length);
                sendEventArgs.UserToken = messageData.Socket;
                messageData.Socket.SendAsync(sendEventArgs);
            }
            else
            {
                waitSendEvent.WaitOne();
                SendMessage(messageData);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            var socket = e.UserToken as Socket;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Close();
                socket.Dispose();
            }

            _acceptedClientsSemaphore.Release();
            Interlocked.Decrement(ref _connectedSocketCount);
            lock(_connectedSocketList)
            {
                _connectedSocketList.Remove(socket);
            }

            ClientDisconnected?.Invoke(this, socket);

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", _connectedSocketCount);
            _socketAsyncReceiveEventArgsPool.Return(e);
        }

        private class MessageData
        {
            public byte[] Data;
            public Socket Socket;
            public MessageData(byte[] buffer, Socket recipient)
            {
                Data = buffer;
                Socket = recipient;
            }
        }
    }
}
