using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IMLD.MixedReality.Core
{
    public class WorldAnchor : MonoBehaviour, IWorldAnchor
    {
        [SerializeField] private Transform PlayspaceOrigin;

        public Transform GetOrigin()
        {
            if (PlayspaceOrigin == null) { return transform; }
            return PlayspaceOrigin;
        }
    }
}