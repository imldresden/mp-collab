using IMLD.MixedReality.Network;
using IMLD.MixedReality.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CancelButton : MonoBehaviour
{
    /// <summary>
    /// When the user clicks cancel, cancel reconnect attemps.
    /// </summary>
    public void OnClicked()
    {
        SessionListUIController.Instance.CancelReconnectAttempt();
    }

    public void OnMouseUpAsButton()
    {
        SessionListUIController.Instance.CancelReconnectAttempt();
    }
}
