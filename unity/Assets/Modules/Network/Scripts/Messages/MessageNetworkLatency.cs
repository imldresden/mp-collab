using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageNetworkLatency : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.NETWORK_LATENCY;
        public float Latency;

        public MessageNetworkLatency(float latency)
        {
            Latency = latency;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageNetworkLatency Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageNetworkLatency>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}