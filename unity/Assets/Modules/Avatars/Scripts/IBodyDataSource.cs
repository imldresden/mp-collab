using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public interface IBodyDataSource
    {
        public Body GetBody(int skeletonId);

        public Body GetClosestBody(Transform transform);

        public string GetJointName(int jointId);

        public int GetJointParent(int jointId);


        public IReadOnlyList<Body> Bodies { get; }

        public int NumOfBodies { get; }

        public Guid SourceId { get; }


    }
}