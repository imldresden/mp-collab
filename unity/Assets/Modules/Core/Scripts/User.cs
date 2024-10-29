using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Network;
using IMLD.MixedReality.Audio;
using System;

namespace IMLD.MixedReality.Core
{
    public class User : MonoBehaviour
    {
        /// <summary>
        /// The user id
        /// </summary>
        public Guid Id { get { return UserDescription.Id; } }

        /// <summary>
        /// Indicates wether the User is local (true) or remote (false)
        /// </summary>
        public bool IsLocal { get { return _sessionManager.Room.Id == UserDescription.RoomId; } }

        /// <summary>
        /// Easy access to the room id.
        /// </summary>
        public int RoomId { get{return UserDescription.RoomId; } }

        /// <summary>
        /// The audio receiver of this user, receives and plays back voice data.
        /// </summary>
        [field: SerializeField]
        public AudioPlayer AudioPlayer { get; set; }

        /// <summary>
        /// The avatar representation of the user. Typically, only remote users should have one.
        /// </summary>
        [SerializeField][ReadOnly] private AbstractAvatar _avatar;

        private ISessionManager _sessionManager;
        private IStudyManager _studyManager;
        
        private AvatarType _avatarType = AvatarType.NONE;

        public UserDescription UserDescription { get; set; }
        public HandDataFrame LeftHand { get; set; }
        public HandDataFrame RightHand { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();
            _studyManager = ServiceLocator.Instance.Get<IStudyManager>();

            if (_studyManager != null)
            {
                //_studyManager.AvatarTypeChanged += OnAvatarTypeChanged;
                //ChangeAvatarType(_studyManager.AvatarType);

                //default Avatar is first in the list
                ChangeAvatar(0);
            }
        }
        private void Update()
        {
            if(_avatar != null)
            {
                _avatar.ApplyHandPosture(LeftHand, RightHand);
            }
            if (UserDescription.IsActive == false)
            {
                HideAvatar();
            }
            else
            {
                ShowAvatar();
            }
        }

        private void OnDestroy()
        {
            if (_avatar != null)
            {
                Destroy(_avatar.gameObject);
            }
        }

        private void OnAvatarTypeChanged(object sender, AvatarEventArgs e)
        {
            ChangeAvatarType(e.AvatarType);
        }

        private void ChangeAvatarType(AvatarType type)
        {
            if (_avatarType == type)
            {
                return;
            }

            _avatarType = type;

            // dispose old avatar
            if (_avatar != null)
            {
                Destroy(_avatar.gameObject);
            }

            // create new avatar using the factory provided by IStudyManager:
            _avatar = _studyManager.CreateAvatar(_avatarType, transform, RoomId);
            _avatar.User = this;
        }

        public void ChangeAvatar(int avatarIndex)
        {
            // check if we need to change the avatar
            if (_avatar != null && _avatar.AvatarId == avatarIndex)
            {
                return;
            }

            // dispose old avatar
            if (_avatar != null)
            {
                Destroy(_avatar.gameObject);
            }

            // create new avatar using the factory provided by IStudyManager:
            _avatar = _studyManager.CreateAvatarFromMenu(avatarIndex, transform, RoomId);
            _avatar.User = this;
        }

        public void HideAvatar()
        {
            // dispose old avatar
            if (_avatar != null && _avatar.gameObject != null)
            {
                _avatar.gameObject.SetActive(false);
            }
        }

        public void ShowAvatar()
        {
            // dispose old avatar
            if (_avatar != null && _avatar.gameObject != null)
            {
                _avatar.gameObject.SetActive(true);
            }
        }
    }
}