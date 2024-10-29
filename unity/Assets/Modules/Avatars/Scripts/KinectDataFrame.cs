using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public struct KinectDataFrame
    {
        // Id of the Kinect sensor
        public Guid KinectId;

        // Id of the room the kinect is located in
        public int RoomId;

        // Timestamp of current data
        public int TimestampInMs;

        // Number of detected bodies.
        public int NumOfBodies;

        // List of all bodies in current frame, each body is list of Body objects.
        public NetworkedBody[] Bodies;

        // Point Cloud data
        public byte[] PointCloud;
    }

    public class SkeletonDataFrame
    {
        // Id of the Kinect sensor
        public Guid KinectId;

        // Id of the room the kinect is located in
        public int RoomId;

        // Timestamp of current data
        public int TimestampInMs;

        // Number of detected bodies.
        public int NumOfBodies;

        // List of all bodies in current frame, each body is list of Body objects.
        public NetworkedBody[] Bodies;
    }

    public class PointCloudDataFrame
    {
        /// <summary>
        /// The number of points in the data frame
        /// </summary>
        public int Count
        {
            get
            {
                if (Data == null) return 0;
                return Data.Length / POINT_LENGTH;
            }
        }

        /// <summary>
        /// Length of a point in bytes
        /// </summary>
        public const int POINT_LENGTH = 9;

        // Id of the Kinect sensor
        public Guid KinectId;

        // Id of the room the kinect is located in
        public int RoomId;

        // Timestamp of current data
        public int TimestampInMs;

        public byte[] Data;
    }

    public struct Point
    {
        public short x;
        public short y;
        public short z;
        public byte r;
        public byte g;
        public byte b;

        public Point(short x, short y, short z, byte r, byte g, byte b)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.r = r;
            this.g = g;
            this.b = b;
        }

    }

    //public struct Point
    //{
    //    public byte x;
    //    public byte y;
    //    public byte z;
    //    public byte r;
    //    public byte g;
    //    public byte b;

    //    public Point(short x, short y, short z, byte r, byte g, byte b)
    //    {
    //        short twokay = 2047;
    //        short minustwokay = -2048;
    //        x = Math.Max(Math.Min(x, twokay), minustwokay);
    //        y = Math.Max(Math.Min(y, twokay), minustwokay);
    //        z = Math.Max(Math.Min(z, twokay), minustwokay);
    //        this.x = (byte)((x + 2048) / 16);
    //        this.y = (byte)((y + 2048) / 16);
    //        this.z = (byte)((z + 2048) / 16);
    //        this.r = r;
    //        this.g = g;
    //        this.b = b;
    //    }
    //}
}