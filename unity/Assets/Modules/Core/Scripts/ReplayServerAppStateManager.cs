using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IMLD.MixedReality.Core
{
    public class ReplayServerAppStateManager : BaseAppStateManager
    {
        

        [SerializeField] private AvatarType _avatarType = AvatarType.SIMPLE_MESH;

        private INetworkService _networkService;
        private INetworkServiceManager _networkServiceManager;
        public override Guid SessionId { get; protected set; } = Guid.NewGuid();

        

        

        void Start()
        {
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
                        _avatarType = AvatarType.SIMPLE_MESH;
                        break;
                }

                // send new avatar type over network
                _networkService.SendMessage(new MessageAvatarType(_avatarType));
            }
        }

        public override void StartSession(List<RoomDescription> rooms)
        {
            //Rooms = rooms;
            //if (_networkServiceManager != null)
            //{
            //    _networkService = _networkServiceManager.StartServer(NetworkServiceDescription.ServiceType.APP_STATE);
            //    _networkService.RegisterMessageHandler(MessageContainer.MessageType.ROOM_JOIN, OnRoomUserJoined);
            //    StartCoroutine(AnnounceRoomsAndUsers(1));
            //}


        }

        IEnumerator AnnounceRoomsAndUsers(float waitTime)
        {
            while (true)
            {
                _networkService.SendMessage(new MessageRoomUpdate(Rooms));
                _networkService.SendMessage(new MessageUserListUpdate(Users));
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}