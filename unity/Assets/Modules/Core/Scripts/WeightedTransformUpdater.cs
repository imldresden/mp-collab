using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeightedTransformUpdater : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private float _qualityDegradation = 0.99f;
    [SerializeField] private bool _useSmooting = false;

    private float _quality = 0f;

    public void UpdateTransform(Vector3 position, Quaternion rotation, float quality)
    {
        if (quality > _quality)
        {
            if (_useSmooting)
            {
                _transform.localPosition = ((_quality * _transform.localPosition) + (quality * position)) / (_quality + quality);
                _transform.localRotation = Quaternion.Slerp(_transform.localRotation, rotation, quality / (_quality + quality));
                _quality = quality;
            }
            else
            {
                _transform.localPosition = position;
                _transform.localRotation = rotation;
                _quality = quality;
            }
        }
    }

    private void Update()
    {
        _quality *= _qualityDegradation;
    }
}
