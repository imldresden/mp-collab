using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static IMLD.MixedReality.Avatars.StudyManager.AvatarMap;

namespace IMLD.MixedReality.Avatars
{
    public interface IStudyManager
    {
        event EventHandler<AvatarEventArgs> AvatarTypeChanged;
        AvatarType AvatarType { get; set; }
        AbstractAvatar CreateAvatar(AvatarType type, Transform parent, int roomId);
        AbstractAvatar CreateAvatarFromMenu(int avatarIndex, Transform transform, int roomId);
        AvatarMapEntry[] GetAvatars();

    }

    public enum AvatarType
    {
        MESH,
        SIMPLE_MESH,
        POINTCLOUD,
        VALID,
        RPM,
        NONE
    }

    public class AvatarEventArgs
    {
        public AvatarType AvatarType;
    }
}