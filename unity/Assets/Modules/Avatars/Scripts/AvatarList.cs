using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static IMLD.MixedReality.Avatars.StudyManager.AvatarMap;

namespace IMLD.MixedReality.Avatars
{
    public class AvatarList : MonoBehaviour
    {
        public GameObject button;
        public GameObject clippingBounds;
        private AvatarMapEntry[] avatarList;
        public IStudyManager _studyManager;
        private ISessionManager _clientAppStateManager;
        // Start is called before the first frame update
        void Start()
        {
            _clientAppStateManager = ServiceLocator.Instance.Get<ISessionManager>();
            _studyManager = ServiceLocator.Instance.Get<IStudyManager>();

            avatarList = _studyManager.GetAvatars();

            GridObjectCollection grid = GetComponentInChildren<GridObjectCollection>();

            for (int i = 0; i < avatarList.Length; i++)
            {
                AvatarMapEntry avatar = avatarList[i];
                GameObject avatarBtn = Instantiate(button);
                avatarBtn.GetComponent<ButtonConfigHelper>().MainLabelText = avatar.name;
                avatarBtn.gameObject.transform.SetParent(this.GetComponentInChildren<GridObjectCollection>().transform);

                int j = i;

                avatarBtn.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() =>
                {
                    Debug.Log("Button Click" + avatar.name);
                    User[] users = FindObjectsOfType<User>();

                    _clientAppStateManager.UpdateAvatarChoice(j);
                });
            }

            grid.UpdateCollection();
            clippingBounds.GetComponent<ClippingBox>().enabled = true;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}