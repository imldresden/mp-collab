using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageAvatarList : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.AVATAR_LIST;
        public Dictionary<Guid, int> Avatars;

        public MessageAvatarList(Dictionary<Guid, int> avatars)
        {
            Avatars = avatars;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAvatarList Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAvatarList>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}