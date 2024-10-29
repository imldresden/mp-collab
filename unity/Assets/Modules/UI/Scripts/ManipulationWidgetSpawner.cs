using IMLD.MixedReality.Avatars;
using IMLD.MixedReality.Core;
using IMLD.MixedReality.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManipulationWidgetSpawner : MonoBehaviour
{
    public ManipulationWidgetManager _manipulationWidgetManager;
    private GameObject _widget;
    public void OnClick()
    {
        Debug.LogWarning("Clicking on Manipulation Spawner.");
        KinectManager kinectManager = ServiceLocator.Instance.Get<IKinectManager>() as KinectManager;
        if (kinectManager != null)
        {
            Debug.LogWarning("Kinect Manager found.");
            if (_manipulationWidgetManager != null && _manipulationWidgetManager.WidgetsEnabled)
            {
                if (_widget == null)
                {
                    Debug.LogWarning("Creating Widget!");
                    _widget = _manipulationWidgetManager.CreateManipulationWidget(transform);
                    kinectManager.StartManualCalibration(_widget.transform);
                }
                else
                {
                    _manipulationWidgetManager.DeleteWidget(_widget);
                    kinectManager.StopManualCalibration();
                }
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Started manual calibration!");
            OnClick();
        }
    }
}
