using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using IMLD.MixedReality.Network;
using System;
using System.Net.Sockets;
using IMLD.MixedReality.Avatars;
using System.Net;

public class TestNetwork
{
    GameObject _serverGO;
    GameObject _clientGO;
    NetworkTransport _server;
    NetworkTransport _client;
    NetworkServiceDescription _serviceDescription;
    bool _onConnectedToServerCalled = false;
    bool _onClientConnectedCalled = false;
    bool _onMessageReceived = false;
    int _messageCounter = 0;
    const int LARGE_MESSAGE_SIZE = 1000 * 1000;
    const int MEDIUM_MESSAGE_SIZE = 30 * 1000;
    Guid _kinectId = Guid.NewGuid();
    byte[] _receiveBuffer;
    int _writeCounter;
    List<byte[]> _sendDataBuffers = new List<byte[]>();
    List<byte[]> _receiveDataBuffers = new List<byte[]>();
    List<MessageAudioData> _sendMsgs = new List<MessageAudioData>();

    [SetUp]
    public void Setup()
    {
        // create game objects
        _clientGO = new GameObject("Client");
        _client = _clientGO.AddComponent<NetworkTransport>();
        _serverGO = new GameObject("Server");
        _server = _serverGO.AddComponent<NetworkTransport>();

        _serviceDescription = new NetworkServiceDescription
        {
            RoomId = 0,
            Data = "data",
            ServiceId = Guid.NewGuid(),
            SessionId = Guid.NewGuid()
        };

        _onConnectedToServerCalled = false;
        _onClientConnectedCalled = false;
        _onMessageReceived = false;
        _messageCounter = 0;
    }

    [TearDown]
    public void Teardown()
    {
        if (_client != null)
        {
            _client.Drop();
        }

        if (_server != null)
        {
            _server.StopServer();
        }

        GameObject.DestroyImmediate(_serverGO);
        GameObject.DestroyImmediate(_clientGO);

        _client = null;
        _server = null;
    }

    //// A Test behaves as an ordinary method
    //[Test]
    //public void TestNetworkSimplePasses()
    //{


    //}

    [UnityTest]
    public IEnumerator LowLevelTcp()
    {
        ServerTcp server = null;
        ClientTcp client = null;
        var rnd = new System.Random();

        try
        {
            server = new ServerTcp(11338);
            client = new ClientTcp("127.0.0.1", 11338);
            bool result = server.Start();

            yield return new WaitForSeconds(0.1f);

            Assert.That(result, Is.True);
            Assert.That(server.IsListening, Is.True);
            Assert.That(server.Clients.Count, Is.EqualTo(0));

            client.Open();

            yield return new WaitForSeconds(0.1f);

            Assert.That(client.IsOpen, Is.True);
            Assert.That(server.Clients.Count, Is.EqualTo(1));

            server.DataReceived += TcpDataReceived;
            client.DataReceived += TcpDataReceived;

            for(int i = 0; i < 10; i++)
            {
                // fill buffer of random size with random content
                int size = rnd.Next(100000, 1000000);
                _receiveBuffer = new byte[size];
                _writeCounter = 0;
                byte[] sendBuffer;
                rnd.NextBytes(sendBuffer = new byte[size]);

                // send buffer
                Debug.Log("Sending test buffer of " + size + " bytes to server.");
                client.Send(sendBuffer);
                yield return new WaitForSeconds(0.5f);

                // check content
                for (int j = 0; j < sendBuffer.Length; j++)
                {
                    if (sendBuffer[j] != _receiveBuffer[j])
                    {
                        Assert.Fail();
                    }
                }
            }

            for (int i = 0; i < 10; i++)
            {
                // fill buffer of random size with random content
                int size = rnd.Next(100000, 1000000);
                _receiveBuffer = new byte[size];
                _writeCounter = 0;
                byte[] sendBuffer;
                rnd.NextBytes(sendBuffer = new byte[size]);

                // send buffer
                Debug.Log("Sending test buffer of " + size + " bytes to client.");
                server.SendToClient(server.Clients[0], sendBuffer);
                yield return new WaitForSeconds(0.5f);

                // check content
                for (int j = 0; j < sendBuffer.Length; j++)
                {
                    if (sendBuffer[j] != _receiveBuffer[j])
                    {
                        Assert.Fail();
                    }
                }
            }

            client.Close();

            yield return new WaitForSeconds(0.1f);

            Assert.That(client.IsOpen, Is.False);
            Assert.That(server.Clients.Count, Is.EqualTo(0));

            server.Stop();

            yield return new WaitForSeconds(0.1f);

            Assert.That(server.IsListening, Is.False);
        }
        finally
        {
            client.Dispose();
            server.Dispose();            
        }

        yield return null;
    }

