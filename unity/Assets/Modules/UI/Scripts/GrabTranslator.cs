using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.UI
{
    public class GrabTranslator : MonoBehaviour, IMixedRealityPointerHandler, IMixedRealityFocusHandler
    {
        public Transform TargetTransform;
        public Transform GhostObjectTransform;
        public Material FocusMaterial;
        public Material StandardMaterial;
        public ManipulationType Manipulation;

        private IMixedRealityPointer _pointer;
        private Vector3 _previousPosition;
        private Vector3 _translationAxis;

        private MeshRenderer _meshRenderer;

        void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            //throw new System.NotImplementedException();
            //PointerHandler
        }

        void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
        {
            _pointer = eventData.Pointer;
            _previousPosition = _pointer.Position;
            switch (Manipulation)
            {
                case ManipulationType.TransX:
                    _translationAxis = transform.right;
                    break;
                case ManipulationType.TransY:
                    _translationAxis = transform.up;
                    break;
                case ManipulationType.TransZ:
                    _translationAxis = transform.forward;
                    break;
            }
        }

        void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (_pointer == null || TargetTransform == null)
            {
                return;
            }

            // compute translation vector between starting point and current point
            Vector3 currentVector = _pointer.Position - _previousPosition;
            Vector3 translationAxis = _translationAxis;
            Vector3 translationVector = Vector3.Project(currentVector, translationAxis);

            TargetTransform.Translate(translationVector, Space.World);
            GhostObjectTransform.Translate(translationVector, Space.World);

            _previousPosition = _pointer.Position;
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
            TransX, TransY, TransZ
        }
    }
}