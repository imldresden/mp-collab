using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMLD.MixedReality.Network;
using System;
using System.Threading.Tasks;

public class MockNetworkService : MonoBehaviour, INetworkService
{
    public INetworkService.NetworkServiceRole Role => throw new NotImplementedException();

    public NetworkServiceDescription.ServiceType ServiceType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public NetworkServiceDescription ServiceDescription { get; set; }

    public INetworkFilter NetworkFilter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public float ServerLatency => throw new NotImplementedException();

    public float ServerTimeOffset => throw new NotImplementedException();

    public INetworkService.NetworkServiceStatus Status { get; set; }
    public float RequestedLatency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public event ClientEventHandler ClientConnected;
    public event ClientEventHandler ClientDisconnected;
    public event ClientEventHandler ClientReappeared;
    public event ClientEventHandler ClientDisappeared;
    public event EventHandler ConnectedToServer;
    public event EventHandler DisconnectedFromServer;

    public void Destroy()
    {
        throw new NotImplementedException();
    }

    public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Func<MessageContainer, Task> messageHandler)
    {
        throw new NotImplementedException();
    }

    public void SendMessage(IMessage message)
    {
        throw new NotImplementedException();
    }

    public void SendMessage(MessageContainer message)
    {
        throw new NotImplementedException();
    }

    public bool StartAsClient(NetworkServiceDescription serviceDescription, bool useMessageQueue = true)
    {
        throw new NotImplementedException();
    }

    public bool StartAsServer(bool useMessageQueue = true)
    {
        throw new NotImplementedException();
    }

    public bool UnregisterMessageHandler(MessageContainer.MessageType messageType)
    {
        throw new NotImplementedException();
    }

    public void UpdateRoom(RoomDescription room)
    {
        throw new NotImplementedException();
    }
}
