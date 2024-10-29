using IMLD.MixedReality.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public interface IAvatar
    {
        public User User {get;}
        public void ApplyHandPosture(HandDataFrame leftHand, HandDataFrame rightHand);
    }
}