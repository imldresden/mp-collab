using Newtonsoft.Json;
using System;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageConnectToServer : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.CONNECT_TO_SERVER;

        public Guid ClientId;

        public MessageConnectToServer(Guid clientId)
        {
            ClientId = clientId;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageConnectToServer Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageConnectToServer>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}