// ------------------------------------------------------------------------------------
// <copyright file="SessionListUIController.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// <comment>
//      Based on code by Microsoft.
//      Copyright (c) Microsoft Corporation. All rights reserved.
//      Licensed under the MIT License.
// </comment>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using TMPro;
using UnityEngine;

namespace IMLD.MixedReality.UI
{
    /// <summary>
    /// Controls a scrollable list of sessions.
    /// </summary>
    public class SessionListUIController : MonoBehaviour
    {
        public static SessionListUIController Instance = null;

        /// <summary>
        /// List of session controls that will have the session info on them.
        /// </summary>
        public SessionListButton[] SessionControls;

        /// <summary>
        /// List of room controls that will have the room info on them.
        /// </summary>
        public RoomListButton[] RoomControls;

        public TextMeshPro Title;

        public GameObject WaitingScreen;

        private ISessionManager _sessionManager;

        /// <summary>
        /// Keeps track of the current index that is the 'top' of the UI list
        /// to enable scrolling.
        /// </summary>
        private int _sessionIndex = 0;
        private int _roomIndex = 0;

        /// <summary>
        /// Current list of sessions.
        /// </summary>
        private List<NetworkServiceDescription> _sessionList = new List<NetworkServiceDescription>();

        /// <summary>
        /// Current list of rooms.
        /// </summary>
        private List<RoomDescription> _roomList = new List<RoomDescription>();

        /// <summary>
        /// Gets the session the user has currently selected.
        /// </summary>
        public NetworkServiceDescription SelectedSession { get; private set; }

        /// <summary>
        /// Gets the room the user has currently selected.
        /// </summary>
        public RoomDescription SelectedRoom { get; private set; }

