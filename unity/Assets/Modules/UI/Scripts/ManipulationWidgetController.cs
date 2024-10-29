using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.UI
{
    public class ManipulationWidgetController : MonoBehaviour
    {
        public GameObject GhostObject;
        public Transform TargetTransform;

        private LineRenderer _lineRenderer;

        // Start is called before the first frame update
        void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            foreach (var translationControl in GetComponentsInChildren<GrabTranslator>())
            {
                translationControl.TargetTransform = TargetTransform;
                translationControl.GhostObjectTransform = GhostObject.transform;
            }

            foreach (var rotationControl in GetComponentsInChildren<GrabRotator>())
            {
                rotationControl.TargetTransform = TargetTransform;
                rotationControl.GhostObjectTransform = GhostObject.transform;
            }
        }


        void LateUpdate()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.SetPositions(new Vector3[] { transform.position, TargetTransform.position });
            }
        }

        private void OnEnable()
        {
            // place widget correctly
            transform.position = CameraCache.Main.transform.position + CameraCache.Main.transform.forward;
        }
    }
}