using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageJoinRoom : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ROOM_JOIN;

        public int RoomId;
        public string UserIdString;
        public Guid UserId { get { return Guid.Parse(UserIdString); } }

        public MessageJoinRoom(int roomId, Guid userId)
        {
            RoomId = roomId;
            UserIdString = userId.ToString();
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageJoinRoom Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageJoinRoom>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }
}