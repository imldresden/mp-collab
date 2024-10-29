using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using IMLD.MixedReality.Avatars;
//using ZstdNet;
using ZstdSharp;

using System.Buffers;
using System.Buffers.Binary;

namespace IMLD.MixedReality.Network
{
    public class MessagePointCloud : IMessage
    {
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.POINT_CLOUD_DATA;

        public PointCloudDataFrame Data;

        public MessagePointCloud(PointCloudDataFrame data)
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
        private const int LENGTH_SIZE_POINTCLOUD = 4;
        private const int LENGTH_POINT = 9;
        private const int LENGTH_ID_POINTCLOUD = 4;
        private const int LENGTH_KINECT_ID = 16;
        private const int LENGTH_ROOM_ID = 4;

        private Span<byte> SerializeData()
        {
            // compute payload length
            int PayloadTotalLength =
                LENGTH_KINECT_ID +                              // the Kinect id
                LENGTH_ROOM_ID +                                // the room id
                LENGTH_TIMESTAMP +                              // the timestamp
                LENGTH_SIZE_POINTCLOUD +                        // the length of the point cloud
                Data.Data.Length;                         // the point cloud data itself

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

            // write size of point cloud data
            BinaryPrimitives.WriteInt32LittleEndian(PayloadSpan.Slice(PayloadIndex), Data.Data.Length);
            PayloadIndex += LENGTH_SIZE_POINTCLOUD;

            // write point cloud
            Data.Data.CopyTo(PayloadSpan.Slice(PayloadIndex));
            //PayloadIndex += Data.PointCloud.Count * LENGTH_POINT;

            // compress data, only taking the used part of the payload array which could be longer because it was rented from an ArrayPool
            Compressor Compressor = new Compressor(3);
            var compressedData = Compressor.Wrap(PayloadSpan.Slice(0, PayloadTotalLength));

            // return temp array to pool
            //ArrayPool<byte>.Shared.Return(Payload);

            return compressedData;
        }

        private static PointCloudDataFrame DeserializeData(byte[] arrayInput)
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

            PointCloudDataFrame PointCloud = new PointCloudDataFrame();

            int ReadIndex = 0;

            // read kinect id
            PointCloud.KinectId = new Guid(arraySpan.Slice(ReadIndex, LENGTH_KINECT_ID));
            ReadIndex += LENGTH_KINECT_ID;

            // read room id
            PointCloud.RoomId = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
            ReadIndex += LENGTH_ROOM_ID;

            // read time stamp
            PointCloud.TimestampInMs = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
            ReadIndex += LENGTH_TIMESTAMP;

            // read size of point cloud
            int pointCloudDataLength = BinaryPrimitives.ReadInt32LittleEndian(arraySpan.Slice(ReadIndex));
            ReadIndex += LENGTH_SIZE_POINTCLOUD;

            // read point cloud
            byte[] pointCloudData = new byte[pointCloudDataLength];
            Buffer.BlockCopy(array, ReadIndex, pointCloudData, 0, pointCloudDataLength);
            //ReadIndex += numPoints * LENGTH_POINT;
            PointCloud.Data = pointCloudData;

            return PointCloud;
        }

        public static MessagePointCloud Unpack(MessageContainer container)
        {
            if (container.Type != Type)
            {
                return null;
            }

            var Result = new MessagePointCloud(DeserializeData(container.Payload));
            return Result;
        }
    }
}