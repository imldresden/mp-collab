using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public class InteractableObject : MonoBehaviour, IInteractableObject
    {
        public int Id { get; set; }
        public bool IsDragged { get; set; }
        public GameObject Object { get; set; }

        public Pose GetPose()
        {
            return new Pose(transform.position, transform.rotation);
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData) { }

        public void OnPointerDown(MixedRealityPointerEventData eventData) { }

        private BoxCollider _collider;

        void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData e)
        {
            IsDragged = true;
        }

        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            IsDragged = false;
        }

        public void UpdatePose(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            ComputeSnapping();
        }

        // Start is called before the first frame update
        void Start()
        {
            PrepareGameObject();
        }

        // Update is called once per frame
        void Update()
        {
            if (IsDragged)
            {
                ComputeSnapping();
            }
        }

        protected virtual void ComputeSnapping()
        {
            if (_collider != null && ServiceLocator.Instance.TryGet<IInteractableManager>(out var service))
            {
                var manager = service as InteractableManager;
                if (manager != null && manager.SnappingEnabled == true)
                {
                    Collider ClosestCollider;
                    Vector3 ClosestColliderOffset = new Vector3(manager.SnappingDistance, manager.SnappingDistance, manager.SnappingDistance);

                    foreach (var Collider in manager.SnappingTargets)
                    {
                        if (Collider != null)
                        {
                            Vector3 ClosestPoint = _collider.ClosestPoint(Collider.transform.position);
                            Vector3 TargetClosestPoint = Collider.ClosestPoint(ClosestPoint);
                            Vector3 OffsetVector = TargetClosestPoint - ClosestPoint;
                            if (OffsetVector.magnitude < ClosestColliderOffset.magnitude)
                            {
                                ClosestColliderOffset = OffsetVector;
                                ClosestCollider = Collider;
                            }
                        }
                    }

                    if (manager.SnapToInteractables)
                    {
                        foreach (var obj in manager.InteractableParentTransform.GetComponentsInChildren<InteractableObject>())
                        {
                            if (obj != null && obj != this)
                            {
                                var Collider = obj.GetComponent<Collider>();
                                if (Collider != null)
                                {
                                    Vector3 ClosestPoint = _collider.ClosestPoint(Collider.transform.position);
                                    Vector3 TargetClosestPoint = Collider.ClosestPoint(ClosestPoint);
                                    Vector3 OffsetVector = TargetClosestPoint - ClosestPoint;
                                    if (OffsetVector.magnitude < ClosestColliderOffset.magnitude)
                                    {
                                        ClosestColliderOffset = OffsetVector;
                                        ClosestCollider = Collider;
                                    }
                                }
                            }
                        }
                    }
                    

                    if (ClosestColliderOffset.magnitude < manager.SnappingDistance)
                    {
                        Object.transform.localPosition = ClosestColliderOffset;
                    }
                    else
                    {
                        Object.transform.localPosition = Vector3.zero;
                    }
                }
            }
        }

        void PrepareGameObject()
        {
            if (Object == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!Object.GetComponent<MeshRenderer>())
            {
                Object.AddComponent<MeshRenderer>();
            }

            CombineMeshes();

            Mesh Mesh = Object.GetComponent<MeshFilter>().mesh;

            _collider = GetComponent<BoxCollider>();
            if (_collider != null)
            {
                _collider.size = Mesh.bounds.size;
                _collider.center = Mesh.bounds.center;
            }

        }

        void CombineMeshes()
        {
            List<MeshFilter> MeshFilters = new List<MeshFilter>();
            Object.GetComponentsInChildren(MeshFilters);
            var MeshFilter = Object.GetComponent<MeshFilter>();
            if (MeshFilter != null)
            {
                MeshFilters.Add(MeshFilter);
            }
            else
            {
                Object.AddComponent<MeshFilter>();
            }

            if (MeshFilters != null && MeshFilters.Count > 0)
            {
                CombineInstance[] combine = new CombineInstance[MeshFilters.Count];
                int i = 0;
                while (i < MeshFilters.Count)
                {
                    combine[i].mesh = MeshFilters[i].sharedMesh;
                    combine[i].transform = transform.worldToLocalMatrix * MeshFilters[i].transform.localToWorldMatrix;
                    i++;
                }

                Mesh mesh = new Mesh();
                mesh.CombineMeshes(combine);
                Object.GetComponent<MeshFilter>().sharedMesh = mesh;
            }
        }
    }
}