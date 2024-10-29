using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace IMLD.MixedReality.Core
{

    /// <summary>
    /// A pose provider that uses QR codes.
    /// This provider is dependent on a <see cref="QRCodeManager"/> being present in the scene and will not work without one.
    /// </summary>
    public class QRPoseProvider
    {
        /// <summary>
        /// The constructor creates a new instance that watches for the provided QR code string.
        /// </summary>
        /// <param name="qrId">the data string of the QR code</param>
        public QRPoseProvider(string qrId)
        {
            this.qrId = qrId;

            SetupQRCodeManager();
        }

        public float Velocity { get; private set; }

        private Guid Id
        {
            get => id;

            set
            {
                if (id != value)
                {
                    id = value;
                    InitializeSpatialGraphNode(force: true);
                }
            }
        }

        private Guid id;
        private QRCode qrCode;
        private string qrId;
        private SpatialGraphNode node;
        private QRCodeManager qrCodeManager;
        private DateTimeOffset lastTime;
        private Pose lastPose;

        public bool GetCurrentPose(out QRCodeInfo info)
        {
            info = new QRCodeInfo();

            if (SetupQRCodeManager() == false)
            {
                return false;
            }

            GetQRCode();
            if (qrCode == null)
            {
                return false;
            }

            info.Data = qrCode.Data;
            info.Size = qrCode.PhysicalSideLength;
            info.Time = qrCode.LastDetectedTime;

            if (node != null && node.TryLocate(FrameTime.OnUpdate, out Pose pose))
            {
                // rotate pose because QR codes use a different coordinate system.
                pose.rotation *= Quaternion.AngleAxis(90, Vector3.right);
                info.Pose = pose;

                var now = DateTimeOffset.Now;
                // If there is a parent to the camera that means we are using teleport and we should not apply the teleport
                // to these objects so apply the inverse
                if (CameraCache.Main.transform.parent != null)
                {
                    info.Pose = info.Pose.GetTransformedBy(CameraCache.Main.transform.parent);
                }

                float dS = Vector3.Distance(info.Pose.position, lastPose.position);
                float dT = (float)(now - lastTime).TotalSeconds;
                if (dT == 0)
                {
                    info.Velocity = 0f;
                    info.AngularVelocity = 0f;
                }
                else
                {
                    info.Velocity = dS / dT;
                    dS = Quaternion.Angle(info.Pose.rotation, lastPose.rotation);
                    info.AngularVelocity = dS / dT;
                }          

                lastTime = now;
                lastPose = info.Pose;

                return true;
            }

            return false;
        }

        private bool SetupQRCodeManager()
        {
            if (qrCodeManager == null)
            {
                qrCodeManager = QRCodeManager.FindDefaultQRCodeManager();
                if (qrCodeManager == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetQRCode()
        {
            qrCode = qrCodeManager?.GetQRCode(qrId);
            if (qrCode != null)
            {
                Id = qrCode.SpatialGraphNodeId;
                //Debug.Log("Length: " + qrCode.PhysicalSideLength + ", Time: " + qrCode.LastDetectedTime);
            }
        }

        private void InitializeSpatialGraphNode(bool force = false)
        {
            if (node == null || force)
            {
                node = Id != Guid.Empty ? SpatialGraphNode.FromStaticNodeId(Id) : null;
                //Debug.Log("Initialize SpatialGraphNode Id= " + Id);
            }
        }
    }

    public class QRCodeInfo
    {
        public string Data { get { return _data; } set { _data = value; _valid = false; } }
        public float Size { get { return _size; } set { _size = value; _valid = false; } }
        public Pose Pose { get { return _pose; } set { _pose = value; _valid = false; } }
        public float Velocity { get { return _velocity; } set { _velocity = value; _valid = false; } }
        public float AngularVelocity { get { return _angularVelocity; } set { _angularVelocity = value; _valid = false; } }
        public DateTimeOffset Time { get { return _time; } set { _time = value; _valid = false; } }

        private string _data;
        private bool _valid = false;
        private float _quality;
        private float _size;
        private Pose _pose;
        private float _velocity;
        private float _angularVelocity;
        private DateTimeOffset _time;

        public float Quality
        {
            get
            {
                if (_valid)
                {
                    return _quality;
                }
                else
                {
                    ComputeQuality();
                    return _quality;
                }
            }
        }

        private void ComputeQuality()
        {
            //TODO: compute new value for _quality

            _valid = true;
        }
    }
}