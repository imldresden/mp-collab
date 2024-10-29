using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public interface IInteractableObject : IMixedRealityPointerHandler
    {
        public void UpdatePose(Vector3 position, Quaternion rotation);
        public Pose GetPose();
        public int Id { get; set; }
        public bool IsDragged { get; set; }
        public GameObject Object { get; set; }
    }
}