//using IMLD.MixedReality.Core;
//using IMLD.MixedReality.Network;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace IMLD.MixedReality.Avatars
//{
//    public class AvatarCalibration : MonoBehaviour
//    {
//        public int BufferSize { get; set; }

//        private ISessionManager _sessionManager;
//        private IBodyDataSource _bodyDataSource;
//        private IPointCloudSource _pointCloudSource;

//        private Dictionary<int, RingBuffer<Pose>> _bodyPoseDict = new Dictionary<int, RingBuffer<Pose>>();
//        private Dictionary<int, RingBuffer<Pose>> _pointCloudPoseDict = new Dictionary<int, RingBuffer<Pose>>();
//        private Dictionary<int, RingBuffer<Pose>> _hololensPoseDict = new Dictionary<int, RingBuffer<Pose>>();


//        public AvatarCalibration()
//        {
//            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();
//            _bodyDataSource = ServiceLocator.Instance.Get<IBodyDataSource>();
//            _pointCloudSource = ServiceLocator.Instance.Get<IPointCloudSource>();

//        }

//        private void Start()
//        {
            
//        }

//        void Update()
//        {
//            // get latest positions
//            foreach (var body in _bodyDataSource.Bodies)
//            {
//                // initialize new ring buffer if needed
//                if(!_bodyPoseDict.ContainsKey(body.Id))
//                {
//                    _bodyPoseDict.Add(body.Id, new RingBuffer<Pose>(BufferSize));
//                }

//                // store data
//                _bodyPoseDict[body.Id].Put(new Pose(body.Joints[(int)CustomJointId.Head].Position, body.Joints[(int)CustomJointId.Head].Rotation));
//            }

//            //foreach (var pc in _pointCloudSource)
//            //{
//            //    // initialize new ring buffer if needed
//            //    if (!_bodyPoseDict.ContainsKey(body.Id))
//            //    {
//            //        _bodyPoseDict.Add(body.Id, new RingBuffer<Pose>(BufferSize));
//            //    }

//            //    // store data
//            //    _bodyPoseDict[body.Id].Put(new Pose(body.Joints[(int)CustomJointId.Head].Position, body.Joints[(int)CustomJointId.Head].Rotation));
//            //}
//        }

//        private static HumanBodyBones MapKinectJoint(CustomJointId joint)
//        {
//            // https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints
//            switch (joint)
//            {
//                case CustomJointId.Pelvis: return HumanBodyBones.Hips;
//                case CustomJointId.SpineNavel: return HumanBodyBones.Spine;
//                case CustomJointId.SpineChest: return HumanBodyBones.Chest;
//                case CustomJointId.Neck: return HumanBodyBones.Neck;
//                case CustomJointId.Head: return HumanBodyBones.Head;
//                case CustomJointId.HipLeft: return HumanBodyBones.LeftUpperLeg;
//                case CustomJointId.KneeLeft: return HumanBodyBones.LeftLowerLeg;
//                case CustomJointId.AnkleLeft: return HumanBodyBones.LeftFoot;
//                case CustomJointId.FootLeft: return HumanBodyBones.LeftToes;
//                case CustomJointId.HipRight: return HumanBodyBones.RightUpperLeg;
//                case CustomJointId.KneeRight: return HumanBodyBones.RightLowerLeg;
//                case CustomJointId.AnkleRight: return HumanBodyBones.RightFoot;
//                case CustomJointId.FootRight: return HumanBodyBones.RightToes;
//                case CustomJointId.ClavicleLeft: return HumanBodyBones.LeftShoulder;
//                case CustomJointId.ShoulderLeft: return HumanBodyBones.LeftUpperArm;
//                case CustomJointId.ElbowLeft: return HumanBodyBones.LeftLowerArm;
//                case CustomJointId.WristLeft: return HumanBodyBones.LeftHand;
//                case CustomJointId.ClavicleRight: return HumanBodyBones.RightShoulder;
//                case CustomJointId.ShoulderRight: return HumanBodyBones.RightUpperArm;
//                case CustomJointId.ElbowRight: return HumanBodyBones.RightLowerArm;
//                case CustomJointId.WristRight: return HumanBodyBones.RightHand;
//                default: return HumanBodyBones.LastBone;
//            }
//        }
//    }
//}