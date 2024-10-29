using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageAudioPosition : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.AUDIO_POSITION;
        public int AudioId;

        public MessageAudioPosition(int audioId)
        {
            AudioId = audioId;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageAudioPosition Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageAudioPosition>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}