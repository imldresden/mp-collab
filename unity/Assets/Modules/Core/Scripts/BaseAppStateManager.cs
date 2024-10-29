using IMLD.MixedReality.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public class BaseAppStateManager : MonoBehaviour, ISessionManager
    {
        public event EventHandler SessionsChanged;
        protected virtual void OnSessionsChanged()
        {
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RoomsChanged;
        protected virtual void OnRoomsChanged()
        {
            RoomsChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler SessionJoined;
        protected virtual void OnSessionJoined()
        {
            SessionJoined?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<RoomEventArgs> RoomJoined;
        protected virtual void OnRoomJoined(RoomEventArgs e)
        {
            RoomJoined?.Invoke(this, e);
        }

        public event EventHandler SessionLeft;
        protected virtual void OnSessionLeft()
        {
            SessionLeft?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler SessionLost;
        protected virtual void OnSessionLost()
        {
            SessionLost?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RoomLeft;
        protected virtual void OnRoomLeft()
        {
            RoomLeft?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<UserSessionEventArgs> UserJoinedRoom;
        protected virtual void OnUserJoinedRoom(UserSessionEventArgs e)
        {
            UserJoinedRoom?.Invoke(this, e);
        }

        public event EventHandler<UserSessionEventArgs> UserLeftRoom;
        protected virtual void OnUserLeftRoom(UserSessionEventArgs e)
        {
            UserLeftRoom?.Invoke(this, e);
        }

        public virtual RoomDescription Room { get; protected set; } = new RoomDescription() { Id = -1, Name = "disconnected", UserCount = 1 };

        public virtual NetworkServiceDescription Session { get; protected set; }

        public virtual List<RoomDescription> Rooms { get;  set; } = new List<RoomDescription>();

        public virtual List<NetworkServiceDescription> Sessions { get; protected set; } = new List<NetworkServiceDescription>();

        public virtual Dictionary<Guid, UserDescription> Users { get; protected set; } = new Dictionary<Guid, UserDescription>();

        public virtual Guid CurrentUserId { get; protected set; } = Guid.Empty;

        public virtual Guid SessionId { get; protected set; } = Guid.Empty;

        public IReadOnlyList<Type> Dependencies { get; } = new List<Type> { };

        public virtual User GetUser(Guid id)
        {
            return null;
        }

        public virtual void StartSession(List<RoomDescription> rooms)
        {

        }

        public virtual void JoinSession(NetworkServiceDescription session)
        {

        }

        public virtual void LeaveSession()
        {

        }

        public virtual void JoinRoom(RoomDescription room)
        {

        }

        public virtual void LeaveRoom() { }

        public virtual void UpdateAvatarChoice(int i) { }
        public virtual void UpdateInteractableObjectPose(int Id, Pose pose) { }

        public virtual void UpdateAudioPosition(int i) { }

        public virtual void UpdateTargetNetworkLatency(float latency) { }
    }
}