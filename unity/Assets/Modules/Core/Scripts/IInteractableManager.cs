using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public interface IInteractableManager : IService
    {
        void UpdateInteractablePose(int id, Vector3 position, Quaternion rotation);
    }
}