    private void TcpDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
    {
        Assert.That(data, Is.Not.Null);
        Assert.That(_writeCounter + data.Length, Is.LessThanOrEqualTo(_receiveBuffer.Length));
        Array.Copy(data, 0, _receiveBuffer, _writeCounter, data.Length);
        _writeCounter += data.Length;
    }

    [UnityTest]
    public IEnumerator NetworkTransportDataParsing()
    {
        // setup callbacks
        _client.MessageReceived += OnInjectedAudioMessageReceived;

        // inject various data chunks into client
        _onMessageReceived = false;
        _messageCounter = 0;
        System.Random rnd = new System.Random();
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11338);

        // create random sized messages
        int totalSize = 0;
        for (int i = 0; i < 100; i++)
        {
            byte[] data = new byte[rnd.Next(1000, 10000)];
            rnd.NextBytes(data);
            var msg = new MessageAudioData(Guid.NewGuid(), i, data);
            _sendMsgs.Add(msg);
            var msgData = msg.Pack().Serialize();
            _sendDataBuffers.Add(msgData);
            totalSize += msgData.Length;
        }

        // build large buffer
        byte[] largeBuffer = new byte[totalSize];
        int dstOffset = 0;
        for (int i = 0; i < 100; i++)
        {
            Buffer.BlockCopy(_sendDataBuffers[i], 0, largeBuffer, dstOffset, _sendDataBuffers[i].Length);
            dstOffset += _sendDataBuffers[i].Length;
        }

        // inject data in random chunks
        int byteCounter = 0;
        while(byteCounter < totalSize)
        {
            // compute random chunk size
            int n = Math.Min(rnd.Next(100, 1000), totalSize - byteCounter);

            // take chunk
            byte[] data = new byte[n];
            Buffer.BlockCopy(largeBuffer, byteCounter, data, 0, n);

            // inject data
            _client.InjectData(this, endpoint, data);

            // update counter
            byteCounter += n;
        }

        yield return new WaitForSeconds(1f);

        Assert.That(_messageCounter , Is.EqualTo(100));

        for (int i = 0; i < 100; i++)
        {
            var dataSent = _sendMsgs[i].ByteData;
            var dataReceived = _receiveDataBuffers[i];
            Assert.That(dataSent.Length , Is.EqualTo(dataReceived.Length));
            for (int j = 0 ; j < dataSent.Length; j++)
            {
                if (dataSent[j] != dataReceived[j])
                {
                    Assert.Fail();
                }
            }
        }

