using IMLD.MixedReality.Avatars;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageHandUpdate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.HAND_DATA;

        public Guid UserId;
        public int RoomId;
        public HandDataFrame LeftHand;
        public HandDataFrame RightHand;

        public MessageHandUpdate(Guid userId, int roomId, HandDataFrame leftHand, HandDataFrame rightHand)
        {
            UserId = userId;
            RoomId = roomId;
            LeftHand = leftHand;
            RightHand = rightHand;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageHandUpdate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageHandUpdate>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}