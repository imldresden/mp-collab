using IMLD.MixedReality.Core;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace IMLD.MixedReality.Network
{
    public class MessageUserListUpdate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.USER_LIST_UPDATE;

        public Dictionary<Guid, UserDescription> Users;

        public MessageUserListUpdate(Dictionary<Guid, UserDescription> users)
        {
            Users = users;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageUserListUpdate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageUserListUpdate>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}
