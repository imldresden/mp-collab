using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
namespace IMLD.MixedReality.Avatars
{
    public class KinectManager : MonoBehaviour, IKinectManager
    {
        [SerializeField] private Transform _localKinect;
        [SerializeField] private KinectRemoteDataSource _kinectPrefab;
        [SerializeField] private Transform _calibrationWidget;
        private KinectRemoteDataSource _localKinectDataSource;
        private INetworkServiceManager _networkServiceManager;
        private ISessionManager _sessionManager;
        private List<INetworkService> _kinectDataServices = new List<INetworkService>();
        private Dictionary<Guid, KinectRemoteDataSource> _kinectDataSources = new Dictionary<Guid, KinectRemoteDataSource>();
        private Dictionary<int, List<Guid>> _kinectsByRoomId = new Dictionary<int, List<Guid>>();

        public Guid LocalKinectId { get; private set; }

        IReadOnlyList<Type> IService.Dependencies { get; } = new List<Type> { typeof(ISessionManager), typeof(INetworkServiceManager) };

        private NetworkServiceDescription _localKinectServiceDescription;
        private INetworkService _localKinectService;

        public Transform GetLocalKinectTransform()
        {
            return _localKinect;
        }

        public IBodyDataSource GetBodyDataSource(int roomId)
        {
            return GetKinectRemoteDataSource(roomId);
        }

        public IPointCloudSource GetPointCloudSource(int roomId)
        {
            return GetKinectRemoteDataSource(roomId);
        }

        public IPointCloudSource GetPointCloudSource(Guid kinectId)
        {
            return GetKinectRemoteDataSource(kinectId);
        }

        public IBodyDataSource GetBodyDataSource(Guid kinectId)
        {
            return GetKinectRemoteDataSource(kinectId);
        }

        public Transform GetKinectTransform(Guid kinectId)
        {
            return _kinectDataSources[kinectId].transform;
        }

        public Transform GetKinectTransform(int roomId)
        {
            return GetKinectTransform(GetFirstKinectInRoom(roomId));
        }

        private Guid GetFirstKinectInRoom(int roomId)
        {
            _kinectsByRoomId.TryGetValue(roomId, out var guids);
            if (guids == null || guids.Count == 0)
            {
                return Guid.Empty;
            }

            return guids.First();
        }

        private KinectRemoteDataSource GetKinectRemoteDataSource(int roomId)
        {
            return GetKinectRemoteDataSource(GetFirstKinectInRoom(roomId));
        }

        private KinectRemoteDataSource GetKinectRemoteDataSource(Guid kinectId)
        {
            KinectRemoteDataSource value;
            _kinectDataSources.TryGetValue(kinectId, out value);
            return value;
        }

        // Start is called before the first frame update
        void Start()
        {
            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();
            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();

            if (_networkServiceManager != null)
            {
                _networkServiceManager.AvailableServicesChanged += OnAvailableServicesChanged;
                CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.SAME_ROOM_ID);
            }

            if (_sessionManager != null)
            {
                _sessionManager.RoomJoined += OnRoomJoined;
            }

            if (_localKinect != null)
            {
                _localKinectDataSource = _localKinect.GetComponent<KinectRemoteDataSource>();
            }
            
        }

