using IMLD.MixedReality.Core;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public class StickfigureAvatar : AbstractAvatar
    {
        IBodyDataSource SkeletonSource;

        private void Awake()
        {

        }

        // Start is called before the first frame update
        void Start()
        {
            // get skeleton provider
            SkeletonSource = ServiceLocator.Instance.Get<IKinectManager>().GetBodyDataSource(User.RoomId);
        }

        // Update is called once per frame
        void Update()
        {
            if (SkeletonSource != null)
            {
                var Skeleton = SkeletonSource.GetClosestBody(CameraCache.Main.transform);
                if (Skeleton != null)
                {
                    // ToDo: Use skeleton data
                    //Debug.Log("Got Skeleton Data!");
                    RenderSkeleton(Skeleton);
                }
            }
        }

        private void RenderSkeleton(Body skeleton)
        {
            for (int jointNum = 0; jointNum < (int)CustomJointId.Count; jointNum++)
            {
                Vector3 jointPos = skeleton.Joints[jointNum].Position;
                Quaternion jointRot = skeleton.Joints[jointNum].Rotation;

                // these are absolute body space because each joint has the body root for a parent in the scene graph
                transform.GetChild(0).GetChild(jointNum).localPosition = jointPos;
                transform.GetChild(0).GetChild(jointNum).localRotation = jointRot;

                //if (skeleton.Joints[jointNum].Confidence == Body.Confidence.High)
                //{
                //    transform.GetChild(0).GetChild(jointNum).gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
                //}
                //else if (skeleton.Joints[jointNum].Confidence == Body.Confidence.Medium)
                //{
                //    transform.GetChild(0).GetChild(jointNum).gameObject.GetComponent<MeshRenderer>().material.color = Color.yellow;
                //}
                //else if (skeleton.Joints[jointNum].Confidence == Body.Confidence.Low)
                //{
                //    transform.GetChild(0).GetChild(jointNum).gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
                //}
                //else
                //{
                //    transform.GetChild(0).GetChild(jointNum).gameObject.GetComponent<MeshRenderer>().material.color = Color.grey;
                //}
                

                int parentId = SkeletonSource.GetJointParent(jointNum);
                if (parentId != (int)CustomJointId.Head && parentId != (int)CustomJointId.Count)
                {
                    Vector3 parentTrackerSpacePosition = skeleton.Joints[parentId].Position;
                    Vector3 boneDirectionTrackerSpace = jointPos - parentTrackerSpacePosition;
                    Vector3 boneDirectionWorldSpace = transform.rotation * boneDirectionTrackerSpace;
                    Vector3 boneDirectionLocalSpace = Quaternion.Inverse(transform.GetChild(0).GetChild(jointNum).rotation) * Vector3.Normalize(boneDirectionWorldSpace);
                    transform.GetChild(0).GetChild(jointNum).GetChild(0).localScale = new Vector3(1, 20.0f * 0.5f * boneDirectionWorldSpace.magnitude, 1);
                    transform.GetChild(0).GetChild(jointNum).GetChild(0).localRotation = Quaternion.FromToRotation(Vector3.up, boneDirectionLocalSpace);
                    transform.GetChild(0).GetChild(jointNum).GetChild(0).position = transform.GetChild(0).GetChild(jointNum).position - 0.5f * boneDirectionWorldSpace;
                }
                else
                {
                    transform.GetChild(0).GetChild(jointNum).GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        public override void ApplyHandPosture(HandDataFrame leftHand, HandDataFrame rightHand)
        {
            // do nothing
        }
    }
}