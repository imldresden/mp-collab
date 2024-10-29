using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Core
{
    public interface IWorldAnchor
    {
        public Transform GetOrigin();
    }
}