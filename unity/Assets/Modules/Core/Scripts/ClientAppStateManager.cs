using IMLD.MixedReality.Audio;
using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Network;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace IMLD.MixedReality.Core
{
    public class ClientAppStateManager : BaseAppStateManager
    {
        public new IReadOnlyList<Type> Dependencies { get; } = new List<Type> { typeof(INetworkServiceManager), typeof(IWorldAnchor), typeof(IKinectManager), typeof(IAudioReceiver), typeof(ILog), typeof(Config) };
        

        [SerializeField]
        protected User _userPrefab;

        [SerializeField]
        protected DelayedRegistrationRefiner _registrationHelper;

        [SerializeField]
        protected Transform _localUser;

        [SerializeField]
        protected bool _isKinect = false;

        [SerializeField]
        protected HandDataProvider _handDataProvider;

        [SerializeField]
        protected AudioConfigurator _audioConfigurator;

        [SerializeField]
        protected bool _automaticallyReconnect;

        protected INetworkService _networkService;
        protected INetworkServiceManager _networkServiceManager;
        protected IWorldAnchor _worldAnchor;
        protected IKinectManager _kinectManager;
        protected IAudioReceiver _audioReceiver;
        protected ILog _log;
        protected Dictionary<Guid, User> _userGODictionary = new Dictionary<Guid, User>();
        protected Vector3 _previousPosition;
        protected Quaternion _previousRotation;
        private NetworkServiceDescription _previousSession = null;
        private RoomDescription _previousRoom = RoomDescription.Empty;
        private int _avatarId = 0;
        public override void JoinSession(NetworkServiceDescription session)
        {
            if (_networkService != null)
            {
                LeaveSession();
            }

            if (_networkServiceManager.TryConnectToService(session, out _networkService))
            {
                Session = session;
                SessionId = session.SessionId;
                _networkService.ConnectedToServer += OnSessionConnected;
                _networkService.DisconnectedFromServer += OnSessionDisconnected;
            }  
        }


        private void OnSessionDisconnected(object sender, EventArgs e)
        {
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.ROOM_UPDATE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.USER_LIST_UPDATE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.UPDATE_USER);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.AVATAR_TYPE);
            _networkService = null;

            Sessions.Remove(Session);
            
            if (_automaticallyReconnect)
            {
                _previousSession = Session;
                _previousRoom = Room;
            }

            Session = null;
            SessionId = Guid.Empty;
            Room = RoomDescription.Empty;

            OnSessionLost();
        }
        private void OnSessionConnected(object sender, EventArgs e)
        {
            _previousSession = null;
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.ROOM_UPDATE, OnRoomUpdate);

            // send current avatar choice
            _networkService?.SendMessage(new MessageAvatarChoice(_avatarId, CurrentUserId));

            SaveSessionDataToFile();
            OnSessionJoined();
        }

        protected Task OnUserUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.UPDATE_USER)
            {
                var message = MessageUpdateUser.Unpack(container);
                if (message == null)
                {
                    return Task.CompletedTask;
                }

                var user = message.User;
                if (Users.ContainsKey(user.Id))
                {
                    // update existing user
                    UpdateUser(user, message.Position, message.Orientation, message.LeftHand, message.RightHand);

                    // update kinect calibration but only for data from remote users
                    if (user.RoomId != Room.Id)
                    {
                        UpdateKinect(message.KinectId, message.KinectPosition, message.KinectOrientation, message.TrackingQuality); 
                    }
                }
                else
                {
                    // handle new user
                    AddUser(user);
                }
            }

            return Task.CompletedTask;
        }

        protected Task OnUserListUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.USER_LIST_UPDATE)
            {
                List<Guid> deleteList = new List<Guid>(Users.Keys);
                var message = MessageUserListUpdate.Unpack(container);
                foreach(var user in message.Users.Values)
                {
                    if (Users.ContainsKey(user.Id))
                    {
                        // update existing user
                        UpdateUser(user);
                        deleteList.Remove(user.Id);
                    }
                    else
                    {
                        // handle new user
                        AddUser(user);
                    }
                }

                foreach(var userId in deleteList)
                {
                    // remove users that are not in the update list
                    RemoveUser(Users[userId]);
                }
            }

            return Task.CompletedTask;
        }

        protected void UpdateUser(UserDescription user, Vector3 position, Quaternion orientation, HandDataFrame leftHand, HandDataFrame rightHand)
        {
            try
            {
                Users[user.Id] = user;
                _userGODictionary[user.Id].UserDescription = user;
                _userGODictionary[user.Id].transform.localPosition = position;
                _userGODictionary[user.Id].transform.localRotation = orientation;
                //_userGODictionary[user.Id].LeftHand = leftHand.ConvertToGlobal(_worldAnchor.GetRemoteKinect());
                //_userGODictionary[user.Id].RightHand = rightHand.ConvertToGlobal(_worldAnchor.GetRemoteKinect());
                _userGODictionary[user.Id].LeftHand = leftHand.ConvertToGlobal(_kinectManager.GetKinectTransform(user.RoomId));
                _userGODictionary[user.Id].RightHand = rightHand.ConvertToGlobal(_kinectManager.GetKinectTransform(user.RoomId));
                //_log.Write("User\t" + user.Id.ToString() + "\t" + user.RoomId + "\t" + position + "\t" + orientation);
                //_log.Write("user", user.Id, user.RoomId, position, orientation, left);
            }
            catch(Exception)
            {
                Debug.LogWarning("Error updating user.");
            }
        }

        protected void UpdateUser(UserDescription user)
        {
            try
            {
                Users[user.Id] = user;
                _userGODictionary[user.Id].UserDescription = user;
            }
            catch(Exception)
            {
                Debug.LogWarning("Error updating user.");
            }
        }

        protected void AddUser(UserDescription user)
        {
            if (user.RoomId != -1 && user.RoomId != Room.Id)
            {
                Debug.Log("Added user: " + user.Id + ", " + user.RoomId);
                Users.Add(user.Id, user);
                if (_userPrefab != null)
                {
                    var userGO = Instantiate(_userPrefab, _worldAnchor.GetOrigin());
                    userGO.UserDescription = user;
                    _userGODictionary.Add(user.Id, userGO);
                }
            }
        }

        protected void UpdateKinect(Guid kinectId, Vector3 position, Quaternion orientation, float quality)
        {
            if (_kinectManager != null)
            {
                var kinectTransform = _kinectManager.GetKinectTransform(kinectId);
                if (kinectTransform != null)
                {
                    kinectTransform.SetLocalPositionAndRotation(position, orientation);
                    //var updater = kinectTransform.GetComponent<WeightedTransformUpdater>();
                    //if (updater != null)
                    //{
                    //    updater.UpdateTransform(position, orientation, quality);
                    //}
                    //else
                    //{
                    //    Debug.LogWarning("No WeightedTransformUpdater found, using transform directly");
                    //    kinectTransform.SetLocalPositionAndRotation(position, orientation);
                    //}

                }
            }
        }

        protected void RemoveUser(UserDescription user)
        {
            try
            {
                Debug.Log("Removed user: " + user.Id + ", " + user.RoomId);
                Users.Remove(user.Id);
                var userGO = _userGODictionary[user.Id];
                Destroy(userGO.gameObject);
                _userGODictionary.Remove(user.Id);
            }
            catch(Exception)
            {
                Debug.LogWarning("Error removing user!");
            }
        }

        protected Task OnRoomUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.ROOM_UPDATE)
            {
                var message = MessageRoomUpdate.Unpack(container);
                Rooms = message.Rooms;
                foreach (var room in Rooms)
                {
                    if (room.Id == Room.Id)
                    {
                        Room = room;
                        break;
                    }
                }

                if (_previousRoom.Id != -1)
                {
                    JoinRoom(_previousRoom);
                }
                else if (Room.Id == -1)
                {
                    JoinRoomFromConfig();
                }

                OnRoomsChanged();
            }

            return Task.CompletedTask;
        }

        protected Task OnAvatarTypeUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.AVATAR_TYPE)
            {
                var message = MessageAvatarType.Unpack(container);
                var studyManager = ServiceLocator.Instance.Get<IStudyManager>();
                if (studyManager != null && studyManager.AvatarType != (AvatarType)message.AvatarType)
                {
                    studyManager.AvatarType = (AvatarType)message.AvatarType;
                }
            }

            return Task.CompletedTask;
        }

        protected Task OnAvatarChoiceUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.AVATAR_CHOICE)
            {
                var message = MessageAvatarChoice.Unpack(container);

                Debug.Log("User " + message.UserId + "chose Avatar No.: " + message.AvatarId);

                UpdateAvatarForUser(message.UserId, message.AvatarId);
            }

            return Task.CompletedTask;
        }

        private Task OnInteractableObjectUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.OBJECT_UPDATE)
            {
                var message = MessageInteractableObjectUpdate.Unpack(container);

                if (message == null)
                {
                    return Task.CompletedTask;
                }

                // update existing furniture

                if (ServiceLocator.Instance.TryGet<IInteractableManager>(out var InteractableManager))
                {
                    InteractableManager.UpdateInteractablePose(message.ID, new Vector3(message.posX, message.posY, message.posZ), new Quaternion(message.rotX, message.rotY, message.rotZ, message.rotW));
                }
            }

            return Task.CompletedTask;
        }

        private Task OnInteractableObjectListUpdate(MessageContainer container)
        {
            if (ServiceLocator.Instance.TryGet<IInteractableManager>(out var InteractableManager) == false)
            {
                return Task.CompletedTask;
            }

            if (container?.Type == MessageContainer.MessageType.OBJECT_LIST)
            {
                var message = MessageInteractableObjectList.Unpack(container);
                foreach (var kvp in message?.Interactables)
                {
                    InteractableManager.UpdateInteractablePose(kvp.Key, Conversion.FromNumericsVector3(kvp.Value.Position), Conversion.FromNumericsQuaternion(kvp.Value.Rotation));
                }
            }

            return Task.CompletedTask;
        }

        protected Task OnAvatarListUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.AVATAR_LIST)
            {
                var message = MessageAvatarList.Unpack(container);
                foreach (var kvp in message?.Avatars)
                {
                    UpdateAvatarForUser(kvp.Key, kvp.Value);
                }
            }

            return Task.CompletedTask;
        }

        private void UpdateAvatarForUser(Guid userId, int avatarId)
        {
            var User = GetUser(userId);
            if (User != null)
            {
                User.ChangeAvatar(avatarId);
            }
        }

        protected Task OnAudioPositionUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.AUDIO_POSITION)
            {
                var message = MessageAudioPosition.Unpack(container);
                _audioConfigurator.changeAudioPlacement(message.AudioId);
            }           

            return Task.CompletedTask;
        }

        private Task OnTargetNetworkLatencyUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.NETWORK_LATENCY)
            {
                var message = MessageNetworkLatency.Unpack(container);
                _networkServiceManager.SetTargetNetworkLatency(message.Latency);
            }

            return Task.CompletedTask;
        }

        public override void LeaveSession()
        {
            _networkServiceManager.DisconnectFromService(_networkService);

            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.ROOM_UPDATE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.USER_LIST_UPDATE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.UPDATE_USER);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.AVATAR_TYPE);
            _networkService = null;
            Session = null;
            SessionId = Guid.Empty;
            Room = RoomDescription.Empty;
            ClearSessionDataFromFile();
            ClearRoomDataFromFile();
            OnSessionLeft();
        }

        public override void UpdateAvatarChoice(int avatarId)
        {
            Debug.Log("Updating Avatar to number: " + avatarId);
            _avatarId = avatarId;
            if(_avatarId >= 0)
            {
                _networkService?.SendMessage(new MessageAvatarChoice(avatarId, CurrentUserId));
            }
        }

        public override void UpdateInteractableObjectPose(int id, Pose pose)
        {
            _networkService?.SendMessage(new MessageInteractableObjectUpdate(id, pose));
        }

        public override void UpdateAudioPosition(int audioID)
        {
           // audioConfigurator.changeAudioPlacement(audioID);
            _networkService?.SendMessage(new MessageAudioPosition(audioID));
        }

        public override void UpdateTargetNetworkLatency(float latency)
        {
            _networkService?.SendMessage(new MessageNetworkLatency(latency));
        }

        public override void JoinRoom(RoomDescription room)
        {
            _networkService?.SendMessage(new MessageJoinRoom(room.Id, CurrentUserId));
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.USER_LIST_UPDATE, OnUserListUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.UPDATE_USER, OnUserUpdate);
           // _networkService?.RegisterMessageHandler(MessageContainer.MessageType.AVATAR_TYPE, OnAvatarTypeUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.AVATAR_CHOICE, OnAvatarChoiceUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.AVATAR_LIST, OnAvatarListUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.OBJECT_UPDATE, OnInteractableObjectUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.OBJECT_LIST, OnInteractableObjectListUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.AUDIO_POSITION, OnAudioPositionUpdate);
            _networkService?.RegisterMessageHandler(MessageContainer.MessageType.NETWORK_LATENCY, OnTargetNetworkLatencyUpdate);

            _previousRoom = RoomDescription.Empty;
            Room = room;
            SaveRoomDataToFile();
            OnRoomJoined(new RoomEventArgs() { Room = room });

            CheckAvailableServices(INetworkService.NetworkServiceFilter.DEFAULT);
        }

        public override void LeaveRoom()
        {
            _networkService?.SendMessage(new MessageLeaveRoom(CurrentUserId));

            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.USER_LIST_UPDATE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.UPDATE_USER);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.AVATAR_TYPE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.AVATAR_CHOICE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.AVATAR_LIST);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.OBJECT_UPDATE);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.AUDIO_POSITION);
            _networkService?.UnregisterMessageHandler(MessageContainer.MessageType.NETWORK_LATENCY);

            // Disconnect from all services other than app state
            if (_networkServiceManager != null)
            {
                for (int i = _networkServiceManager.RequestedServices.Count - 1; i >= 0; i--)
                {
                    if (_networkServiceManager.RequestedServices[i].ServiceType != NetworkServiceDescription.ServiceType.APP_STATE)
                    {
                        _networkServiceManager.DisconnectFromService(_networkServiceManager.RequestedServices[i]);
                    }
                }
            }            

            Room = RoomDescription.Empty;
            ClearRoomDataFromFile();
            OnRoomLeft();
        }

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
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
                CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.DEFAULT);
            }

            // get world anchor
            _worldAnchor = ServiceLocator.Instance.Get<IWorldAnchor>();

            // get kinect manager
            _kinectManager = ServiceLocator.Instance.Get<IKinectManager>();

            // get audio receiver
            _audioReceiver = ServiceLocator.Instance.Get<IAudioReceiver>();

            // get logger
            _log = ServiceLocator.Instance.Get<ILog>();

            // load client id from config or generate new client id
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null && config.TryLoad<string>("ClientId", out var clientId))
            {
                CurrentUserId = Guid.Parse(clientId);
            }
            else
            {
                CurrentUserId = Guid.NewGuid();
                if (config != null)
                {
                    config.Save("ClientId", CurrentUserId.ToString());
                }
            }

            // add own user id to list of users
            AddUser(new UserDescription() { Id = CurrentUserId, RoomId = -1, IsActive = true});

            // reconnect to session if configured in persistent storage
            ReconnectToSessionFromConfig();
        }

        private void OnApplicationQuit()
        {
            LeaveRoom();
            LeaveSession();
        }

        protected virtual void OnAvailableServicesChanged(object sender, EventArgs e)
        {
            CheckAvailableServices(filter: INetworkService.NetworkServiceFilter.DEFAULT);
        }

        protected virtual void CheckAvailableServices(INetworkService.NetworkServiceFilter filter)
        {
            if (_networkServiceManager != null)
            {
                Sessions.Clear();
                foreach (var service in _networkServiceManager.GetAvailableServices(filter))
                {
                    if (service.Type == NetworkServiceDescription.ServiceType.APP_STATE)
                    {
                        Sessions.Add(service);
                        if (_automaticallyReconnect == true && _previousSession != null)
                        {
                            JoinSession(service);
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

        public override User GetUser(Guid id)
        {
            if (_userGODictionary.TryGetValue(id, out User value))
            {
                return value;
            }

            return null;
        }

        private void ReconnectToSessionFromConfig()
        {
            var config = ServiceLocator.Instance.Get<Config>();
            if (config == null)
            {
                return;
            }

            if (config.TryLoad<NetworkServiceDescription>("Session", out var session))
            {
                JoinSession(session);
            }
        }

        private void JoinRoomFromConfig()
        {
            var config = ServiceLocator.Instance.Get<Config>();
            if (config == null)
            {
                return;
            }

            if (config.TryLoad<RoomDescription>("Room", out var room) && room.Id != -1)
            {
                JoinRoom(room);
            }
        }

        private void SaveSessionDataToFile()
        {
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null)
            {
                config.Save("Session", Session);
            }
        }

        private void SaveRoomDataToFile()
        {
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null)
            {
                config.Save("Room", Room);
            }
        }

        private void ClearSessionDataFromFile()
        {
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null)
            {
                config.TryRemove("Session");
            }
        }

        private void ClearRoomDataFromFile()
        {
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null)
            {
                config.TryRemove("Room");
            }
        }

        private float ComputeKinectPoseQuality()
        {
            float distance = (_kinectManager.GetLocalKinectTransform().position - CameraCache.Main.transform.position).sqrMagnitude;
            float distanceQuality = 1 / (1 + distance); // a value between 1 and 0, depending on the distance between hl and kinect

            float speed = (_previousPosition - CameraCache.Main.transform.position).magnitude / Time.deltaTime;
            float speedQuality = 0.1f / (0.1f + speed); // a value between 1 and 0, depending on the current camera movement speed

            float angularSpeed = Quaternion.Angle(_previousRotation, CameraCache.Main.transform.rotation) / Time.deltaTime;
            float angularSpeedQuality = 10 / (10 + angularSpeed); // a value between 1 and 0, depending on the current camera movement angular speed

            float viewingAngleOrigin = Vector3.Angle(-1 * _worldAnchor.GetOrigin().up, CameraCache.Main.transform.forward);
            float viewingAngleKinect = Vector3.Angle(-1 * _kinectManager.GetLocalKinectTransform().forward, CameraCache.Main.transform.forward);
            float viewingAngleQuality = Math.Clamp((180 - (viewingAngleOrigin + viewingAngleKinect))/180, 0, 1); // a value between 1 and 0, depending on the viewing angle to the qr codes

            return (distanceQuality + speedQuality + angularSpeedQuality + viewingAngleQuality) / 4;
        }

        protected virtual void Update()
        {
            //if (Input.GetKeyUp(KeyCode.D))
            //{
            //    Debug.Log("Droppig session connection.");
            //    (_currentSession as NetworkService).Drop();
            //}

            if (_localUser != null && _handDataProvider != null && _worldAnchor != null)
            {
                // set helper transform for local user
                _localUser.position = CameraCache.Main.transform.position;
                _localUser.rotation = CameraCache.Main.transform.rotation;

                // get hand data relative to kinect
                var leftHand = _handDataProvider.GetHandData(Handedness.Left).ConvertToLocal(_kinectManager.GetLocalKinectTransform());
                var rightHand = _handDataProvider.GetHandData(Handedness.Right).ConvertToLocal(_kinectManager.GetLocalKinectTransform());

                // get kinect pose relative to world origin
                Pose kinectPose = Conversion.GetRelativePose(_kinectManager.GetLocalKinectTransform(), _worldAnchor.GetOrigin());

                // compute kinect pose quality
                float kinectPoseQuality = ComputeKinectPoseQuality();

                // send data over network
                if (_networkService != null && _networkService.Status == INetworkService.NetworkServiceStatus.CONNECTED)
                {
                    _networkService.SendMessage(new MessageUpdateUser(
                        new UserDescription() { Id = CurrentUserId, RoomId = Room.Id, IsActive = true },
                        _localUser.localPosition,
                        _localUser.localRotation,
                        leftHand,
                        rightHand,
                        _kinectManager.LocalKinectId,
                        kinectPose.position,
                        kinectPose.rotation,
                        kinectPoseQuality
                    ));
                }
            }           
        }

        protected void LateUpdate()
        {
            // update previous user position and rotation for next frame
            _previousPosition = CameraCache.Main.transform.position;
            _previousRotation = CameraCache.Main.transform.rotation;
        }
    }
}