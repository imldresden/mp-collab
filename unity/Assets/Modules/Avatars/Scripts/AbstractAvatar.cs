using IMLD.MixedReality.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Avatars
{
    public abstract class AbstractAvatar : MonoBehaviour, IAvatar
    {
        public User User { get; set; }

        public abstract void ApplyHandPosture(HandDataFrame leftHand, HandDataFrame rightHand);

        public int AvatarId { get; set; }
    }
}