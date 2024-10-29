using Newtonsoft.Json;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageAcceptClient
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ACCEPT_CLIENT;

        public int ClientIndex;

        public MessageAcceptClient(int clientIndex)
        {
            ClientIndex = clientIndex;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAcceptClient Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAcceptClient>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}