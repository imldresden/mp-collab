using IMLD.MixedReality.Avatars;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class MessageUpdateUser : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.UPDATE_USER;

        public UserDescription User;
        public HandDataFrame LeftHand;
        public HandDataFrame RightHand;

        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 GazeSource;
        public Vector3 GazeDirection;

        public Guid KinectId;
        public Vector3 KinectPosition;
        public Quaternion KinectOrientation;

        public float TrackingQuality;

        private const int LENGTH_SIZE = 4;    // 4 bytes to encode hand data size
        private const int LENGTH_USER_POSE = 28;        // 3 floats for position & 4 floats for rotation
        private const int LENGTH_USER_GAZE = 24;        // 3 floats for source position & 3 floats for gaze direction
        private const int LENGTH_KINECT_ID = 16;        // 16 byte long guid identifying a kinect sensor
        private const int LENGTH_KINECT_POSE = 28;      // 3 floats for position & 4 floats for rotation
        private const int LENGTH_QUALITY = 4;    // 4 bytes to encode tracking quality

        public MessageUpdateUser(UserDescription user, Vector3 position, Quaternion orientation, HandDataFrame leftHand, HandDataFrame rightHand, Guid kinectId, Vector3 kinectPosition, Quaternion kinectOrientation, float trackingQuality)
        {
            User = user;
            Position = position;
            Orientation = orientation;
            LeftHand = leftHand;
            RightHand = rightHand;
            KinectId = kinectId;
            KinectPosition = kinectPosition;
            KinectOrientation = kinectOrientation;
            TrackingQuality = trackingQuality;
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

            // convert HandDataFrames to byte arrays
            int sizeLeftHand = 0, sizeRightHand= 0;
            byte[] leftHandBytes = null;
            byte[] rightHandBytes = null;
            bool leftHandValid = false, rightHandValid = false;
            IntPtr Ptr;

            if ( LeftHand.JointPositions3D != null && LeftHand.JointRotations != null)
            {
                leftHandValid = true;
                sizeLeftHand = Marshal.SizeOf(LeftHand);
                RawDataSize += sizeLeftHand;
                leftHandBytes = new byte[sizeLeftHand];

                Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(sizeLeftHand);
                    Marshal.StructureToPtr(LeftHand, Ptr, true);
                    Marshal.Copy(Ptr, leftHandBytes, 0, sizeLeftHand);
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }

            if (RightHand.JointPositions3D != null && RightHand.JointRotations != null)
            {
                rightHandValid = true;
                sizeRightHand = Marshal.SizeOf(RightHand);
                RawDataSize += sizeRightHand;
                rightHandBytes = new byte[sizeRightHand];

                Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(sizeRightHand);
                    Marshal.StructureToPtr(RightHand, Ptr, true);
                    Marshal.Copy(Ptr, rightHandBytes, 0, sizeRightHand);
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }

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
                LENGTH_KINECT_ID + 
                LENGTH_KINECT_POSE +
                3 * LENGTH_SIZE +
                LENGTH_QUALITY +
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

            // copy hand data
            BitConverter.GetBytes(sizeLeftHand).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;

            if (leftHandValid)
            {
                leftHandBytes.CopyTo(Payload, PayloadIndex);
                PayloadIndex += leftHandBytes.Length;
            }

            BitConverter.GetBytes(sizeRightHand).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;

            if (rightHandValid)
            {
                rightHandBytes.CopyTo(Payload, PayloadIndex);
                PayloadIndex += rightHandBytes.Length;
            }

            // copy user description
            BitConverter.GetBytes(sizeUserDesc).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            userDescBytes.CopyTo(Payload, PayloadIndex);
            PayloadIndex += userDescBytes.Length;

            // copy kinect id
            KinectId.ToByteArray().CopyTo(Payload, PayloadIndex);
            PayloadIndex += LENGTH_KINECT_ID;

            // copy kinect pose
            BitConverter.GetBytes(KinectPosition.x).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(KinectPosition.y).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(KinectPosition.z).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(KinectOrientation.x).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(KinectOrientation.y).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(KinectOrientation.z).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;
            BitConverter.GetBytes(KinectOrientation.w).CopyTo(Payload, PayloadIndex);
            PayloadIndex += 4;

            // copy tracking quality
            BitConverter.GetBytes(TrackingQuality).CopyTo(Payload, PayloadIndex);
            PayloadIndex += LENGTH_QUALITY;

            // Compression
            //byte[] returnValue;

            //using (var compressor = new Compressor(new CompressionOptions(3)))
            //{
            //    returnValue = compressor.Wrap(Payload);
            //}

            //return returnValue;
            return Payload;
        }

        private static MessageUpdateUser DeserializeData(byte[] arrayInput)
        {
            Vector3 position = new Vector3();
            Quaternion rotation = new Quaternion();

            Vector3 gazePosition = new Vector3();
            Vector3 gazeDirection = new Vector3();

            HandDataFrame leftHand, rightHand;

            UserDescription user;

            Guid kinectId;
            Vector3 kinectPosition = new Vector3();
            Quaternion kinectRotation = new Quaternion();

            // zstandard decompression
            //======================
            //byte[] array;
            //using (var decompressor = new Decompressor())
            //{
            //    array = decompressor.Unwrap(arrayInput);
            //}
            //======================

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

            // read hand data
            int leftHandSize = BitConverter.ToInt32(array, ReadIndex);
            ReadIndex += 4;

            if (leftHandSize > 0)
            {
                IntPtr Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(leftHandSize);
                    Marshal.Copy(array, ReadIndex, Ptr, leftHandSize);
                    ReadIndex += leftHandSize;
                    leftHand = Marshal.PtrToStructure<HandDataFrame>(Ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }
            else
            {
                leftHand = new HandDataFrame();
            }

            int rightHandSize = BitConverter.ToInt32(array, ReadIndex);
            ReadIndex += 4;

            if (rightHandSize > 0)
            {
                IntPtr Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(rightHandSize);
                    Marshal.Copy(array, ReadIndex, Ptr, rightHandSize);
                    ReadIndex += rightHandSize;
                    rightHand = Marshal.PtrToStructure<HandDataFrame>(Ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }
            else
            {
                rightHand = new HandDataFrame();
            }

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

            // read kinect id
            byte[] kinectIdBytes = new byte[LENGTH_KINECT_ID];
            Array.Copy(array, ReadIndex, kinectIdBytes, 0, LENGTH_KINECT_ID);
            kinectId = new Guid(kinectIdBytes);
            ReadIndex += LENGTH_KINECT_ID;

            // read kinect pose
            kinectPosition.x = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            kinectPosition.y = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            kinectPosition.z = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            kinectRotation.x = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            kinectRotation.y = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            kinectRotation.z = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;
            kinectRotation.w = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += 4;

            // read tracking quality
            float trackingQuality = BitConverter.ToSingle(array, ReadIndex);
            ReadIndex += LENGTH_QUALITY;

            return new MessageUpdateUser(user, position, rotation, leftHand, rightHand, kinectId, kinectPosition, kinectRotation, trackingQuality);
        }

        public static MessageUpdateUser Unpack(MessageContainer container)
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