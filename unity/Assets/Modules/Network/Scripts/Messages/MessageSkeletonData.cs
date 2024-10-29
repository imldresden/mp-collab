using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using IMLD.MixedReality.Avatars;
using ZstdSharp;

using System.Buffers;
using System.Buffers.Binary;

namespace IMLD.MixedReality.Network
{
    public class MessageSkeletonData : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.SKELETON_DATA;
        private static long deflateTimeSum, zstandardTimeSum, lz4TimeSum;
        private static float deflateRatioSum, zstandardRatioSum, lz4RatioSum;
        private static int n;

        public SkeletonDataFrame Data;

        public MessageSkeletonData(SkeletonDataFrame data)
        {
            Data = data;
        }

        public MessageContainer Pack()
        {
            var Payload = SerializeData();

            // create message buffer, pre-filled with header
            MessageContainer.CreateBuffer(Payload.Length, Type, out byte[] Buffer, out int Offset);

            // copy actual data to buffer
            var bufferSpan = new Span<byte>(Buffer, Offset, Payload.Length);
            Payload.CopyTo(bufferSpan);
            //System.Buffer.BlockCopy(Payload.ToArray(), 0, Buffer, Offset, Payload.Length);
            return new MessageContainer(Type, Buffer);
        }

        private const int LENGTH_TIMESTAMP = 4;
        private const int LENGTH_NUM_BODIES = 4;
        private const int LENGTH_SIZE_BODY = 4;
        private const int LENGTH_KINECT_ID = 16;
        private const int LENGTH_ROOM_ID = 4;

        private Span<byte> SerializeData()
        {
            int RawDataSize = 0;

            // convert bodies to byte arrays
            List<byte[]> Bodies = new List<byte[]>(Data.Bodies.Length);

            for (int i = 0; i < Data.Bodies.Length; i++)
            {
                int size = Marshal.SizeOf(Data.Bodies[i]);
                Bodies.Add(new byte[size]);
                RawDataSize += size;

                IntPtr Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(Data.Bodies[i], Ptr, true);
                    Marshal.Copy(Ptr, Bodies[i], 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }

            // compute payload length
            int PayloadTotalLength =
                LENGTH_KINECT_ID +                              // the Kinect id
                LENGTH_ROOM_ID +                                // the room id
                LENGTH_TIMESTAMP +                              // the timestamp
                LENGTH_NUM_BODIES +                             // the number of bodies
                LENGTH_SIZE_BODY * Bodies.Count +               // the individual lengths of the bodies
                RawDataSize;                                     // the body and point cloud data itself

            // initialize payload byte array
            //byte[] Payload = ArrayPool<byte>.Shared.Rent(PayloadTotalLength);
            byte[] Payload = new byte[PayloadTotalLength];

            // get span over the payload array
            Span<byte> PayloadSpan = new Span<byte>(Payload);

            int PayloadIndex = 0;

            // copy kinect id
            Data.KinectId.TryWriteBytes(PayloadSpan.Slice(PayloadIndex));
            PayloadIndex += LENGTH_KINECT_ID;

            // copy room id
            BinaryPrimitives.WriteInt32LittleEndian(PayloadSpan.Slice(PayloadIndex), Data.RoomId);
            PayloadIndex += LENGTH_ROOM_ID;

            // copy time stamp
            BinaryPrimitives.WriteInt32LittleEndian(PayloadSpan.Slice(PayloadIndex), Data.TimestampInMs);
            PayloadIndex += LENGTH_TIMESTAMP;

            // copy num bodies
            BinaryPrimitives.WriteInt32LittleEndian(PayloadSpan.Slice(PayloadIndex), Bodies.Count);
            PayloadIndex += LENGTH_NUM_BODIES;

            // copy bodies
            foreach (var body in Bodies)
            {
                // write length
                BinaryPrimitives.WriteInt32LittleEndian(PayloadSpan.Slice(PayloadIndex), body.Length);
                PayloadIndex += LENGTH_SIZE_BODY;

                // write data
                body.CopyTo(PayloadSpan.Slice(PayloadIndex));
                PayloadIndex += body.Length;
            }

            // compress data, only taking the used part of the payload array which could be longer because it was rented from an ArrayPool
            Compressor Compressor = new Compressor(3);
            var compressedData = Compressor.Wrap(PayloadSpan.Slice(0, PayloadTotalLength));

            // return temp array to pool
            //ArrayPool<byte>.Shared.Return(Payload);

            return compressedData;
        }

        private static SkeletonDataFrame DeserializeData(byte[] arrayInput)
        {
            // decompress data
            Span<byte> arraySpan;
            byte[] array = null;
            try
            {
                Decompressor Decompressor = new Decompressor();
                //Decompressor.Unwrap(arrayInput, array, 0);
                arraySpan = Decompressor.Unwrap(arrayInput);
                array = arraySpan.ToArray();
            }
            catch(Exception e)
            {
                Debug.LogError("Error decompressing message data: " + e.Message + "\nArray length: " + arrayInput.Length);
                return default;
            }

            SkeletonDataFrame Data = new SkeletonDataFrame();

            int ReadIndex = 0;

            // read kinect id
            Data.KinectId = new Guid(arraySpan.Slice(ReadIndex, LENGTH_KINECT_ID));
            ReadIndex += LENGTH_KINECT_ID;

            // read room id
            Data.RoomId = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
            ReadIndex += LENGTH_ROOM_ID;

            // read time stamp
            Data.TimestampInMs = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
            ReadIndex += LENGTH_TIMESTAMP;

            // read num bodies
            Data.NumOfBodies = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
            ReadIndex += LENGTH_NUM_BODIES;

            // read bodies
            Data.Bodies = new NetworkedBody[Data.NumOfBodies];
            for (int i = 0; i < Data.NumOfBodies; i++)
            {
                // read size of body struct
                int Size = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
                ReadIndex += LENGTH_SIZE_BODY;

                // read body struct
                IntPtr Ptr = IntPtr.Zero;
                try
                {
                    Ptr = Marshal.AllocHGlobal(Size);
                    Marshal.Copy(array, ReadIndex, Ptr, Size);
                    ReadIndex += Size;
                    NetworkedBody Body = Marshal.PtrToStructure<NetworkedBody>(Ptr);
                    Data.Bodies[i] = Body;
                }
                finally
                {
                    Marshal.FreeHGlobal(Ptr);
                }
            }

            return Data;
        }

        public static MessageSkeletonData Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var Result = new MessageSkeletonData(DeserializeData(container.Payload));
            return Result;
        }
    }
}