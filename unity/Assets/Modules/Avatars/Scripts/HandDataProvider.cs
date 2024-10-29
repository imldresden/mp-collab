using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public class HandDataProvider : MonoBehaviour
    {
        private HandDataFrame _rightHandData;
        private HandDataFrame _leftHandData;
        [SerializeField] CustomNetworkedHandVisualizer _avatar;
        public HandDataFrame GetHandData(Handedness handedness)
        {
            if (handedness == Handedness.Left)
            {
                return _leftHandData;
            }

            if (handedness == Handedness.Right)
            {
                return _rightHandData;
            }

            else
            {
                throw new ArgumentException("Parameter handedness must be Left or Right.");
            }
        }

        // Update is called once per frame
        void Update()
        {
            // try to get all hand joint poses for both hands
            GetHandJointData();
            //_avatar.ApplyHandPosture(_leftHandData, _rightHandData);
        }

        private void GetHandJointData()
        {
            _leftHandData = new HandDataFrame();
            _rightHandData = new HandDataFrame();
            MixedRealityPose pose;

            foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            {
                if (HandJointUtils.TryGetJointPose(joint, Handedness.Right, out pose))
                {
                    AddJointToArray(pose, joint, ref _rightHandData);
                }
                if (HandJointUtils.TryGetJointPose(joint, Handedness.Left, out pose))
                {
                    AddJointToArray(pose, joint, ref _leftHandData);
                }
            }            
        }

        private void AddJointToArray(MixedRealityPose pose, TrackedHandJoint id, ref HandDataFrame handData)
        {
            int jointId = (int)id;
            System.Numerics.Vector3 position = new System.Numerics.Vector3(pose.Position.x, pose.Position.y, pose.Position.z);
            System.Numerics.Quaternion rotation = new System.Numerics.Quaternion(pose.Rotation.x, pose.Rotation.y, pose.Rotation.z, pose.Rotation.w);
            
            if (handData.JointPositions3D == null)
            {
                handData.JointPositions3D = new System.Numerics.Vector3[27];
            }

            if (handData.JointRotations == null)
            {
                handData.JointRotations = new System.Numerics.Quaternion[27];
            }

            handData.JointPositions3D[jointId] = position;
            handData.JointRotations[jointId] = rotation;
        }
    }

    public struct HandDataFrame
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 27)]
        public System.Numerics.Vector3[] JointPositions3D;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 27)]
        public System.Numerics.Quaternion[] JointRotations;

        public HandDataFrame ConvertToLocal(Transform parent)
        {
            if (JointPositions3D == null || JointRotations == null)
            {
                return this;
            }

            HandDataFrame value = new HandDataFrame();
            value.JointPositions3D = new System.Numerics.Vector3[27];
            value.JointRotations = new System.Numerics.Quaternion[27];

            for (int i = 0; i < JointPositions3D.Length; i++)
            {
                if (JointPositions3D[i] != null && JointRotations[i] != null)
                {
                    Pose pose = new Pose(Conversion.FromNumericsVector3(JointPositions3D[i]), Conversion.FromNumericsQuaternion(JointRotations[i]));
                    pose = pose.GetRelativePose(parent);
                    value.JointPositions3D[i] = Conversion.FromUnityVector3(pose.position);
                    value.JointRotations[i] = Conversion.FromUnityQuaternion(pose.rotation);
                }
            }

            return value;
        }

        public HandDataFrame ConvertToGlobal(Transform parent)
        {
            if (JointPositions3D == null || JointRotations == null)
            {
                return this;
            }

            HandDataFrame value = new HandDataFrame();
            value.JointPositions3D = new System.Numerics.Vector3[27];
            value.JointRotations = new System.Numerics.Quaternion[27];

            for (int i = 0; i < JointPositions3D.Length; i++)
            {
                if (JointPositions3D[i] != null && JointRotations[i] != null)
                {
                    Pose pose = new Pose(Conversion.FromNumericsVector3(JointPositions3D[i]), Conversion.FromNumericsQuaternion(JointRotations[i]));
                    pose = pose.GetAbsolutePose(parent);
                    value.JointPositions3D[i] = Conversion.FromUnityVector3(pose.position);
                    value.JointRotations[i] = Conversion.FromUnityQuaternion(pose.rotation);
                }
            }

            return value;
        }
    }
}