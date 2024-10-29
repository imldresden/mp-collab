using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessagePointCloudRequest : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.POINT_CLOUD_REQUEST;

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessagePointCloudRequest Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessagePointCloudRequest>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}