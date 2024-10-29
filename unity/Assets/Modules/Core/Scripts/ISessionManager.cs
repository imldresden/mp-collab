using IMLD.MixedReality.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public interface ISessionManager : IService
    {
        event EventHandler SessionsChanged;
        event EventHandler RoomsChanged;
        event EventHandler SessionJoined;
        event EventHandler<RoomEventArgs> RoomJoined;
        event EventHandler SessionLeft;
        event EventHandler SessionLost;
        event EventHandler RoomLeft;
        event EventHandler<UserSessionEventArgs> UserJoinedRoom;
        event EventHandler<UserSessionEventArgs> UserLeftRoom;

        RoomDescription Room { get; }

        NetworkServiceDescription Session { get; }

        List<RoomDescription> Rooms { get; }

        List<NetworkServiceDescription> Sessions { get; }

        Dictionary<Guid, UserDescription> Users { get; }

        User GetUser(Guid id);

        Guid CurrentUserId { get; }

        public Guid SessionId { get; }

        void StartSession(List<RoomDescription> rooms);
        void JoinSession(NetworkServiceDescription session);
        void LeaveSession();
        void JoinRoom(RoomDescription room);
        void LeaveRoom();
        void UpdateAvatarChoice(int j);
        void UpdateInteractableObjectPose(int Id, Pose pose);
        void UpdateAudioPosition(int audioID);
        void UpdateTargetNetworkLatency(float latency);
    }

    public class UserSessionEventArgs
    {
        public Guid UserId;
        public RoomDescription Room;
    }

    public class RoomEventArgs
    {
        public RoomDescription Room;
    }
}