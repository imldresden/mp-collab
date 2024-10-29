using Newtonsoft.Json;
using System;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageDisconnectFromServer : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.DISCONNECT_FROM_SERVER;

        public Guid ClientId;

        public MessageDisconnectFromServer(Guid clientId)
        {
            ClientId = clientId;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageDisconnectFromServer Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageDisconnectFromServer>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}