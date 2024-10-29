using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IMLD.MixedReality.Core
{
    public class ServerAppStateManager : BaseAppStateManager
    {
        [SerializeField]
        private List<string> _roomNames;

        [SerializeField]
        private int _roomNumber;

        [SerializeField] private AvatarType _avatarType = AvatarType.SIMPLE_MESH;

        public new IReadOnlyList<Type> Dependencies { get; } = new List<Type> { typeof(INetworkServiceManager) };

        private INetworkService _networkService;
        private INetworkServiceManager _networkServiceManager;
        public override Guid SessionId { get; protected set; } = Guid.NewGuid();

        private Dictionary<Guid, int> _userAvatars = new Dictionary<Guid, int>();
        private Dictionary<int, MessageInteractableObjectList.InteractableObjectStruct> _interactables = new Dictionary<int, MessageInteractableObjectList.InteractableObjectStruct>();

        private void Awake()
        {
            // create list of rooms
            Rooms = new List<RoomDescription>();
            for (int i = 0; i < _roomNames.Count; i++)
            {
                Rooms.Add(new RoomDescription() { Id = i, Name = _roomNames[i], UserCount = 0 });
                if (i == _roomNumber)
                {
                    Room = Rooms[i];
                }
            }
        }

        void Start()
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
            
            // starts the session with the configured set of rooms
            StartSession(Rooms);
        }

        void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                switch (_avatarType)
                {
                    case AvatarType.SIMPLE_MESH:
                        _avatarType = AvatarType.MESH;
                        break;
                    case AvatarType.MESH:
                        _avatarType = AvatarType.POINTCLOUD;
                        break;
                    case AvatarType.POINTCLOUD:
                        _avatarType = AvatarType.VALID;
                        break;
                    case AvatarType.VALID:
                        _avatarType = AvatarType.RPM;
                        break;
                    case AvatarType.RPM:
                        _avatarType = AvatarType.SIMPLE_MESH;
                        break;

                }

                // send new avatar type over network
                //_networkService.SendMessage(new MessageAvatarType(_avatarType));
            }
        }

        public override void StartSession(List<RoomDescription> rooms)
        {
            Rooms = rooms;
            if (_networkServiceManager != null)
            {
                _networkService = _networkServiceManager.StartServer(NetworkServiceDescription.ServiceType.APP_STATE);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.ROOM_JOIN, OnRoomUserJoined);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.ROOM_LEAVE, OnRoomUserLeft);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.AVATAR_CHOICE, OnAvatarChoiceUpdate);
                _networkService.RegisterMessageHandler(MessageContainer.MessageType.OBJECT_UPDATE, OnInteractableObjectUpdate);
                _networkService.ClientDisconnected += OnClientDisconnected;
                _networkService.ClientDisappeared += OnClientDisappeared;
                _networkService.ClientReappeared += OnClientReappeared;
                StartCoroutine(AnnounceRoomsAndUsers(waitTime: 1));
            }

            
        }

        private Task OnInteractableObjectUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.OBJECT_UPDATE)
            {
                var message = MessageInteractableObjectUpdate.Unpack(container);
                _interactables[message.ID] = new MessageInteractableObjectList.InteractableObjectStruct
                {
                    Position = new System.Numerics.Vector3(message.posX, message.posY, message.posZ),
                    Rotation = new System.Numerics.Quaternion(message.rotX, message.rotY, message.rotZ, message.rotZ)
                };
            }

            return Task.CompletedTask;
        }

        private Task OnAvatarChoiceUpdate(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.AVATAR_CHOICE)
            {
                var message = MessageAvatarChoice.Unpack(container);
                _userAvatars[message.UserId] = message.AvatarId;
            }

            return Task.CompletedTask;
        }

        private void OnClientDisconnected(object sender, ConnectionInfo client)
        {
            HandleUserLeft(client.Id);
        }

        private void OnClientDisappeared(object sender, ConnectionInfo client)
        {
            HandleUserDisappeared(client.Id);
        }

        private void OnClientReappeared(object sender, ConnectionInfo client)
        {
            HandleUserReappeared(client.Id);
        }
        

        /// <summary>
        /// Callback for network messages of type MessageType.ROOM_JOIN
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        private Task OnRoomUserJoined(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.ROOM_JOIN)
            {
                var message = MessageJoinRoom.Unpack(container);

                // find room corresponding to room id in the message
                var newRoomIdx = Rooms.FindIndex(x => x.Id == message.RoomId);
                var room = Rooms[newRoomIdx];

                // check if user is already known
                if (Users.ContainsKey(message.UserId))
                {
                    // check user's previous room
                    if (Users[message.UserId].RoomId == message.RoomId)
                    {
                        // nothing changed, message was in error, ignore
                        return Task.CompletedTask;
                    }
                    else
                    {
                        // room has changed, decrease number of users in the old room
                        int oldRoomIdx = Rooms.FindIndex(x => x.Id == Users[message.UserId].RoomId);
                        var oldRoom = Rooms[oldRoomIdx];
                        oldRoom.UserCount--;
                        Rooms[oldRoomIdx] = oldRoom;

                        // fire event for user left room
                        OnUserLeftRoom(new UserSessionEventArgs() { UserId = message.UserId, Room = room });

                        // increase number of users in room
                        room.UserCount++;
                        Rooms[newRoomIdx] = room;

                        // update user in collection
                        Users[message.UserId] = new UserDescription() { Id = message.UserId, RoomId = room.Id, IsActive = true };

                        // send update to clients
                        _networkService.SendMessage(new MessageUserListUpdate(Users));

                        // send current avatar type over network
                        //_networkService.SendMessage(new MessageAvatarType(_avatarType));

                        // send interactable states
                        _networkService.SendMessage(new MessageInteractableObjectList(_interactables));

                        // invoke callback for new user in room
                        OnUserJoinedRoom(new UserSessionEventArgs() { UserId = message.UserId, Room = room });
                    }
                }
                else
                {
                    // at this point, user is new...

                    // increase number of users in room
                    room.UserCount++;
                    Rooms[newRoomIdx] = room;

                    // add new user to collection
                    Users.Add(message.UserId, new UserDescription() { Id = message.UserId, RoomId = room.Id, IsActive = true });

                    // send update to clients
                    _networkService.SendMessage(new MessageUserListUpdate(Users));

                    // send current avatar type over network
                    //_networkService.SendMessage(new MessageAvatarType(_avatarType));

                    // invoke callback for new user
                    OnUserJoinedRoom(new UserSessionEventArgs() { UserId = message.UserId, Room = room });
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Callback for network messages of type MessageType.ROOM_LEAVE
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        private Task OnRoomUserLeft(MessageContainer container)
        {
            if (container?.Type == MessageContainer.MessageType.ROOM_LEAVE)
            {
                var message = MessageLeaveRoom.Unpack(container);

                HandleUserLeft(message.UserId);
            }

            return Task.CompletedTask;
        }

        private void HandleUserLeft(Guid userId)
        {
            // check if user is actually known
            if (Users.ContainsKey(userId))
            {
                // get room that the user just left
                var roomId = Users[userId].RoomId;

                // decrease number of users in room
                var room = Rooms[roomId];
                room.UserCount--;
                Rooms[roomId] = room;

                // remove user
                Users.Remove(userId);

                // send update to clients
                _networkService.SendMessage(new MessageUserListUpdate(Users));

                // invoke callback for user leaving room
                OnUserLeftRoom(new UserSessionEventArgs() { UserId = userId, Room = room });
            }
        }

        private void HandleUserDisappeared(Guid userId)
        {
            if (Users.TryGetValue(userId, out var user))
            {
                user.IsActive = false;
            }
        }

        private void HandleUserReappeared(Guid userId)
        {
            if (Users.TryGetValue(userId, out var user))
            {
                user.IsActive = true;
            }

            // send interactable states
            _networkService.SendMessage(new MessageInteractableObjectList(_interactables));
        }

        IEnumerator AnnounceRoomsAndUsers(float waitTime)
        {
            while (true)
            {
                _networkService.SendMessage(new MessageRoomUpdate(Rooms));
                _networkService.SendMessage(new MessageUserListUpdate(Users));
                _networkService.SendMessage(new MessageAvatarList(_userAvatars));
                //_networkService.SendMessage(new MessageInteractableObjectList(_interactables));
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}