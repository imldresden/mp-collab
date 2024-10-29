using Newtonsoft.Json;
using System;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageLeaveRoom : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ROOM_LEAVE;

        public string UserIdString;
        public Guid UserId { get { return Guid.Parse(UserIdString); } }

        public MessageLeaveRoom(Guid userId)
        {
            UserIdString = userId.ToString();
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageLeaveRoom Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageLeaveRoom>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}