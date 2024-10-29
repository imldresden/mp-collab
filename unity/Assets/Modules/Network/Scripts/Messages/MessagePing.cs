using IMLD.MixedReality.Network;
using System;

public class MessagePing : IMessage
{
    public static MessageContainer.MessageType Type = MessageContainer.MessageType.PING;

    public long TicksRequest;
    public long TicksResponse;

    public MessagePing()
    {
        TicksRequest = DateTime.UtcNow.Ticks;
        TicksResponse = 0;
    }

    public MessagePing(long ticksRequest, long ticksResponse)
    {
        TicksRequest = ticksRequest;
        TicksResponse = ticksResponse;
    }

    public MessagePing(MessagePing request)
    {
        TicksRequest = request.TicksRequest;
        TicksResponse = DateTime.UtcNow.Ticks;
    }

    public MessageContainer Pack()
    {
        // payload size is the length of the two ticks variables
        int PayloadSize = sizeof(long) * 2;

        // create message buffer, pre-filled with header
        MessageContainer.CreateBuffer(PayloadSize, Type, out byte[] Buffer, out int Offset);

        // write message payload
        System.Buffer.BlockCopy(BitConverter.GetBytes(TicksRequest), 0, Buffer, Offset, sizeof(long));
        System.Buffer.BlockCopy(BitConverter.GetBytes(TicksResponse), 0, Buffer, Offset + sizeof(long), sizeof(long));

        return new MessageContainer(Type, Buffer);
    }

    public static MessagePing Unpack(MessageContainer container)
    {
        if (container.Type != Type)
        {
            return null;
        }

        long ticksRequest = BitConverter.ToInt64(container.Payload, 0);
        long ticksResponse = BitConverter.ToInt64(container.Payload, sizeof(long));
        var Result = new MessagePing(ticksRequest, ticksResponse);
        return Result;
    }
}
