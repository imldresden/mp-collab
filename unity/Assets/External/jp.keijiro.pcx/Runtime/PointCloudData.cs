//// Pcx - Point cloud importer & renderer for Unity
//// https://github.com/keijiro/Pcx

//using UnityEngine;
//using System.Collections.Generic;
//using IMLD.MixedReality.Avatars;

//namespace Pcx
//{
//    /// A container class optimized for compute buffer.
//    public class PointCloudData : ScriptableObject
//    {
//        #region Public properties

//        /// Byte size of the point element.
//        public const int elementSize = sizeof(float) * 4;

//        /// Number of points.
//        public int pointCount {
//            get { return _pointData.Length; }
//        }

//        /// Get access to the compute buffer that contains the point cloud.
//        public ComputeBuffer computeBuffer {
//            get {
//                if (_pointBuffer == null)
//                {
//                    _pointBuffer = new ComputeBuffer(pointCount, elementSize);
//                    _pointBuffer.SetData(_pointData);
//                }
//                return _pointBuffer;
//            }
//        }

//        #endregion

//        #region ScriptableObject implementation

//        ComputeBuffer _pointBuffer;

//        void OnDisable()
//        {
//            if (_pointBuffer != null)
//            {
//                _pointBuffer.Release();
//                _pointBuffer = null;
//            }
//        }

//        #endregion

//        #region Serialized data members

//        [System.Serializable]
//        struct PcxPoint
//        {
//            public Vector3 position;
//            public uint color;
//        }

//        [SerializeField] PcxPoint[] _pointData;

//        #endregion



//        static uint EncodeColor(Color c)
//        {
//            const float kMaxBrightness = 16;

//            var y = Mathf.Max(Mathf.Max(c.r, c.g), c.b);
//            y = Mathf.Clamp(Mathf.Ceil(y * 255 / kMaxBrightness), 1, 255);

//            var rgb = new Vector3(c.r, c.g, c.b);
//            rgb *= 255 * 255 / (y * kMaxBrightness);

//            return ((uint)rgb.x      ) |
//                   ((uint)rgb.y <<  8) |
//                   ((uint)rgb.z << 16) |
//                   ((uint)y     << 24);
//        }

//        public void UpdateData(PointCloud pointCloud)
//        {

//            //_pointData = pointData.ToArray();
//            if(_pointData == null || _pointData.Length != pointData.Length)
//            {
//                _pointData = new PcxPoint[pointData.Length];
//            }

//            for (int i = 0; i < pointData.Length; i++)
//            {
//                _pointData[i] = new PcxPoint();
//                _pointData[i].position = new Vector3(pointData[i].x / 1000f, pointData[i].y / 1000f, pointData[i].z / 1000f);
//                _pointData[i].color = EncodeColor(new Color(pointData[i].r / 255f, pointData[i].g / 255f, pointData[i].b / 255f));
//            }

//            if (_pointBuffer == null)
//            {
//                _pointBuffer = new ComputeBuffer(pointData.Length, elementSize);
//            }
//            else if (_pointBuffer.count < pointData.Length)
//            {
//                _pointBuffer.Release();
//                _pointBuffer = new ComputeBuffer(pointData.Length, elementSize);
//            }
//            _pointBuffer.SetData( .SetData(_pointData);
//        }

//        public void Initialize(List<Vector3> positions, List<Color32> colors)
//        {
//            _pointData = new PcxPoint[positions.Count];
//            for (var i = 0; i < _pointData.Length; i++)
//            {
//                _pointData[i] = new PcxPoint
//                {
//                    position = positions[i],
//                    color = EncodeColor(colors[i])
//                };
//            }
//            if (_pointBuffer == null)
//            {
//                _pointBuffer = new ComputeBuffer(pointCount, elementSize);
//            }
//            _pointBuffer.SetData(_pointData);
//        }

//        private void OnDestroy()
//        {
//            if(_pointBuffer != null)
//            {
//                _pointBuffer.Release();
//                _pointBuffer = null;
//            }
//        }

//    }
//}
