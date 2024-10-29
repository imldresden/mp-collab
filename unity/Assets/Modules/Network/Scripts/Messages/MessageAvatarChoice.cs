using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageAvatarChoice : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.AVATAR_CHOICE;
        public int AvatarId;
        //public string UserIdString;
        public Guid UserId;
        //public Guid UserId { get { return Guid.Parse(UserIdString); } }

        public MessageAvatarChoice(int avatarId, Guid userId)
        {
            AvatarId = avatarId;
            //UserIdString = userId.ToString();
            UserId = userId;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAvatarChoice Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAvatarChoice>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}