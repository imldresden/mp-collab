using IMLD.MixedReality.Core;
using System;
using UnityEngine;
using static IMLD.MixedReality.Avatars.StudyManager.AvatarMap;

namespace IMLD.MixedReality.Avatars
{
    public class StudyManager : MonoBehaviour, IStudyManager
    {

        public AvatarMapEntry[] avatars;

        [SerializeField]
        private AbstractAvatar _meshAvatarPrefab;

        [SerializeField]
        private AbstractAvatar _simpleAvatarPrefab;

        [SerializeField]
        private AbstractAvatar _pointCloudAvatarPrefab;

        [SerializeField]
        private AbstractAvatar _validAvatarPrefab;

        [SerializeField]
        private AbstractAvatar _rpmAvatarPrefab;

        private IWorldAnchor _worldAnchor;

        private IKinectManager _kinectManager;

        public AvatarMapEntry[] GetAvatars()
        {
            return avatars;
        }


        public AvatarType AvatarType
        {
            get { return _avatarType; }
            set
            {
                _avatarType = value;
                AvatarTypeChanged?.Invoke(this, new AvatarEventArgs() { AvatarType = _avatarType });
            }
        }

        public AbstractAvatar CreateAvatar(AvatarType type, Transform parent, int roomId)
        {
            AbstractAvatar returnValue;

            Transform avatarParent = parent;
            if (_kinectManager != null)
            {
                avatarParent = _kinectManager.GetKinectTransform(roomId);
            }

            switch (type)
            {
                case AvatarType.MESH:
                    returnValue = Instantiate(_meshAvatarPrefab, avatarParent);
                    return returnValue;
                case AvatarType.SIMPLE_MESH:
                    returnValue = Instantiate(_simpleAvatarPrefab, parent);
                    return returnValue;
                case AvatarType.POINTCLOUD:
                    returnValue = Instantiate(_pointCloudAvatarPrefab, avatarParent);
                    return returnValue;
                case AvatarType.VALID:
                    returnValue = Instantiate(_validAvatarPrefab, avatarParent);
                    return returnValue;
                case AvatarType.RPM:
                    returnValue = Instantiate(_rpmAvatarPrefab, avatarParent);
                    return returnValue;
                default:
                    return null;
            }
        }

        public AbstractAvatar CreateAvatarFromMenu(int avatarIndex, Transform parent, int roomId)
        {
            AbstractAvatar returnValue;

            Transform avatarParent = parent;

            if (_kinectManager != null)
            {
                avatarParent = _kinectManager.GetKinectTransform(roomId);
            }

            try
            {
                returnValue = Instantiate(avatars[avatarIndex].avatarPrefab, avatarParent);
                returnValue.AvatarId = avatarIndex;
                return returnValue;
            }
            catch (Exception ex)
            {
                Debug.LogError("Avatar with id " + avatarIndex + " could not be created: " + ex.Message);
            }
            return null;
        }


        [SerializeField] private AvatarType _avatarType = AvatarType.SIMPLE_MESH;

        public event EventHandler<AvatarEventArgs> AvatarTypeChanged;

        // Start is called before the first frame update
        void Start()
        {
            _kinectManager = ServiceLocator.Instance.Get<IKinectManager>();
        }

        // Update is called once per frame
        void Update()
        {

        }
        public class AvatarMap : ScriptableObject
        {
            [System.Serializable]
            public class AvatarMapEntry
            {
                public string name;
                public AbstractAvatar avatarPrefab;
            }
        }
    }
}