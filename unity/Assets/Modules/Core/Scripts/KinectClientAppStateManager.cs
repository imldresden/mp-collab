
using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Network;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public class KinectClientAppStateManager : ClientAppStateManager
    {
        //[SerializeField]
        //private User _userPrefab;

        //[SerializeField]
        //private Transform _localUser;

        //[SerializeField]
        //private HandDataProvider _handDataProvider;

        //private INetworkService _currentSession;
        //private INetworkServiceManager _networkServiceManager;
        //private IWorldAnchor _worldAnchor;
        //private Dictionary<Guid, User> _userGODictionary = new Dictionary<Guid, User>();

        public new IReadOnlyList<Type> Dependencies { get; } = new List<Type> { typeof(INetworkServiceManager) };


        public override void LeaveSession()
        {
            _networkServiceManager.DisconnectFromService(_networkService);

            _networkService.UnregisterMessageHandler(MessageContainer.MessageType.ROOM_UPDATE);

            _networkService = null;
            Session = null;
            OnSessionLeft();
        }

        public override void JoinRoom(RoomDescription room)
        {
            Room = room;
            OnRoomJoined(new RoomEventArgs() { Room = room });
        }

        public override void LeaveRoom()
        {
            Room = RoomDescription.Empty;
            OnRoomLeft();
        }

        protected override void Start()
        {
            // check dependencies
            if (ServiceLocator.Instance.CheckDependencies(this) == false)
            {
                Debug.LogError("Missing dependency, disabling component.");
                enabled = false;
                return;
            }

            // get network service manager provider
            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();

            if (_networkServiceManager != null)
            {
                _networkServiceManager.AvailableServicesChanged += OnAvailableServicesChanged;
                CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.APP_STATE);
            }

            //// get world anchor
            //_worldAnchor = ServiceLocator.Instance.Get<IWorldAnchor>();

            // add own user id to list of users
            AddUser(new UserDescription() { Id = CurrentUserId, RoomId = -1, IsActive = true });

        }

        protected override void CheckAvailableServices(INetworkService.NetworkServiceFilter filter)
        {
            if (_networkServiceManager != null)
            {
                Sessions.Clear();
                foreach (var service in _networkServiceManager.GetAvailableServices(filter))
                {
                    if (service.Type == NetworkServiceDescription.ServiceType.APP_STATE)
                    {
                        Sessions.Add(service);
                    }
                }

                OnSessionsChanged();
            }
        }

    }
}