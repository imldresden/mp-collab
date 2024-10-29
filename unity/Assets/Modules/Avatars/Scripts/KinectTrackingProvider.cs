using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using IMLD.MixedReality.Avatars;

public class KinectTrackingProvider : BackgroundDataProvider
{
    bool readFirstFrame = false;
    TimeSpan initialTimestamp;
    private List<Point>[] PointCloudData;
    private short[] PointCloudArray;
    private byte[] UserIndexMapArray;
    private byte[] ColorImageArray;
    private int ImageWidth;
    private int ImageHeight;
    private int NumBodies;
    //private List<Point>[][] ParallelPointData;
    //private PointCloud[] ParallelPointData;
    private short[][] _xListArray;
    private short[][] _yListArray;
    private short[][] _zListArray;
    private byte[][] _rListArray;
    private byte[][] _gListArray;
    private byte[][] _bListArray;
    private int[] _numPoints;
    private bool _filter = true;
    private bool _unfilteredRequested = false;

    private short _minX = -2000, _maxX = 2000, _minY = -2000, _maxY = 2000, _minZ = 1000, _maxZ = 2500;

    private long time = 0;
    private int framecounter = 0;

    public KinectTrackingProvider(int id) : base(id)
    {
        Debug.Log("in the skeleton provider constructor");
    }

    public void RequestUnfilteredData()
    {
        _unfilteredRequested = true;
    }

