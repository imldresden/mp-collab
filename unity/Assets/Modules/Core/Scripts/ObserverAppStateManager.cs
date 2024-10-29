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
    public class ObserverAppStateManager : ClientAppStateManager
    {
        public override void JoinSession(NetworkServiceDescription session)
        {
            if (_networkService != null)
            {
                //_networkServiceManager.DisconnectFromService(_currentSession);
                LeaveSession();
            }

            Session = session;
            SessionId = session.SessionId;
            if (_networkServiceManager.TryConnectToService(session, out _networkService))
            {
                // register to all message types when connecting to a session; never connect to a room
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.ROOM_UPDATE, OnRoomUpdate);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.USER_LIST_UPDATE, OnUserListUpdate);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.UPDATE_USER, OnUserUpdate);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.AVATAR_TYPE, OnAvatarTypeUpdate);

                OnSessionJoined();
                OnRoomJoined(new RoomEventArgs());
            }  
        }

        public override void LeaveSession()
        {
            _networkServiceManager.DisconnectFromService(_networkService);

            // unregister from all messages
            _networkService.UnregisterMessageHandler(MessageContainer.MessageType.ROOM_UPDATE);
            _networkService.UnregisterMessageHandler(MessageContainer.MessageType.USER_LIST_UPDATE);
            _networkService.UnregisterMessageHandler(MessageContainer.MessageType.UPDATE_USER);
            _networkService.UnregisterMessageHandler(MessageContainer.MessageType.AVATAR_TYPE);

            _networkService = null;
            Session = null;
            SessionId = Guid.Empty;
            OnSessionLeft();
        }

        public override void JoinRoom(RoomDescription room)
        {
            Debug.LogWarning("Observer cannot join rooms.");
        }

        public override void LeaveRoom()
        {
            Debug.LogWarning("Observer cannot join or leave rooms.");
        }

        protected override void Start()
        {
            // get network service manager provider
            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();

            if (_networkServiceManager != null)
            {
                _networkServiceManager.AvailableServicesChanged += OnAvailableServicesChanged;
                CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.ALL);
            }

            // get world anchor
            _worldAnchor = ServiceLocator.Instance.Get<IWorldAnchor>();

            // get kinect manager
            _kinectManager = ServiceLocator.Instance.Get<IKinectManager>();

            // get audio receiver
            _audioReceiver = ServiceLocator.Instance.Get<IAudioReceiver>();

            // get logger
            _log = ServiceLocator.Instance.Get<ILog>();

            // add own user id to list of users
            AddUser(new UserDescription() { Id = CurrentUserId, RoomId = -1, IsActive = true });
        }

        protected override void OnAvailableServicesChanged(object sender, EventArgs e)
        {
            CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.ALL);
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
                        if (_networkServiceManager is ReplayNetworkServiceManager)
                        {
                            JoinSession(service);
                        }
                        else
                        {
                            Sessions.Add(service);
                        }                        
                    }

                    if (service.Type == NetworkServiceDescription.ServiceType.KINECT_DATA && _kinectManager != null)
                    {
                        _kinectManager.ConnectToKinectService(service);
                    }

                    if (service.Type == NetworkServiceDescription.ServiceType.AUDIO && _audioReceiver != null)
                    {
                        _audioReceiver.ConnectToAudioService(service);
                    }
                }

                OnSessionsChanged();
            }
        }

        protected override void Update()
        {

        }
    }
}