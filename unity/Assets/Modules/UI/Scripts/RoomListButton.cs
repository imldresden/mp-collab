// ------------------------------------------------------------------------------------
// <copyright file="SessionListButton.cs" company="Technische Universität Dresden">
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

using IMLD.MixedReality.Network;
using TMPro;
using UnityEngine;

namespace IMLD.MixedReality.UI
{
    /// <summary>
    /// This Unity component is a button in the room list. Each button represents one joinable room.
    /// </summary>
    public class RoomListButton : MonoBehaviour
    {
        public TextMeshPro TextMesh;

        /// <summary>
        /// Information about the room attached to this button
        /// </summary>
        private RoomDescription _roomInfo;

        /// <summary>
        /// The material for the text so we can change the text color.
        /// </summary>
        private Material _textMaterial;

        /// <summary>
        /// When the user clicks a session this will route that information to the
        /// scrolling UI control so it knows which session is selected.
        /// </summary>
        public void OnClicked()
        {
            SessionListUIController.Instance.SetSelectedRoom(_roomInfo);
        }
        public void OnMouseUpAsButton()
        {
            SessionListUIController.Instance.SetSelectedRoom(_roomInfo);
        }

        /// <summary>
        /// Sets the room information associated with this button
        /// </summary>
        /// <param name="roomInfo">The room info</param>
        public void SetRoomInfo(RoomDescription roomInfo)
        {
            _roomInfo = roomInfo;
            TextMesh.text = _roomInfo.Name + "(" + _roomInfo.UserCount + ")";
            if (_roomInfo.Id == SessionListUIController.Instance.SelectedRoom.Id)
            {
                TextMesh.GetComponent<MeshRenderer>().material.SetColor(Shader.PropertyToID("_Color"), Color.blue);

                TextMesh.color = Color.blue;
            }
            else
            {
                TextMesh.GetComponent<MeshRenderer>().material.SetColor(Shader.PropertyToID("_Color"), Color.yellow);
                TextMesh.color = Color.yellow;
            }
        }

        /// <summary>
        /// Called by unity when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_textMaterial != null)
            {
                Destroy(_textMaterial);
                _textMaterial = null;
            }
        }
    }
}