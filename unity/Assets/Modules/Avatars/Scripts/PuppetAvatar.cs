using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Microsoft.MixedReality.Toolkit.Utilities;
using IMLD.MixedReality.Core;
using System;

namespace IMLD.MixedReality.Avatars
{
    public class PuppetAvatar : AbstractAvatar
    {
        //public TrackerHandler KinectDevice;
        //Dictionary<CustomJointId, Quaternion> absoluteOffsetMapKinect;
        Dictionary<HumanBodyBones, Quaternion> absoluteOffsetMap;
        Animator PuppetAnimator;
        HandDataFrame LeftHand, RightHand;
        public GameObject RootPosition;
        public Transform CharacterRootTransform;
        public float OffsetY;
        public float OffsetZ;

        [SerializeField] private CustomNetworkedHandVisualizer _handVisualizerLeft;
        [SerializeField] private CustomNetworkedHandVisualizer _handVisualizerRight;       

        private ILog _log;

        //// testing
        //public HandDataProvider HandDataProvider;

        IBodyDataSource SkeletonSource;

        private static HumanBodyBones MapCustomJointToUnityBone(CustomJointId joint)
        {
            // https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints
            switch (joint)
            {
                case CustomJointId.Pelvis: return HumanBodyBones.Hips;
                case CustomJointId.SpineNavel: return HumanBodyBones.Spine;
                case CustomJointId.SpineChest: return HumanBodyBones.Chest;
                case CustomJointId.Neck: return HumanBodyBones.Neck;
                case CustomJointId.Head: return HumanBodyBones.Head;
                case CustomJointId.HipLeft: return HumanBodyBones.LeftUpperLeg;
                case CustomJointId.KneeLeft: return HumanBodyBones.LeftLowerLeg;
                case CustomJointId.AnkleLeft: return HumanBodyBones.LeftFoot;
                case CustomJointId.FootLeft: return HumanBodyBones.LeftToes;
                case CustomJointId.HipRight: return HumanBodyBones.RightUpperLeg;
                case CustomJointId.KneeRight: return HumanBodyBones.RightLowerLeg;
                case CustomJointId.AnkleRight: return HumanBodyBones.RightFoot;
                case CustomJointId.FootRight: return HumanBodyBones.RightToes;
                case CustomJointId.ClavicleLeft: return HumanBodyBones.LeftShoulder;
                case CustomJointId.ShoulderLeft: return HumanBodyBones.LeftUpperArm;
                case CustomJointId.ElbowLeft: return HumanBodyBones.LeftLowerArm;
                case CustomJointId.WristLeft: return HumanBodyBones.LeftHand;
                case CustomJointId.ClavicleRight: return HumanBodyBones.RightShoulder;
                case CustomJointId.ShoulderRight: return HumanBodyBones.RightUpperArm;
                case CustomJointId.ElbowRight: return HumanBodyBones.RightLowerArm;
                case CustomJointId.WristRight: return HumanBodyBones.RightHand;
                default: return HumanBodyBones.LastBone;
            }
        }

