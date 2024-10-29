using IMLD.MixedReality.Core;
using Pcx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public class PointCloudManager : MonoBehaviour
    {
        [SerializeField] Color _pointTint = new Color(0.5f, 0.5f, 0.5f, 1);
        [SerializeField] float _pointSize = 0.05f;
        [SerializeField] Shader _pointShader = null;
        [SerializeField] Shader _diskShader = null;
        [SerializeField] Transform _pointCloudOrigin;

        public IPointCloudSource PointCloudSource { get; set; }

        Material _pointMaterial;
        Material _diskMaterial;

        ComputeBuffer _xBuffer, _yBuffer, _zBuffer, _rgbBuffer;
        int _numPoints;

        private int _framecounter = 0;
        private long _time = 0;
        private System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

        private int[] _x;
        private int[] _y;
        private int[] _z;
        private uint[] _rgb;

        void OnValidate()
        {
            _pointSize = Mathf.Max(0, _pointSize);
        }

        void OnDestroy()
        {
            if (_pointMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_pointMaterial);
                }
                else
                {
                    DestroyImmediate(_pointMaterial);
                }
            }

            if (_diskMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_diskMaterial);
                }
                else
                {
                    DestroyImmediate(_diskMaterial);
                }
            }

            if (_rgbBuffer != null)
            {
                _rgbBuffer.Release();
            }

            if (_xBuffer != null)
            {
                _xBuffer.Release();
            }

            if (_yBuffer != null)
            {
                _yBuffer.Release();
            }

            if (_zBuffer != null)
            {
                _zBuffer.Release();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (PointCloudSource != null && PointCloudSource.PointCloud != null)
            {
                _numPoints = PointCloudSource.PointCloud.Count;
                CreateBuffers();
                FillPointCloudBuffers(PointCloudSource.PointCloud);
            }
        }

        public void FillPointCloudBuffers(PointCloudDataFrame pointCloud)
        {
            var Data = pointCloud.Data;
            int count = Data.Length / PointCloudDataFrame.POINT_LENGTH;
            var xShort = MemoryMarshal.Cast<byte, short>(new ReadOnlySpan<byte>(Data, 0, count * sizeof(short)));
            var yShort = MemoryMarshal.Cast<byte, short>(new ReadOnlySpan<byte>(Data, count * sizeof(short), count * sizeof(short)));
            var zShort = MemoryMarshal.Cast<byte, short>(new ReadOnlySpan<byte>(Data, 2 * count * sizeof(short), count * sizeof(short)));

            int offset = 3 * count * sizeof(short);
            int offset2 = 3 * count * sizeof(short) + count;
            int offset3 = 3 * count * sizeof(short) + (2 * count);

            for (int i = 0; i < count; i++)
            {
                _x[i] = xShort[i];
                _y[i] = yShort[i];
                _z[i] = zShort[i];
                _rgb[i] = EncodeColor(Data[offset + i], Data[offset2 + i], Data[offset3 + i]);
            }

            var array = _xBuffer.BeginWrite<int>(0, count);
            NativeArray<int>.Copy(_x, array, count);
            _xBuffer.EndWrite<int>(count);

            array = _yBuffer.BeginWrite<int>(0, count);
            NativeArray<int>.Copy(_y, array, count);
            _yBuffer.EndWrite<int>(count);

            array = _zBuffer.BeginWrite<int>(0, count);
            NativeArray<int>.Copy(_z, array, count);
            _zBuffer.EndWrite<int>(count);

            var rgbArray = _rgbBuffer.BeginWrite<uint>(0, count);
            NativeArray<uint>.Copy(_rgb, rgbArray, count);
            _rgbBuffer.EndWrite<uint>(count);
        }

        void OnRenderObject()
        {
            // Check buffers for null
            if (_xBuffer == null || _yBuffer == null || _zBuffer == null || _rgbBuffer == null) return;

            // Check the camera condition.
            var camera = Camera.current;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (camera.name == "Preview Scene Camera") return;

            // TODO: Do view frustum culling here.

            // Lazy initialization
            if (_pointMaterial == null)
            {
                _pointMaterial = new Material(_pointShader);
                _pointMaterial.hideFlags = HideFlags.DontSave;
                _pointMaterial.EnableKeyword("_COMPUTE_BUFFER");

                _diskMaterial = new Material(_diskShader);
                _diskMaterial.hideFlags = HideFlags.DontSave;
                _diskMaterial.EnableKeyword("_COMPUTE_BUFFER");
            }

            if (_pointSize == 0)
            {
                _pointMaterial.SetPass(0);
                _pointMaterial.SetColor("_Tint", _pointTint);

                if (_pointCloudOrigin == null)
                {
                    _pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                }
                else
                {
                    _pointMaterial.SetMatrix("_Transform", _pointCloudOrigin.localToWorldMatrix);
                }
                
                _pointMaterial.SetBuffer("_XBuffer", _xBuffer);
                _pointMaterial.SetBuffer("_YBuffer", _yBuffer);
                _pointMaterial.SetBuffer("_ZBuffer", _zBuffer);
                _pointMaterial.SetBuffer("_RGBBuffer", _rgbBuffer);

                Graphics.DrawProceduralNow(MeshTopology.Points, _numPoints, 1);

            }
            else
            {
                _diskMaterial.SetPass(0);
                _diskMaterial.SetColor("_Tint", _pointTint);

                if (_pointCloudOrigin == null)
                {
                    _diskMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                }
                else
                {
                    _diskMaterial.SetMatrix("_Transform", _pointCloudOrigin.localToWorldMatrix);
                }

                _diskMaterial.SetBuffer("_XBuffer", _xBuffer);
                _diskMaterial.SetBuffer("_YBuffer", _yBuffer);
                _diskMaterial.SetBuffer("_ZBuffer", _zBuffer);
                _diskMaterial.SetBuffer("_RGBBuffer", _rgbBuffer);
                _diskMaterial.SetFloat("_PointSize", _pointSize);

                Graphics.DrawProceduralNow(MeshTopology.Points, _numPoints, 1);

            }
        }

        private void CreateBuffers()
        {
            int numPoints = Math.Max((int)(_numPoints * 1.2f), 10000);

            if (_x == null || _x.Length < numPoints)
            {
                _x = new int[(int)(numPoints * 1.2)];
            }

            if (_y == null || _y.Length < numPoints)
            {
                _y = new int[(int)(numPoints * 1.2)];
            }

            if (_z == null || _z.Length < numPoints)
            {
                _z = new int[(int)(numPoints * 1.2)];
            }

            if (_rgb == null || _rgb.Length < numPoints)
            {
                _rgb = new uint[(int)(numPoints * 1.2)];
            }

            if (_xBuffer == null)
            {
                _xBuffer = new ComputeBuffer(numPoints, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }
            else if (_xBuffer.count < _numPoints)
            {
                _xBuffer.Release();
                _xBuffer = new ComputeBuffer(numPoints, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }

            if (_yBuffer == null)
            {
                _yBuffer = new ComputeBuffer(numPoints, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }
            else if (_yBuffer.count < _numPoints)
            {
                _yBuffer.Release();
                _yBuffer = new ComputeBuffer(numPoints, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }

            if (_zBuffer == null)
            {
                _zBuffer = new ComputeBuffer(numPoints, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }
            else if (_zBuffer.count < _numPoints)
            {
                _zBuffer.Release();
                _zBuffer = new ComputeBuffer(numPoints, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }

            if (_rgbBuffer == null)
            {
                _rgbBuffer = new ComputeBuffer(numPoints, sizeof(uint), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }
            else if (_rgbBuffer.count < _numPoints)
            {
                _rgbBuffer.Release();
                _rgbBuffer = new ComputeBuffer(numPoints, sizeof(uint), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            }
        }

        private static uint EncodeColor(byte r, byte g, byte b)
        {
            return ((uint)r) |
                   ((uint)g << 8) |
                   ((uint)b << 16);
        }
    }
}