        yield return null;
    }

    private void OnInjectedAudioMessageReceived(object sender, MessageContainer message)
    {
        Assert.NotNull(message);
        MessageAudioData msg = MessageAudioData.Unpack(message);
        Assert.NotNull(msg);
        _receiveDataBuffers.Add(msg.ByteData);
        _onMessageReceived = true;
        _messageCounter++;
    }

    [UnityTest]
    public IEnumerator ServiceAnnouncement()
    {
        StartNetworkTransportServer();

        _client.MessageReceived += OnBroadcastReceived;
        _onMessageReceived = false;
        _client.StartListening(11338);

        // wait for broadcasts...
        yield return new WaitForSeconds(2f);

        // Assert
        Assert.IsTrue(_onMessageReceived);

        yield return null;
    }

    [UnityTest]
    public IEnumerator BasicConnection()
    {
        StartNetworkTransportServer();
        ConnectNetworkTransportClient();

        // wait to give client time to connect to server...
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(_client.IsConnected); // Did the client connect successfully?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1)); // Does the server have exactly one connected client now?
        Assert.IsTrue(_onClientConnectedCalled); // Did the OnClientConnected callback get called?
        Assert.IsTrue(_onConnectedToServerCalled); // Did the OnConnectedToServer callback get called?

        // close connection
        _client.Drop();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(_client.IsConnected); // Did the connection close?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(0)); // Does the server have no clients connected?

        yield return null;
    }

    [UnityTest]
    public IEnumerator BasicDataTransmission()
    {
        StartNetworkTransportServer();
        ConnectNetworkTransportClient();

        // wait to give client time to connect to server...
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(_client.IsConnected); // Did the client connect successfully?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1)); // Does the server have exactly one connected client now?
        Assert.IsTrue(_onClientConnectedCalled); // Did the OnClientConnected callback get called?
        Assert.IsTrue(_onConnectedToServerCalled); // Did the OnConnectedToServer callback get called?

        // setup callbacks
        _client.MessageReceived += OnPingMessageReceived;
        _server.MessageReceived += OnPingMessageReceived;

        // transmit data from client to server
        _onMessageReceived = false;
        _client.SendToServer(new MessagePing().Pack());
        yield return new WaitForSeconds(0.1f);
        Assert.True(_onMessageReceived);

        // transmit data from server to the client
        _onMessageReceived = false;
        _server.SendToAll(new MessagePing().Pack());
        yield return new WaitForSeconds(0.1f);
        Assert.True(_onMessageReceived);

        // close connection
        _client.Drop();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(_client.IsConnected); // Did the connection close?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(0)); // Does the server have no clients connected?

        yield return null;
    }

    [UnityTest]
    public IEnumerator LargeDataTransmission()
    {
        StartNetworkTransportServer();
        ConnectNetworkTransportClient();

        // wait to give client time to connect to server...
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(_client.IsConnected); // Did the client connect successfully?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1)); // Does the server have exactly one connected client now?
        Assert.IsTrue(_onClientConnectedCalled); // Did the OnClientConnected callback get called?
        Assert.IsTrue(_onConnectedToServerCalled); // Did the OnConnectedToServer callback get called?

        // setup callbacks
        _client.MessageReceived += OnLargeAudioMessageReceived;
        _server.MessageReceived += OnLargeAudioMessageReceived;

        // transmit large data chunk from client to server
        var msg = new MessageAudioData(Guid.NewGuid(), 1, new byte[LARGE_MESSAGE_SIZE]); // a very large message
        _onMessageReceived = false;
        _client.SendToServer(msg.Pack());
        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);

        // transmit large data chunk from server to client
        msg = new MessageAudioData(Guid.NewGuid(), 1, new byte[LARGE_MESSAGE_SIZE]); // a very large message
        _onMessageReceived = false;
        _server.SendToAll(msg.Pack());
        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);

        // close connection
        _client.Drop();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(_client.IsConnected); // Did the connection close?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(0)); // Does the server have no clients connected?

        yield return null;
    }

    [UnityTest]
    public IEnumerator MultiplePackageDataTransmission()
    {
        StartNetworkTransportServer();
        ConnectNetworkTransportClient();

        // wait to give client time to connect to server...
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(_client.IsConnected); // Did the client connect successfully?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1)); // Does the server have exactly one connected client now?
        Assert.IsTrue(_onClientConnectedCalled); // Did the OnClientConnected callback get called?
        Assert.IsTrue(_onConnectedToServerCalled); // Did the OnConnectedToServer callback get called?

        // setup callbacks
        _client.MessageReceived += OnMediumAudioMessageReceived;
        _server.MessageReceived += OnMediumAudioMessageReceived;

        // transmit multiple data chunks from client to server
        _onMessageReceived = false;
        _messageCounter = 0;
        for (int i = 0; i < 100; i++)
        {
            var msg = new MessageAudioData(Guid.NewGuid(), i, new byte[MEDIUM_MESSAGE_SIZE]);
            _client.SendToServer(msg.Pack());
        }

        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);
        Assert.That(_messageCounter, Is.EqualTo(100));

        // transmit multiple data chunks from server to client
        _onMessageReceived = false;
        _messageCounter = 0;
        for (int i = 0; i < 100; i++)
        {
            var msg = new MessageAudioData(Guid.NewGuid(), i, new byte[MEDIUM_MESSAGE_SIZE]);
            _server.SendToAll(msg.Pack());
        }

        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);
        Assert.That(_messageCounter, Is.EqualTo(100));

        // close connection
        _client.Drop();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(_client.IsConnected); // Did the connection close?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(0)); // Does the server have no clients connected?

        yield return null;
    }

    [UnityTest]
    public IEnumerator TestMessagePointCloudData()
    {
        StartNetworkTransportServer();
        ConnectNetworkTransportClient();

        // wait to give client time to connect to server...
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(_client.IsConnected); // Did the client connect successfully?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1)); // Does the server have exactly one connected client now?
        Assert.IsTrue(_onClientConnectedCalled); // Did the OnClientConnected callback get called?
        Assert.IsTrue(_onConnectedToServerCalled); // Did the OnConnectedToServer callback get called?

        // setup callbacks
        _client.MessageReceived += OnMessagePointCloud;
        _server.MessageReceived += OnMessagePointCloud;

        // transmit data from client to server
        _onMessageReceived = false;
        _messageCounter = 0;
        System.Random rnd = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            int n = rnd.Next(5, 10000);
            var pointData = new byte[n*9];
            rnd.NextBytes(pointData);
            var dataFrame = new PointCloudDataFrame()
            {
                KinectId = _kinectId,
                Data = pointData,
                RoomId = 0,
                TimestampInMs = 1337
            };

            var msg = new MessagePointCloud(dataFrame);
            _client.SendToServer(msg.Pack());
        }

        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);
        Assert.That(_messageCounter, Is.EqualTo(100));

        // transmit data from server to client
        _onMessageReceived = false;
        _messageCounter = 0;
        rnd = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            int n = rnd.Next(5, 10000);
            var pointData = new byte[n * 9];
            rnd.NextBytes(pointData);
            var dataFrame = new PointCloudDataFrame()
            {
                KinectId = _kinectId,
                Data = pointData,
                RoomId = 0,
                TimestampInMs = 1337
            };

            var msg = new MessagePointCloud(dataFrame);
            _server.SendToAll(msg.Pack());
        }

        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);
        Assert.That(_messageCounter, Is.EqualTo(100));

        // close connection
        _client.Drop();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(_client.IsConnected); // Did the connection close?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(0)); // Does the server have no clients connected?

        yield return null;
    }

    [UnityTest]
    public IEnumerator TestMessageSkeletonData()
    {
        StartNetworkTransportServer();
        ConnectNetworkTransportClient();

        // wait to give client time to connect to server...
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsTrue(_client.IsConnected); // Did the client connect successfully?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1)); // Does the server have exactly one connected client now?
        Assert.IsTrue(_onClientConnectedCalled); // Did the OnClientConnected callback get called?
        Assert.IsTrue(_onConnectedToServerCalled); // Did the OnConnectedToServer callback get called?

        // setup callbacks
        _client.MessageReceived += OnMessageSkeletonData;
        _server.MessageReceived += OnMessageSkeletonData;

        // transmit data from client to server
        _onMessageReceived = false;
        _messageCounter = 0;
        System.Random rnd = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            int n = rnd.Next(1, 10);
            var bodies = new NetworkedBody[n];
            for (int j = 0; j < n; j++)
            {
                bodies[j] = new NetworkedBody(32);
            }

            var dataFrame = new SkeletonDataFrame()
            {
                Bodies = bodies,
                KinectId = _kinectId,
                NumOfBodies = bodies.Length,
                RoomId = 0,
                TimestampInMs = 1337
            };

            var msg = new MessageSkeletonData(dataFrame);
            _client.SendToServer(msg.Pack());
        }

        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);
        Assert.That(_messageCounter, Is.EqualTo(100));

        // transmit data from server to client
        _onMessageReceived = false;
        _messageCounter = 0;
        rnd = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            int n = rnd.Next(1, 10);
            var bodies = new NetworkedBody[n];
            for (int j = 0; j < n; j++)
            {
                bodies[j] = new NetworkedBody(32);
            }

            var dataFrame = new SkeletonDataFrame()
            {
                Bodies = bodies,
                KinectId = _kinectId,
                NumOfBodies = bodies.Length,
                RoomId = 0,
                TimestampInMs = 1337
            };

            var msg = new MessageSkeletonData(dataFrame);
            _server.SendToAll(msg.Pack());
        }

        yield return new WaitForSeconds(0.5f);
        Assert.True(_onMessageReceived);
        Assert.That(_messageCounter, Is.EqualTo(100));

        // close connection
        _client.Drop();
        yield return new WaitForSeconds(0.1f);
        Assert.IsFalse(_client.IsConnected); // Did the connection close?
        Assert.That(_server.NumberOfPeers, Is.EqualTo(0)); // Does the server have no clients connected?

        yield return null;
    }
    private void OnMessagePointCloud(object sender, MessageContainer message)
    {
        Assert.NotNull(message);
        var msg = MessagePointCloud.Unpack(message);
        Assert.NotNull(msg);
        Assert.That(msg.Data.KinectId, Is.EqualTo(_kinectId));
        Assert.That(msg.Data.Data.Length % PointCloudDataFrame.POINT_LENGTH, Is.Zero); // length of the data array is a multiple of the length of one point
        _onMessageReceived = true;
        _messageCounter++;
    }

    private void OnMessageSkeletonData(object sender, MessageContainer message)
    {
        Assert.NotNull(message);
        MessageSkeletonData msg = MessageSkeletonData.Unpack(message);
        Assert.NotNull(msg);
        Assert.That(msg.Data.KinectId, Is.EqualTo(_kinectId));
        Assert.That(msg.Data.NumOfBodies, Is.EqualTo(msg.Data.Bodies.Length));
        _onMessageReceived = true;
        _messageCounter++;
    }

    private void OnBroadcastReceived(object sender, MessageContainer message)
    {
        _onMessageReceived = true;
        Assert.NotNull(message);
        MessageAnnouncement msg = MessageAnnouncement.Unpack(message);
        Assert.NotNull(msg);
        Assert.That(msg.Service.Data, Is.EqualTo("data"));
    }

    private void OnLargeAudioMessageReceived(object sender, MessageContainer message)
    {
        _onMessageReceived = true;
        Assert.NotNull(message);
        MessageAudioData msg = MessageAudioData.Unpack(message);
        Assert.NotNull(msg);
        Assert.That(msg.ByteData.Length, Is.EqualTo(LARGE_MESSAGE_SIZE));
    }

    private void OnMediumAudioMessageReceived(object sender, MessageContainer message)
    {
        Assert.NotNull(message);
        MessageAudioData msg = MessageAudioData.Unpack(message);
        Assert.NotNull(msg);
        Assert.That(msg.ByteData.Length, Is.EqualTo(MEDIUM_MESSAGE_SIZE));
        Assert.That(_messageCounter, Is.EqualTo(msg.Channels));
        _onMessageReceived = true;
        _messageCounter++;
    }


    private void OnConnectedToServer(object sender, EventArgs e)
    {
        Assert.That(_client.IsConnected, Is.True);
        _onConnectedToServerCalled = true;
    }

    private void OnClientConnected(object sender, Socket socket)
    {
        Assert.That(_server.NumberOfPeers, Is.EqualTo(1));
        _onClientConnectedCalled = true;
    }

    private void OnPingMessageReceived(object sender, MessageContainer message)
    {
        _onMessageReceived = true;
        Assert.NotNull(message);
        MessagePing msg = MessagePing.Unpack(message);
        Assert.NotNull(msg);
    }

    private void StartNetworkTransportServer()
    {
        bool startResult = _server.StartServer(_serviceDescription);
        Assert.IsTrue(startResult); // Did the server start?

        if (startResult)
        {
            _serviceDescription = _server.ServiceDescription;
        }

        _server.ClientConnectionEstablished += OnClientConnected;
    }

    private void ConnectNetworkTransportClient()
    {
        _client.ConnectedToServer += OnConnectedToServer;

        bool connectionResult = _client.ConnectToServer("127.0.0.1", _serviceDescription.Port);
        Assert.IsTrue(connectionResult); // Did connection attempt start successfully?
    }
}
