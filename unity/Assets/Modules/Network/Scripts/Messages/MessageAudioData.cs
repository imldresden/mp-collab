using IMLD.MixedReality.Network;
using System;
using UnityEngine;

public class MessageAudioData : IMessage
{
    public static MessageContainer.MessageType Type = MessageContainer.MessageType.AUDIO_DATA;

    public Guid UserId;
    public int Channels;
    public byte[] ByteData;

    //public MessageAudioData(float[] data)
    //{
    //    Data = data;
    //}

    public MessageAudioData(Guid userId, int channels, byte[] data)
    {
        UserId = userId;
        Channels = channels;
        ByteData = data;
    }

    public MessageContainer Pack()
    {
        // payload size is the length of our audio frame + an int for the number of channels + the length of the user guid
        int PayloadSize = ByteData.Length + sizeof(int) + 16;

        // create message buffer, pre-filled with header
        MessageContainer.CreateBuffer(PayloadSize, Type, out byte[] Buffer, out int Offset);

        // write message payload
        System.Buffer.BlockCopy(UserId.ToByteArray(), 0, Buffer, Offset, 16);
        System.Buffer.BlockCopy(BitConverter.GetBytes(Channels), 0, Buffer, Offset + 16, sizeof(int));
        System.Buffer.BlockCopy(ByteData, 0, Buffer, Offset + 16 + sizeof(int), ByteData.Length);

        return new MessageContainer(Type, Buffer);
    }

    public static MessageAudioData Unpack(MessageContainer container)
    {
        if (container.Type != Type)
        {
            return null;
        }

        byte[] Data = new byte[container.Payload.Length - (16 + sizeof(int))];
        Buffer.BlockCopy(container.Payload, 16 + sizeof(int), Data, 0, Data.Length);
        int Channels = BitConverter.ToInt32(container.Payload, 16);
        //byte[] GuidBytes = new byte[16];
        //Buffer.BlockCopy(container.Payload, 0, GuidBytes, 0, GuidBytes.Length);
        //Guid Guid = new Guid(GuidBytes);
        Guid Guid = new Guid(container.Payload.AsSpan().Slice(0, 16));
        var Result = new MessageAudioData(Guid, Channels, Data);
        return Result;
    }
}
