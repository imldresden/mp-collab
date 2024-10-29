using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LatencyUIController : MonoBehaviour
{
    public TextMeshPro ValueLabel;
    public PinchSlider Slider;
    private INetworkServiceManager _networkServiceManager;
    private float _latency;
    private float _sliderValue;

    // Start is called before the first frame update
    void Start()
    {
        _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();
    }

    private void Update()
    {
        if (_networkServiceManager != null)
        {
            _latency = _networkServiceManager.GetTargetNetworkLatency();
            _sliderValue = ComputeSliderFromLatency(_latency);
        }
        else
        {
            _latency = 0;
            _sliderValue = 0;
        }

        if (Slider != null)
        {
            Slider.SliderValue = _sliderValue;
        }
    }

    public void OnSetGlobal()
    {
        ServiceLocator.Instance.Get<ISessionManager>().UpdateTargetNetworkLatency(_latency);
    }

    public void OnSliderUpdate(SliderEventData value)
    {
        _latency = ComputeLatencyFromSlider(value.NewValue);
        if (ValueLabel != null)
        {
            ValueLabel.text = Mathf.RoundToInt(_latency * 1000) + " ms";
        }
        if (_networkServiceManager != null)
        {
            _networkServiceManager.SetTargetNetworkLatency(_latency);
            Debug.Log("Requested latency set to " + _latency + ", minimal latency " + _networkServiceManager.GetEstimatedNetworkLatency());
        }
    }

    private float ComputeLatencyFromSlider(float value)
    {
        float minLatency = 0.0f;
        if (_networkServiceManager != null)
        {
            minLatency = _networkServiceManager.GetEstimatedNetworkLatency();
        }

        float baselineLatency = Mathf.Floor(minLatency / 0.025f) * 0.025f;

        switch (value)
        {
            case < 0.1f:
                return 0.0f;
            case < 0.2f:
                return minLatency;
            case < 0.3f:
                return baselineLatency + 0.025f;
            case < 0.4f:
                return baselineLatency + 0.050f;
            case < 0.5f:
                return baselineLatency + 0.075f;
            case < 0.6f:
                return baselineLatency + 0.100f;
            case < 0.7f:
                return baselineLatency + 0.125f;
            case < 0.8f:
                return baselineLatency + 0.150f;
            case < 0.9f:
                return baselineLatency + 0.175f;
            case < 1.0f:
                return baselineLatency + 0.200f;
            default:
                return baselineLatency + 0.225f;
        }
    }

    private float ComputeSliderFromLatency(float value)
    {
        float minLatency = 0.0f;
        if (_networkServiceManager != null)
        {
            minLatency = _networkServiceManager.GetEstimatedNetworkLatency();
        }

        float baselineLatency = Mathf.Floor(minLatency / 0.025f) * 0.025f;

        if (value <= float.Epsilon)
        {
            return 0.0f;
        }
        else if (value <= minLatency)
        {
            return 0.1f;
        }
        else if (value <= baselineLatency + 0.025f)
        {
            return 0.2f;
        }
        else if (value <= baselineLatency + 0.050f)
        {
            return 0.3f;
        }
        else if (value <= baselineLatency + 0.075f)
        {
            return 0.4f;
        }
        else if (value <= baselineLatency + 0.100f)
        {
            return 0.5f;
        }
        else if (value <= baselineLatency + 0.125f)
        {
            return 0.6f;
        }
        else if (value <= baselineLatency + 0.150f)
        {
            return 0.7f;
        }
        else if (value <= baselineLatency + 0.175f)
        {
            return 0.8f;
        }
        else if (value <= baselineLatency + 0.200f)
        {
            return 0.9f;
        }
        else
        {
            return 1.0f;
        }
    }
}
