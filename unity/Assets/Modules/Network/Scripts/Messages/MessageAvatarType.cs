using IMLD.MixedReality.Avatars;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageAvatarType : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.AVATAR_TYPE;

        
        public int AvatarType;

        public MessageAvatarType(AvatarType type)
        {
            AvatarType = (int)type;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAvatarType Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAvatarType>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}