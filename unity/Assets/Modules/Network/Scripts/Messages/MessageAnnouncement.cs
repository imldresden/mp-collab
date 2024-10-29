using Newtonsoft.Json;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageAnnouncement: IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ANNOUNCEMENT;
        public NetworkServiceDescription Service;

        public MessageAnnouncement(NetworkServiceDescription service)
        {
            Service = service;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAnnouncement Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAnnouncement>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
