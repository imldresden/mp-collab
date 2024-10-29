using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace IMLD.MixedReality.Network
{
    public class MessageRoomUpdate : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.ROOM_UPDATE;

        public List<RoomDescription> Rooms;

        public MessageRoomUpdate(List<RoomDescription> rooms)
        {
            Rooms = rooms;
        }

        public MessageContainer Pack()
        {
            string Payload = JsonConvert.SerializeObject(this);
            return new MessageContainer(Type, Payload);
        }

        public static MessageRoomUpdate Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }
            var Result = JsonConvert.DeserializeObject<MessageRoomUpdate>(Encoding.UTF8.GetString(container.Payload));
            return Result;
        }
    }

    public struct RoomDescription
    {
        public int Id;
        public string Name;
        public int UserCount;

        public static RoomDescription Empty { get { return new RoomDescription() { Id = -1, Name = "", UserCount = 0 }; } }
    }
}