using IMLD.MixedReality.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IMLD.MixedReality.Avatars
{
    public interface IKinectManager : IService
    {
        public Guid LocalKinectId { get; }
        public void ConnectToKinectService(NetworkServiceDescription service);
        public Transform GetLocalKinectTransform();
        public Transform GetKinectTransform(Guid kinectId);
        public Transform GetKinectTransform(int roomId);
        public IPointCloudSource GetPointCloudSource(int roomId);
        public IBodyDataSource GetBodyDataSource(int roomId);

        public IPointCloudSource GetPointCloudSource(Guid kinectId);
        public IBodyDataSource GetBodyDataSource(Guid kinectId);
    }
}