using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace IMLD.MixedReality.Core
{
    public class InteractableManager : MonoBehaviour, IInteractableManager
    {
        public InteractableObject InteractableObjectPrefab;
        public List<Collider> SnappingTargets;
        public Material furnitureMaterial;
        public bool SnappingEnabled;
        public bool SnapToInteractables;
        public float SnappingDistance;
        public Transform InteractableParentTransform;
        public List<InteractableObject> InteractableObjects;

        public IReadOnlyList<Type> Dependencies => throw new NotImplementedException();

        public void UpdateInteractablePose(int id, Vector3 position, Quaternion rotation)
        {
            if (InteractableObjects != null && id < InteractableObjects.Count)
            {
                var Pose = Conversion.GetAbsolutePose(position, rotation, InteractableParentTransform.position, InteractableParentTransform.rotation, InteractableParentTransform.localScale);
                InteractableObjects[id]?.UpdatePose(Pose.position, Pose.rotation);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (InteractableParentTransform == null)
            {
                if (ServiceLocator.Instance.TryGet<IWorldAnchor>(out var WorldAnchor))
                {
                    InteractableParentTransform = WorldAnchor.GetOrigin();
                }
                else
                {
                    InteractableParentTransform = transform;
                }
            }

            // Debug.Log("How many"+ origin.GetComponentsInChildren<InteractableFurniture>().Length);

            // get list of objects to turn into interactables
            List<Transform> ChildrenObjects = new List<Transform>();
            foreach (Transform child in InteractableParentTransform)
            {
                ChildrenObjects.Add(child);
            }

            InteractableObjects = new List<InteractableObject>();

            for (int i = 0; i < ChildrenObjects.Count; i++)
            {
                var child = ChildrenObjects[i];
                var InteractableObject = Instantiate(InteractableObjectPrefab, InteractableParentTransform);
                InteractableObject.Object = child.gameObject;
                InteractableObject.transform.position = child.position;
                InteractableObject.transform.rotation = child.rotation;
                child.SetParent(InteractableObject.transform);
                InteractableObject.Id = i;
                InteractableObjects.Add(InteractableObject);
            }

        }

        private void Update()
        {
            //send furniture message here
            // if any of the furnitures are being moved?

            if (ServiceLocator.Instance.TryGet<ISessionManager>(out var SessionManager))
            {
                foreach (IInteractableObject obj in InteractableObjects)
                {
                    if (obj.IsDragged == true)
                    {
                        Pose objPose = Conversion.GetRelativePose(obj.GetPose(), InteractableParentTransform);

                        SessionManager.UpdateInteractableObjectPose(obj.Id, objPose);
                    }
                }
            }
        }
    }
}