        /// <summary>
        /// Joins the selected session if there is a selected session.
        /// </summary>
        public void JoinSelectedSession()
        {
            // TODO: hand over control to some session manager

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Updates which session is the 'top' session in the list, and sets the
        /// session buttons accordingly
        /// </summary>
        /// <param name="direction">are we scrolling up, down, or not scrolling</param>
        public void ScrollSessions(int direction)
        {
            int sessionCount = _sessionList == null ? 0 : _sessionList.Count;

            // Update the session index
            _sessionIndex = Mathf.Clamp(_sessionIndex + direction, 0, Mathf.Max(0, sessionCount - SessionControls.Length));

            // Update all of the controls
            for (int index = 0; index < SessionControls.Length; index++)
            {
                if (_sessionIndex + index < sessionCount)
                {
                    SessionControls[index].gameObject.SetActive(true);
                    NetworkServiceDescription sessionInfo = _sessionList[_sessionIndex + index];
                    SessionControls[index].SetSessionInfo(sessionInfo);
                }
                else
                {
                    SessionControls[index].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Updates which room is the 'top' room in the list, and sets the
        /// room buttons accordingly
        /// </summary>
        /// <param name="direction">are we scrolling up, down, or not scrolling</param>
        public void ScrollRooms(int direction)
        {
            int roomCount = _roomList == null ? 0 : _roomList.Count;

            // Update the session index
            _roomIndex = Mathf.Clamp(_roomIndex + direction, 0, Mathf.Max(0, roomCount - RoomControls.Length));

            // Update all of the controls
            for (int index = 0; index < RoomControls.Length; index++)
            {
                if (_roomIndex + index < roomCount)
                {
                    RoomControls[index].gameObject.SetActive(true);
                    RoomDescription roomInfo = _roomList[_roomIndex + index];
                    RoomControls[index].SetRoomInfo(roomInfo);
                }
                else
                {
                    RoomControls[index].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Sets the selected session and automatically connects to the service to get a list of rooms
        /// </summary>
        /// <param name="sessionInfo">The session to set as selected</param>
        public void SetSelectedSession(NetworkServiceDescription sessionInfo)
        {
            SelectedSession = sessionInfo;

            if (_sessionManager != null)
            {
                _sessionManager.JoinSession(sessionInfo);
            }

            // Recalculating the session list so we can update the text colors.
            ScrollSessions(0);
        }

        /// <summary>
        /// Sets the selected room
        /// </summary>
        /// <param name="roomInfo">The room to set as selected</param>
        public void SetSelectedRoom(RoomDescription roomInfo)
        {
            SelectedRoom = roomInfo;

            // trigger joining the session properly and setting the room number
            if (_sessionManager != null)
            {
                _sessionManager.JoinRoom(roomInfo);
            }
        }

        public void CancelReconnectAttempt()
        {
            ShowSessionsScreen();
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ClearSessionList()
        {
            _sessionList.Clear();
        }

        private void AddSessionEntry(NetworkServiceDescription service)
        {
            _sessionList.Add(service);
        }

        private void ClearRoomList()
        {
            _roomList.Clear();
        }

        private void AddRoomEntry(RoomDescription room)
        {
            _roomList.Add(room);
        }
        private void ShowWaitingScreen()
        {
            gameObject.SetActive(true);

            if (SessionControls != null && SessionControls.Length > 0)
            {
                // get the parent of the first session control, deactivate the whole group.
                SessionControls[0].transform.parent.gameObject.SetActive(false);
            }

            if (RoomControls != null && RoomControls.Length > 0)
            {
                // get the parent of the first room control, activate the whole group.
                RoomControls[0].transform.parent.gameObject.SetActive(false);
            }

            if (Title != null)
            {
                Title.text = "Please wait...";
            }

            if (WaitingScreen != null)
            {
                WaitingScreen.SetActive(true);
            }
        }

        private void ShowSessionsScreen()
        {
            gameObject.SetActive(true);

            if (SessionControls != null && SessionControls.Length > 0)
            {
                // get the parent of the first session control, deactivate the whole group.
                SessionControls[0].transform.parent.gameObject.SetActive(true);
            }

            if (RoomControls != null && RoomControls.Length > 0)
            {
                // get the parent of the first room control, activate the whole group.
                RoomControls[0].transform.parent.gameObject.SetActive(false);
            }

            if (Title != null)
            {
                Title.text = "Select Session";
            }

            if (WaitingScreen != null)
            {
                WaitingScreen.SetActive(false);
            }
        }

        private void ShowRoomsScreen()
        {
            gameObject.SetActive(true);

            if (RoomControls != null && RoomControls.Length > 0)
            {
                // get the parent of the first room control, activate the whole group.
                RoomControls[0].transform.parent.gameObject.SetActive(true);
            }

            if (SessionControls != null && SessionControls.Length > 0)
            {
                // get the parent of the first session control, deactivate the whole group.
                SessionControls[0].transform.parent.gameObject.SetActive(false);
            }

            if (Title != null)
            {
                Title.text = "Select Room";
            }

            if (WaitingScreen != null)
            {
                WaitingScreen.SetActive(false);
            }
        }

        private void Start()
        {
            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();
            _sessionManager.SessionsChanged += OnSessionsChanged;
            _sessionManager.RoomsChanged += OnRoomsChanged;
            _sessionManager.SessionJoined += OnSessionJoined;
            _sessionManager.RoomJoined += OnRoomJoined;
            _sessionManager.SessionLeft += OnSessionLeft;
            _sessionManager.SessionLost += OnSessionLost;
            _sessionManager.RoomLeft += OnRoomLeft;

            ScrollSessions(0);
        }

        private void OnRoomLeft(object sender, EventArgs e)
        {
            ClearRoomList();
            ShowRoomsScreen();
        }

        private void OnSessionLeft(object sender, EventArgs e)
        {
            ClearSessionList();
            ShowSessionsScreen();
        }
        private void OnSessionLost(object sender, EventArgs e)
        {
            ClearSessionList();
            ShowWaitingScreen();
        }

        private void OnRoomJoined(object sender, RoomEventArgs e)
        {
            gameObject.SetActive(false);
        }

        private void OnSessionJoined(object sender, EventArgs e)
        {
            ShowRoomsScreen();
        }

        private void OnSessionsChanged(object sender, EventArgs e)
        {
            if (_sessionManager != null)
            {
                ClearSessionList();
                foreach (var service in _sessionManager.Sessions)
                {
                    if (service.Type == NetworkServiceDescription.ServiceType.APP_STATE)
                    {
                        // add entry to list of sessions
                        AddSessionEntry(service);
                    }
                }
                ScrollSessions(0);
            }
        }

        private void OnRoomsChanged(object sender, EventArgs e)
        {
            ClearRoomList();
            foreach (var room in _sessionManager.Rooms)
            {
                // add entry to list of rooms
                AddRoomEntry(room);
            }
            ScrollRooms(0);
        }
    }
}