        private static HumanBodyBones MapMrtkJointToUnityBone(TrackedHandJoint joint, Handedness handedness)
        {
            switch (handedness)
            {
                case Handedness.Left:
                {
                    switch (joint)
                    {
                        case TrackedHandJoint.ThumbDistalJoint: return HumanBodyBones.LeftThumbDistal;
                        case TrackedHandJoint.ThumbProximalJoint: return HumanBodyBones.LeftThumbProximal;
                        case TrackedHandJoint.IndexDistalJoint: return HumanBodyBones.LeftIndexDistal;
                        case TrackedHandJoint.IndexMiddleJoint: return HumanBodyBones.LeftIndexIntermediate;
                        case TrackedHandJoint.IndexKnuckle: return HumanBodyBones.LeftIndexProximal;
                        case TrackedHandJoint.MiddleDistalJoint: return HumanBodyBones.LeftMiddleDistal;
                        case TrackedHandJoint.MiddleMiddleJoint: return HumanBodyBones.LeftMiddleIntermediate;
                        case TrackedHandJoint.MiddleKnuckle: return HumanBodyBones.LeftMiddleProximal;
                        case TrackedHandJoint.RingDistalJoint: return HumanBodyBones.LeftRingDistal;
                        case TrackedHandJoint.RingMiddleJoint: return HumanBodyBones.LeftRingIntermediate;
                        case TrackedHandJoint.RingKnuckle: return HumanBodyBones.LeftRingProximal;
                        case TrackedHandJoint.PinkyDistalJoint: return HumanBodyBones.LeftLittleDistal;
                        case TrackedHandJoint.PinkyMiddleJoint: return HumanBodyBones.LeftLittleIntermediate;
                        case TrackedHandJoint.PinkyKnuckle: return HumanBodyBones.LeftLittleProximal;
                        default: return HumanBodyBones.LastBone;
                    }
                }
                case Handedness.Right:
                {
                    switch (joint)
                    {
                        case TrackedHandJoint.ThumbDistalJoint: return HumanBodyBones.RightThumbDistal;
                        case TrackedHandJoint.ThumbProximalJoint: return HumanBodyBones.RightThumbProximal;
                        case TrackedHandJoint.IndexDistalJoint: return HumanBodyBones.RightIndexDistal;
                        case TrackedHandJoint.IndexMiddleJoint: return HumanBodyBones.RightIndexIntermediate;
                        case TrackedHandJoint.IndexKnuckle: return HumanBodyBones.RightIndexProximal;
                        case TrackedHandJoint.MiddleDistalJoint: return HumanBodyBones.RightMiddleDistal;
                        case TrackedHandJoint.MiddleMiddleJoint: return HumanBodyBones.RightMiddleIntermediate;
                        case TrackedHandJoint.MiddleKnuckle: return HumanBodyBones.RightMiddleProximal;
                        case TrackedHandJoint.RingDistalJoint: return HumanBodyBones.RightRingDistal;
                        case TrackedHandJoint.RingMiddleJoint: return HumanBodyBones.RightRingIntermediate;
                        case TrackedHandJoint.RingKnuckle: return HumanBodyBones.RightRingProximal;
                        case TrackedHandJoint.PinkyDistalJoint: return HumanBodyBones.RightLittleDistal;
                        case TrackedHandJoint.PinkyMiddleJoint: return HumanBodyBones.RightLittleIntermediate;
                        case TrackedHandJoint.PinkyKnuckle: return HumanBodyBones.RightLittleProximal;
                        default: return HumanBodyBones.LastBone;
                    }
                }
                default: return HumanBodyBones.LastBone;
            }
        }

        public override void ApplyHandPosture(HandDataFrame leftHand, HandDataFrame rightHand)
        {
            LeftHand = leftHand;
            RightHand = rightHand;
        }

