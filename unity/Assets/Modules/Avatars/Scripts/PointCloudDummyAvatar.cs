using IMLD.MixedReality.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public class PointCloudDummyAvatar : AbstractAvatar
    {
        IPointCloudSource _pointCloudSource;

        public override void ApplyHandPosture(HandDataFrame leftHand, HandDataFrame rightHand)
        {
            // do nothing
        }

        void Start()
        {
            // get point cloud provider
            _pointCloudSource = ServiceLocator.Instance.Get<IKinectManager>().GetPointCloudSource(User.RoomId);

            // enable point cloud rendering
            EnablePointClouds();
        }

        private void OnEnable()
        {
            EnablePointClouds();
        }

        private void OnDisable()
        {
            DisablePointClouds();
        }

        private void EnablePointClouds()
        {
            if (_pointCloudSource != null)
            {
                _pointCloudSource.RenderPointClouds = true;
            }
        }

        private void DisablePointClouds()
        {
            if (_pointCloudSource != null)
            {
                _pointCloudSource.RenderPointClouds = false;
            }
        }
    }
}