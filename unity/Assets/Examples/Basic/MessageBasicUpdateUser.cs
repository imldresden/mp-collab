using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageBasicUpdateUser : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_USER_BASIC;

        public UserDescription User;

        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 GazeSource;
        public Vector3 GazeDirection;

        private const int LENGTH_USER_POSE = 28;        // 3 floats for position & 4 floats for rotation
        private const int LENGTH_USER_GAZE = 24;        // 3 floats for source position & 3 floats for gaze direction

        public MessageBasicUpdateUser(UserDescription user, Vector3 position, Quaternion orientation)
        {
            User = user;
            Position = position;
            Orientation = orientation;
        }

        public MessageContainer Pack()
        {
            byte[] Payload = SerializeData();

            // create message buffer, pre-filled with header
            MessageContainer.CreateBuffer(Payload.Length, Type, out byte[] Buffer, out int Offset);

            // copy actual data to buffer
            System.Buffer.BlockCopy(Payload, 0, Buffer, Offset, Payload.Length);
            return new MessageContainer(Type, Buffer);
        }

        private byte[] SerializeData()
        {
            int RawDataSize = 0;
            IntPtr Ptr;

            // convert user description to byte array
            int sizeUserDesc = Marshal.SizeOf(User);
            RawDataSize += sizeUserDesc;
            byte[] userDescBytes = new byte[sizeUserDesc];

            Ptr = IntPtr.Zero;
            try
            {
                Ptr = Marshal.AllocHGlobal(sizeUserDesc);
                Marshal.StructureToPtr(User, Ptr, true);
                Marshal.Copy(Ptr, userDescBytes, 0, sizeUserDesc);
            }
            finally
            {
                Marshal.FreeHGlobal(Ptr);
            }

            // initialize payload byte array
            byte[] Payload = new byte[
                LENGTH_USER_POSE +
                LENGTH_USER_GAZE +
                RawDataSize
                ];

            int PayloadIndex = 0;

            // copy user pose
            BitConverter.GetBytes(Position.x).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(Position.y).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(Position.z).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(Orientation.x).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(Orientation.y).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(Orientation.z).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(Orientation.w).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;

            // copy user gaze
            BitConverter.GetBytes(GazeSource.x).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(GazeSource.y).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(GazeSource.z).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(GazeDirection.x).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(GazeDirection.y).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(GazeDirection.z).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;

            // copy user description
            BitConverter.GetBytes(sizeUserDesc).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            userDescBytes.CopyTo(Payload, PayloadIndex);
            PayloadIndex += userDescBytes.Length;

            //return returnValue;
            return Payload;
        }

        private static MessageBasicUpdateUser DeserializeData(byte[] arrayInput)
        {
            Vector3 position = new Vector3();
            Quaternion rotation = new Quaternion();

            Vector3 gazePosition = new Vector3();
            Vector3 gazeDirection = new Vector3();

            UserDescription user;

            byte[] array = arrayInput;

            int ReadIndex = 0;

            // read user pose
            position.x = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            position.y = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            position.z = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            rotation.x = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            rotation.y = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            rotation.z = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            rotation.w = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;

            // read user gaze
            gazePosition.x = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            gazePosition.y = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            gazePosition.z = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            gazeDirection.x = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            gazeDirection.y = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            gazeDirection.z = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;

            // read user description
            int userDescSize = BitConverter.ToInt32(array, ReadIndex);
            ReadIndex += 4;

            if (userDescSize > 0)
            {
                IntPtr Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(userDescSize);
                    Marshal.Copy(array, ReadIndex, Ptr, userDescSize);
                    ReadIndex += userDescSize;
                    user = Marshal.PtrToStructure<UserDescription>(Ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }
            else
            {
                user = new UserDescription();
            }

            return new MessageBasicUpdateUser(user, position, rotation);
        }

        public static MessageBasicUpdateUser Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            try
            {
                return DeserializeData(container.Payload);
            }
            catch (Exception e)
            {
                Debug.LogError("Error unpacking message: " + e.Message);
                return null;
            }
        }

    }
}