        private void Start()
        {
            PuppetAnimator = GetComponent<Animator>();
            Transform _rootJointTransform = CharacterRootTransform;

            // generate offsets for kinect data
            absoluteOffsetMap = new Dictionary<HumanBodyBones, Quaternion>();
            for (int i = 0; i < (int)CustomJointId.Count; i++)
            {
                HumanBodyBones hbb = MapCustomJointToUnityBone((CustomJointId)i);
                if (hbb != HumanBodyBones.LastBone)
                {
                    
                    Transform transform = PuppetAnimator.GetBoneTransform(hbb);
                    Quaternion absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation;                    

                    // find the absolute offset for the tpose
                    while (!ReferenceEquals(transform, _rootJointTransform))
                    {
                        transform = transform.parent;
                        absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation * absOffset;
                    }
                    absoluteOffsetMap[hbb] = absOffset;
                }
            }

            // get logger
            _log = ServiceLocator.Instance.Get<ILog>();

            //// generate offsets for hand tracking data of left hand
            //for (int i = 0; i < 27; i++)
            //{
            //    HumanBodyBones hbb = MapMrtkJointToUnityBone((TrackedHandJoint)i, Handedness.Left);
            //    if (hbb != HumanBodyBones.LastBone)
            //    {
            //        Transform transform = PuppetAnimator.GetBoneTransform(hbb);
            //        Quaternion absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation;
            //        // find the absolute offset for the tpose
            //        while (!ReferenceEquals(transform, _rootJointTransform))
            //        {
            //            transform = transform.parent;
            //            absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation * absOffset;
            //        }
            //        absoluteOffsetMap[hbb] = absOffset;
            //    }
            //}

            //// generate offsets for hand tracking data of right hand
            //for (int i = 0; i < 27; i++)
            //{
            //    HumanBodyBones hbb = MapMrtkJointToUnityBone((TrackedHandJoint)i, Handedness.Right);
            //    if (hbb != HumanBodyBones.LastBone)
            //    {
            //        Transform transform = PuppetAnimator.GetBoneTransform(hbb);
            //        Quaternion absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation;
            //        // find the absolute offset for the tpose
            //        while (!ReferenceEquals(transform, _rootJointTransform))
            //        {
            //            transform = transform.parent;
            //            absOffset = GetSkeletonBone(PuppetAnimator, transform.name).rotation * absOffset;
            //        }
            //        absoluteOffsetMap[hbb] = absOffset;
            //    }
            //}

            // get skeleton provider
            SkeletonSource = ServiceLocator.Instance.Get<IKinectManager>().GetBodyDataSource(User.RoomId);
        }

        private static SkeletonBone GetSkeletonBone(Animator animator, string boneName)
        {
            int count = 0;
            StringBuilder cloneName = new StringBuilder(boneName);
            cloneName.Append("(Clone)");
            foreach (SkeletonBone sb in animator.avatar.humanDescription.skeleton)
            {
                if (sb.name == boneName || sb.name == cloneName.ToString())
                {
                    return animator.avatar.humanDescription.skeleton[count];
                }
                count++;
            }
            return new SkeletonBone();
        }

