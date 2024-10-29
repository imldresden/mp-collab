using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.UI
{
    public class GrabRotator : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusHandler
    {


        public Transform TargetTransform;
        public Transform GhostObjectTransform;
        public Material FocusMaterial;
        public Material StandardMaterial;
        public ManipulationType Manipulation;

        private IMixedRealityPointer _pointer;
        private Vector3 _previousVector;
        private Vector3 _planeNormal;
        private MeshRenderer _meshRenderer;

        void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            //throw new System.NotImplementedException();
        }

        void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
        {
            _pointer = eventData.Pointer;
            _previousVector = _pointer.Position - transform.position;
            switch (Manipulation)
            {
                case ManipulationType.RotX:
                    //_planeNormal = Vector3.right;
                    _planeNormal = transform.up;
                    break;
                case ManipulationType.RotY:
                    //_planeNormal = Vector3.up;
                    _planeNormal = transform.up;
                    break;
                case ManipulationType.RotZ:
                    //_planeNormal = Vector3.forward;
                    _planeNormal = transform.up;
                    break;
            }
        }

        void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            // compute rotation angle between starting point and current point
            Vector3 currentVector = _pointer.Position - transform.position;

            Vector3 planeNormal = _planeNormal;
        
            _previousVector = Vector3.ProjectOnPlane(_previousVector, planeNormal);
            currentVector = Vector3.ProjectOnPlane(currentVector, planeNormal);
        
            float angle = Vector3.SignedAngle(currentVector, _previousVector, planeNormal);
            float scaledAngle = -0.25f * angle;
            Quaternion currentRotation = Quaternion.AngleAxis(angle, planeNormal);
        
            //Quaternion currentRotation = Quaternion.FromToRotation(_previousVector, currentVector);
        
            Debug.Log("Start: " + _previousVector + ", Current: " + currentVector);
            Debug.Log(currentRotation.eulerAngles.ToString());
            if (TargetTransform != null)
            {
                transform.RotateAround(transform.position, planeNormal, scaledAngle);
                TargetTransform.RotateAround(transform.position, planeNormal, scaledAngle);
                GhostObjectTransform.RotateAround(transform.position, planeNormal, scaledAngle);
            }
            _previousVector = currentVector;
        }

        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            _pointer = null;
        }

        // Start is called before the first frame update
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        
            if (StandardMaterial == null)
            {
                StandardMaterial = _meshRenderer.material;
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        void IMixedRealityFocusHandler.OnFocusEnter(FocusEventData eventData)
        {
            if (_meshRenderer != null && FocusMaterial != null)
            {
                _meshRenderer.material = FocusMaterial;
            }
        }

        void IMixedRealityFocusHandler.OnFocusExit(FocusEventData eventData)
        {
            if (_meshRenderer != null && StandardMaterial != null)
            {
                _meshRenderer.material = StandardMaterial;
            }
        }

        public enum ManipulationType
        {
            RotX, RotY, RotZ
        }
    }
}