    protected override void RunBackgroundThreadAsync(int id, CancellationToken token)
    {
        UnityEngine.Debug.Log("Starting body tracker background thread...");
        while (!token.IsCancellationRequested)
        {
            try
            {
                UnityEngine.Debug.Log("Accessing Kinect device...");

                // Buffer allocations.
                KinectDataFrame currentFrameData = new KinectDataFrame();
                // Open device.
                using (Device device = Device.Open(id))
                {
                    device.StartCameras(new DeviceConfiguration()
                    {
                        CameraFPS = FPS.FPS30
                    ,ColorResolution = ColorResolution.R1536p
                    ,DepthMode = DepthMode.NFOV_Unbinned
                    ,WiredSyncMode = WiredSyncMode.Standalone
                    ,ColorFormat = ImageFormat.ColorBGRA32
                    });

                    UnityEngine.Debug.Log("Open K4A device successful. id " + id + "sn:" + device.SerialNum);

                    Calibration deviceCalibration = device.GetCalibration();
                    Transformation transform = deviceCalibration.CreateTransformation();

                    using (Tracker tracker = Tracker.Create(deviceCalibration, new TrackerConfiguration() { ProcessingMode = TrackerProcessingMode.Gpu, SensorOrientation = SensorOrientation.Default }))
                    {
                        UnityEngine.Debug.Log("Body tracker created.");
                        while (!token.IsCancellationRequested)
                        {
                            using (Capture sensorCapture = device.GetCapture())
                            {
                                // Queue latest frame from the sensor.
                                tracker.EnqueueCapture(sensorCapture);
                            }

                            // Try getting latest tracker frame.
                            using (Frame frame = tracker.PopResult(TimeSpan.Zero, throwOnTimeout: false))
                            {
                                if (frame == null)
                                {
                                    Debug.Log("Pop result from tracker timeout!");
                                }
                                else
                                {
                                    IsRunning = true;

                                    // Get number of bodies in the current frame.
                                    currentFrameData.NumOfBodies = (int)frame.NumberOfBodies;
                                    currentFrameData.Bodies = new NetworkedBody[currentFrameData.NumOfBodies];

                                    // Copy bodies.
                                    for (uint i = 0; i < currentFrameData.NumOfBodies; i++)
                                    {
                                        currentFrameData.Bodies[i] = new NetworkedBody(32);
                                        currentFrameData.Bodies[i].CopyFromBodyTrackingSdk(frame.GetBody(i), deviceCalibration);
                                    }

                                    // Store depth image.
                                    Capture bodyFrameCapture = frame.Capture;
                                    Image depthImage = bodyFrameCapture.Depth;
                                    if (!readFirstFrame)
                                    {
                                        readFirstFrame = true;
                                        initialTimestamp = depthImage.DeviceTimestamp;
                                    }

                                    currentFrameData.TimestampInMs = (int)(depthImage.DeviceTimestamp - initialTimestamp).TotalMilliseconds;

                                    // Get point cloud image
                                    Image pointCloudImage = transform.DepthImageToPointCloud(depthImage);
                                    PointCloudArray = MemoryMarshal.Cast<byte, short>(pointCloudImage.Memory.Span).ToArray();

                                    // Get Body Index Map
                                    Image bodyIndexMap = frame.BodyIndexMap;
                                    UserIndexMapArray = bodyIndexMap.Memory.ToArray();
                                    ImageWidth = bodyIndexMap.WidthPixels;
                                    NumBodies = currentFrameData.NumOfBodies;

                                    // Get Color data in depth frame transform
                                    Image colorInDepthImage = transform.ColorImageToDepthCamera(bodyFrameCapture);
                                    ColorImageArray = colorInDepthImage.Memory.ToArray();

                                    //// linear version
                                    //var watch = new System.Diagnostics.Stopwatch();
                                    //watch.Start();
                                    //List<short> xl = new List<short>();
                                    //List<short> yl = new List<short>();
                                    //List<short> zl = new List<short>();
                                    //List<byte> rl = new List<byte>();
                                    //List<byte> gl = new List<byte>();
                                    //List<byte> bl = new List<byte>();

                                    //for (int i = 0; i < bodyIndexMap.HeightPixels; i++)
                                    //{
                                    //    for (int j = 0; j < bodyIndexMap.WidthPixels; j++)
                                    //    {
                                    //        int n = i * bodyIndexMap.WidthPixels + j;

                                    //        short px = PointCloudArray[3 * n];
                                    //        short py = (short)(-1 * PointCloudArray[3 * n + 1]);
                                    //        short pz = PointCloudArray[3 * n + 2];

                                    //        byte pr = ColorImageArray[4 * n + 2];
                                    //        byte pg = ColorImageArray[4 * n + 1];
                                    //        byte pb = ColorImageArray[4 * n];

                                    //        if (UserIndexMapArray[n] < NumBodies && (pz > _minZ && pz < _maxZ && py > _minY && py < _maxY && px > _minX && px < _maxX))
                                    //        {
                                    //            xl.Add(px);
                                    //            yl.Add(py);
                                    //            zl.Add(pz);
                                    //            rl.Add(pr);
                                    //            gl.Add(pg);
                                    //            bl.Add(pb);
                                    //        }
                                    //    }
                                    //}

                                    //short[] x = xl.ToArray();
                                    //short[] y = yl.ToArray();
                                    //short[] z = zl.ToArray();
                                    //byte[] r = rl.ToArray();
                                    //byte[] g = gl.ToArray();
                                    //byte[] b = bl.ToArray();


                                    //new parallel version
                                    var watch = new System.Diagnostics.Stopwatch();
                                    watch.Start();

                                    if (_numPoints == null)
                                    {
                                        _xListArray = new short[bodyIndexMap.HeightPixels][];
                                        _yListArray = new short[bodyIndexMap.HeightPixels][];
                                        _zListArray = new short[bodyIndexMap.HeightPixels][];
                                        _rListArray = new byte[bodyIndexMap.HeightPixels][];
                                        _gListArray = new byte[bodyIndexMap.HeightPixels][];
                                        _bListArray = new byte[bodyIndexMap.HeightPixels][];
                                        _numPoints = new int[bodyIndexMap.HeightPixels];
                                    }

                                    if (_unfilteredRequested)
                                    {
                                        _filter = false; _unfilteredRequested = false;
                                    }

                                    Parallel.For(0, bodyIndexMap.HeightPixels, ComputePointCloud);

                                    int totalPointCount = 0;
                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        totalPointCount += _numPoints[j];
                                    }

                                    byte[] data = new byte[totalPointCount * 9];

                                    int index = 0;

                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        Buffer.BlockCopy(_xListArray[j], 0, data, index, _numPoints[j] * 2);
                                        index += _numPoints[j] * 2;
                                    }

                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        Buffer.BlockCopy(_yListArray[j], 0, data, index, _numPoints[j] * 2);
                                        index += _numPoints[j] * 2;
                                    }

                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        Buffer.BlockCopy(_zListArray[j], 0, data, index, _numPoints[j] * 2);
                                        index += _numPoints[j] * 2;
                                    }

                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        Buffer.BlockCopy(_rListArray[j], 0, data, index, _numPoints[j]);
                                        index += _numPoints[j];
                                    }

                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        Buffer.BlockCopy(_gListArray[j], 0, data, index, _numPoints[j]);
                                        index += _numPoints[j];
                                    }

                                    for (int j = 0; j < bodyIndexMap.HeightPixels; ++j)
                                    {
                                        Buffer.BlockCopy(_bListArray[j], 0, data, index, _numPoints[j]);
                                        index += _numPoints[j];
                                    }

                                    currentFrameData.PointCloud = data;

                                    watch.Stop();
                                    time += watch.ElapsedMilliseconds;
                                    framecounter++;
                                    if (framecounter == 30)
                                    {
                                        time /= 30;
                                        //Debug.Log("Time: " + time);
                                        time = 0;
                                        framecounter = 0;
                                    }
                                    SetCurrentFrameData(ref currentFrameData, _filter);
                                    _filter = true;
                                }

                            }
                        }
                        Debug.Log("dispose of tracker now!!!!!");
                        tracker.Dispose();
                    }
                    device.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.Log($"catching exception for background thread {e.Message}");
                token.ThrowIfCancellationRequested();
            }
            Thread.Sleep(2000);
        }
    }

    private void ComputePointCloud(int row, ParallelLoopState state)
    {
        if (_xListArray[row] == null)
        {
            _xListArray[row] = new short[ImageWidth];
            _yListArray[row] = new short[ImageWidth];
            _zListArray[row] = new short[ImageWidth];
            _rListArray[row] = new byte[ImageWidth];
            _gListArray[row] = new byte[ImageWidth];
            _bListArray[row] = new byte[ImageWidth];
        }

        int count = 0;

        for (int j = 0; j < ImageWidth; j++)
        {
            int n = row * ImageWidth + j;

            short x = PointCloudArray[3 * n];
            short y = (short)(-1 * PointCloudArray[3 * n + 1]);
            short z = PointCloudArray[3 * n + 2];

            byte r = ColorImageArray[4 * n + 2];
            byte g = ColorImageArray[4 * n + 1];
            byte b = ColorImageArray[4 * n];

            if (_filter == false || (UserIndexMapArray[n] < NumBodies && (z > _minZ && z < _maxZ && y > _minY && y < _maxY && x > _minX && x < _maxX)))
            {
                _xListArray[row][count] = x;
                _yListArray[row][count] = y;
                _zListArray[row][count] = z;

                _rListArray[row][count] = r;
                _gListArray[row][count] = g;
                _bListArray[row][count] = b;
                count++;
            }
        }
        _numPoints[row] = count;
    }
}