        void OnAnimatorIK()
        {
            //PuppetAnimator.SetLookAtPosition(User.transform.forward);
            //PuppetAnimator.SetLookAtWeight(1f);
            //PuppetAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.5f);
            //PuppetAnimator.SetIKPosition(AvatarIKGoal.LeftHand, CharacterRootTransform.position + Skeleton.Joints[8].Position);
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            if (SkeletonSource != null)
            {
                var Skeleton = SkeletonSource.GetClosestBody(User.transform); // look up closest body for current head position
                if (Skeleton != null)
                {
                    for (int j = 0; j < (int)CustomJointId.Count; j++)
                    {
                        

                        var bone = MapCustomJointToUnityBone((CustomJointId)j);
                        if (bone != HumanBodyBones.LastBone && absoluteOffsetMap.ContainsKey(bone))
                        {
                            // get the absolute offset
                            Quaternion absOffset = absoluteOffsetMap[bone];
                            Transform finalJoint = PuppetAnimator.GetBoneTransform(bone);

                            //Debug.Log(Enum.GetName(typeof(CustomJointId), j));

                            // we update the head according to HL data
                            if (j == (int)CustomJointId.Head)
                            {
                                //finalJoint.up = User.transform.forward;
                                //finalJoint.right = User.transform.up * -1;
                                //finalJoint.forward = User.transform.right * -1;
                                //finalJoint.rotation = Quaternion.LookRotation(User.transform.forward);
                                finalJoint.rotation = User.transform.rotation * absOffset;
                                continue;
                            }

                            if (_handVisualizerLeft != null && _handVisualizerRight != null)
                            {
                                if ((j == (int)CustomJointId.WristLeft && !_handVisualizerLeft.GetComponent<CustomNetworkedHandVisualizer>().HandTracked) ||
                                (j == (int)CustomJointId.WristRight && !_handVisualizerRight.GetComponent<CustomNetworkedHandVisualizer>().HandTracked))
                                {
                                    continue;
                                }
                            }
                            

                           /* if (j == (int)CustomJointId.Neck)
                            {
                                //finalJoint.up = User.transform.forward;
                                //finalJoint.right = User.transform.up * -1;
                                //finalJoint.forward = User.transform.right * -1;
                                //finalJoint.rotation = Quaternion.LookRotation(User.transform.forward);
                                finalJoint.rotation = Quaternion.Slerp(User.transform.rotation, Skeleton.Joints[j].Rotation, 0.5f) * absOffset;
                                //finalJoint.rotation = Skeleton.Joints[j].Rotation * absOffset;
                                //finalJoint.rotation = Quaternion.Euler(new Vector3((finalJoint.rotation.eulerAngles.x + User.transform.rotation.eulerAngles.x) / 2, finalJoint.rotation.eulerAngles.y, finalJoint.rotation.eulerAngles.z));
                                continue;
                            }*/

                            finalJoint.rotation = /*absOffset * Quaternion.Inverse(absOffset) **/ Skeleton.Joints[j].Rotation * absOffset;
                            finalJoint.rotation = CharacterRootTransform.rotation * finalJoint.rotation;
                            if (j == 0)
                            {
                                // character root plus translation reading from the kinect, plus the offset from the script public variables
                                //finalJoint.position = CharacterRootTransform.position + new Vector3(RootPosition.transform.localPosition.x, RootPosition.transform.localPosition.y + OffsetY, RootPosition.transform.localPosition.z - OffsetZ);
                                finalJoint.position = CharacterRootTransform.position + (CharacterRootTransform.rotation * Skeleton.Joints[j].Position);
                                
                            }
                        }
                    }
                }
            }

            // Update hand data
            if (User != null)
            {
                if (_handVisualizerLeft != null)
                {
                    _handVisualizerLeft.ApplyHandPosture(User.LeftHand);
                }

                if (_handVisualizerRight != null)
                {
                    _handVisualizerRight.ApplyHandPosture(User.RightHand);
                }
            }

            LogUserData();
        }

        private void LogUserData()
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append(User.Id + _log.Delimiter + User.RoomId + _log.Delimiter);

            // log skeleton data
            for (int j = 0; j < (int)CustomJointId.Count; j++)
            {
                var bone = MapCustomJointToUnityBone((CustomJointId)j);
                if (bone != HumanBodyBones.LastBone)
                {
                    Transform finalJoint = PuppetAnimator.GetBoneTransform(bone);
                    if (finalJoint != null)
                    {
                        buffer.Append(LogUtils.ToString(finalJoint) + _log.Delimiter);
                    }
                }
            }

            // log hand data
            if (_handVisualizerLeft != null)
            {
                buffer.Append(_handVisualizerLeft.GetLoggingData(_log.Delimiter) + _log.Delimiter);
            }
            else
            {
                for (int i = 1; i < ArticulatedHandPose.JointCount - 1; i++)
                {
                    buffer.Append(LogUtils.ToString((Transform)null, _log.Delimiter) + _log.Delimiter);
                }
                buffer.Append(LogUtils.ToString((Transform)null, _log.Delimiter));
            }

            if (_handVisualizerRight != null)
            {
                buffer.Append(_handVisualizerRight.GetLoggingData(_log.Delimiter));
            }
            else
            {
                for (int i = 1; i < ArticulatedHandPose.JointCount - 1; i++)
                {
                    buffer.Append(LogUtils.ToString((Transform)null, _log.Delimiter) + _log.Delimiter);
                }
                buffer.Append(LogUtils.ToString((Transform)null, _log.Delimiter));
            }

            _log.Write(buffer.ToString(), "user_" + User.Id);
        }

    }
}