        private void OnRoomJoined(object sender, RoomEventArgs e)
        {
            CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.SAME_ROOM_ID);
        }

        private void CheckAvailableServices(INetworkService.NetworkServiceFilter filter)
        {
            if (_networkServiceManager != null)
            {
                foreach (var service in _networkServiceManager.GetAvailableServices(filter))
                {
                    if (service.Type == NetworkServiceDescription.ServiceType.KINECT_DATA)
                    {
                        LocalKinectId = Guid.Parse(service.Data);
                        _localKinectServiceDescription = service;
                    }
                }
            }
        }

        public void RequestCalibration()
        {
            if (_localKinectServiceDescription != null)
            {
                // subscribe to Kinect data service
                if (_networkServiceManager.TryConnectToService(_localKinectServiceDescription, out INetworkService kinectDataService))
                {
                    kinectDataService.RegisterMessageHandler(MessageContainer.MessageType.CALIBRATION_POINT_CLOUD_DATA, OnCalibrationDataUpdate);
                    _localKinectService = kinectDataService;
                    _localKinectService.SendMessage(new MessagePointCloudRequest());
                    
                    //
                }
            }



        }

        public void StartManualCalibration(Transform calibrationWidget = null)
        {
            
            if (calibrationWidget != null)
            {
                _calibrationWidget = calibrationWidget;
            }
            if (_calibrationWidget != null)
            {
                RequestCalibration();
                _calibrationWidget.gameObject.SetActive(true);
                _calibrationWidget.position = CameraCache.Main.transform.position + CameraCache.Main.transform.forward * 0.5f;
            }
            
            //var objectManipulator = _localKinect.GetComponentInChildren<ObjectManipulator>(true);
            //if (objectManipulator != null)
            //{
            //    objectManipulator.enabled = true;
            //    objectManipulator.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            //}
        }

        public void StopManualCalibration()
        {
            _calibrationWidget.gameObject.SetActive(false);
            _localKinectDataSource.RenderPointClouds = false;

            //var objectManipulator = _localKinect.GetComponentInChildren<ObjectManipulator>(true);
            //if (objectManipulator != null)
            //{
            //    objectManipulator.enabled = false;
            //    objectManipulator.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            //}
        }

        private Task OnCalibrationDataUpdate(MessageContainer container)
        {
            Debug.Log("Receiving unfiltered Point Cloud data.");
            // update point cloud for local kinect with newly arrived calibration point cloud data
            if (_localKinectDataSource != null && container != null && container.Type == MessageContainer.MessageType.CALIBRATION_POINT_CLOUD_DATA)
            {
                var msg = MessageCalibrationPointCloud.Unpack(container);
                Debug.Log("Updating local Point Cloud.");
                _localKinectDataSource.RenderPointClouds = true;
                _localKinectDataSource.UpdatePointCloudData(msg.Data);
            }

            return Task.CompletedTask;
        }

        private void OnAvailableServicesChanged(object sender, EventArgs e)
        {
            CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.SAME_ROOM_ID);
        }

        // Update is called once per frame
        void Update()
        {
            //if (Input.GetKeyDown(KeyCode.C))
            //{
            //    Debug.Log("Started manual calibration!");
            //    StartManualCalibration();
            //}
        }

        public void ConnectToKinectService(NetworkServiceDescription service)
        {
            if (service.Type == NetworkServiceDescription.ServiceType.KINECT_DATA)
            {
                // subscribe to Kinect data service
                if (_networkServiceManager.TryConnectToService(service, out INetworkService kinectDataService, false))
                {
                    kinectDataService.RegisterMessageHandler(MessageContainer.MessageType.SKELETON_DATA, OnKinectDataUpdate);
                    kinectDataService.RegisterMessageHandler(MessageContainer.MessageType.POINT_CLOUD_DATA, OnKinectDataUpdate);
                    _kinectDataServices.Add(kinectDataService);
                }
            }
        }

        //private void OnAvailableServicesChanged(object sender, EventArgs e)
        //{
        //    CheckAvailableServices();
        //}

        //private void CheckAvailableServices()
        //{
        //    if (_networkServiceManager != null)
        //    {
        //        foreach (var service in _networkServiceManager.GetAvailableServices(filter: INetworkService.NetworkServiceFilter.ALL))
        //        {
        //            if (service.Type == NetworkServiceDescription.ServiceType.KINECT_DATA)
        //            {
        //                // subscribe to Kinect data service
        //                if (_networkServiceManager.TryConnectToService(service, out INetworkService kinectDataService))
        //                {
        //                    kinectDataService.RegisterMessageHandler(MessageContainer.MessageType.KINECT_DATA, OnKinectDataUpdate);
        //                    _kinectDataServices.Add(kinectDataService);
        //                }
        //            }
        //        }
        //    }
        //}

        private Task OnKinectDataUpdate(MessageContainer container)
        {
            if (container == null)
            {
                return Task.CompletedTask;
            }

            if (container.Type == MessageContainer.MessageType.SKELETON_DATA)
            {
                // unpack message
                var message = MessageSkeletonData.Unpack(container);

                if (message == null)
                {
                    return Task.CompletedTask;
                }

                if (_kinectDataSources.ContainsKey(message.Data.KinectId))
                {
                    _kinectDataSources[message.Data.KinectId].UpdateSkeletonData(message.Data);
                }
                else
                {
                    var component = Instantiate(_kinectPrefab, ServiceLocator.Instance.Get<IWorldAnchor>().GetOrigin());
                    component.name = "Kinect_" + message.Data.RoomId + "_" + message.Data.KinectId;
                    _kinectDataSources.Add(message.Data.KinectId, component);
                    _kinectDataSources[message.Data.KinectId].UpdateSkeletonData(message.Data);
                    if (_kinectsByRoomId.ContainsKey(message.Data.RoomId))
                    {
                        _kinectsByRoomId[message.Data.RoomId].Add(message.Data.KinectId);
                    }
                    else
                    {
                        _kinectsByRoomId.Add(message.Data.RoomId, new List<Guid> { message.Data.KinectId });
                    }
                }  
            }
            else if (container.Type == MessageContainer.MessageType.POINT_CLOUD_DATA)
            {
                // unpack message
                var message = MessagePointCloud.Unpack(container);

                if (message == null)
                {
                    return Task.CompletedTask;
                }

                if (_kinectDataSources.ContainsKey(message.Data.KinectId))
                {
                    _kinectDataSources[message.Data.KinectId].UpdatePointCloudData(message.Data);
                }
                else
                {
                    var component = Instantiate(_kinectPrefab, ServiceLocator.Instance.Get<IWorldAnchor>().GetOrigin());
                    component.name = "Kinect_" + message.Data.RoomId + "_" + message.Data.KinectId;
                    _kinectDataSources.Add(message.Data.KinectId, component);
                    _kinectDataSources[message.Data.KinectId].UpdatePointCloudData(message.Data);
                    if (_kinectsByRoomId.ContainsKey(message.Data.RoomId))
                    {
                        _kinectsByRoomId[message.Data.RoomId].Add(message.Data.KinectId);
                    }
                    else
                    {
                        _kinectsByRoomId.Add(message.Data.RoomId, new List<Guid> { message.Data.KinectId });
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}