using K4os.Compression.LZ4;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using UnityEngine;
namespace IMLD.MixedReality.Network
{
    public class MessageContainer
    {
        public const int HEADER_SIZE = 
            sizeof(byte) +  // type
            sizeof(int) +   // payload length
            sizeof(long);   // timestamp

        public IPEndPoint Sender;

        public MessageType Type;
        public long Timestamp;
        public byte[] Payload;

        public float Offset = 0f;
        public float ConnectionLatency = 0f;
   
        private byte[] _rawData;

        /// <summary>
        /// Enum of all possible message types. Register new messages here!
        /// </summary>
        public enum MessageType
        {
            BYTE_ARRAY,
            ANNOUNCEMENT = 128,
            UPDATE_USER,
            ACCEPT_CLIENT,
            SKELETON_DATA,
            POINT_CLOUD_DATA,
            POINT_CLOUD_REQUEST,
            HAND_DATA,
            AUDIO_DATA,
            ROOM_UPDATE,
            ROOM_JOIN,
            ROOM_LEAVE,
            USER_LIST_UPDATE,
            AVATAR_TYPE,
            AVATAR_CHOICE,
            PING,
            CALIBRATION_POINT_CLOUD_DATA,
            AUDIO_POSITION,
            CONNECT_TO_SERVER,
            DISCONNECT_FROM_SERVER,
            AVATAR_LIST,
            NETWORK_LATENCY,
            OBJECT_UPDATE,
            OBJECT_LIST,
            UPDATE_USER_BASIC
        }

        public const byte FIRST_JSON_MESSAGE_TYPE = 128;

        public float GetAge()
        {
            return Mathf.Max(0f, (DateTime.UtcNow.Ticks - Timestamp) / (float)TimeSpan.TicksPerSecond - Offset);
        }

        public MessageContainer(MessageType type, string payload)
        {
            Type = type;
            Timestamp = DateTime.UtcNow.Ticks;
            byte[] PayloadBytes = Encoding.UTF8.GetBytes(payload);
            CreateBuffer(PayloadBytes.Length, Type, Timestamp, out _rawData, out int Offset);
            System.Buffer.BlockCopy(PayloadBytes, 0, _rawData, Offset, PayloadBytes.Length);
        }

        public MessageContainer(MessageType type, byte[] buffer)
        {
            Type = type;
            _rawData = buffer;
        }

        private MessageContainer(IPEndPoint sender, MessageType type, long timestamp, byte[] payload)
        {
            Type = type;
            Payload = payload;
            Sender = sender;
            Timestamp = timestamp;
        }

        public static MessageContainer Deserialize(IPEndPoint sender, byte[] payload, byte messageType, long timestamp)
        {
            var Message = new MessageContainer(sender, (MessageType)messageType, timestamp, payload);
            return Message;
        }

        public static MessageContainer Deserialize(IPEndPoint sender, byte[] data)
        {
            byte Type = data[sizeof(int)];
            long Timestamp = BitConverter.ToInt64(data, sizeof(int) + sizeof(byte));
            byte[] Payload = new byte[data.Length - HEADER_SIZE];
            Array.Copy(data, HEADER_SIZE, Payload, 0, data.Length - HEADER_SIZE);
            return Deserialize(sender, Payload, Type, Timestamp);
        }

        public static void CreateBuffer(int payloadSize, MessageType type, long timestamp, out byte[] buffer, out int offset)
        {
            // initialize new byte array with payload size and additional space for the message header
            buffer = new byte[payloadSize + HEADER_SIZE];
            
            // set offset to current header size
            offset = HEADER_SIZE;
            
            // copy header data into buffer
            Array.Copy(BitConverter.GetBytes(payloadSize), 0, buffer, 0, sizeof(int)); // payload length
            buffer[4] = (byte)type; // message type
            Array.Copy(BitConverter.GetBytes(timestamp), 0, buffer, sizeof(int) + sizeof(byte), sizeof(long)); // timestamp
        }

        public static void CreateBuffer(int payloadSize, MessageType type, out byte[] buffer, out int offset)
        {
            CreateBuffer(payloadSize, type, DateTime.UtcNow.Ticks, out buffer, out offset);
        }

        public byte[] Serialize()
        {
            //byte[] Envelope = new byte[Message.Length + 5];
            //Array.Copy(BitConverter.GetBytes(Message.Length), Envelope, 4);
            //Envelope[4] = (byte)Type;
            //Array.Copy(Message, 0, Envelope, 5, Message.Length);
            //return Envelope;

            return _rawData;
        }
    }
}