using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IMLD.MixedReality.Avatars
{
    public interface IPointCloudSource
    {
        public PointCloudDataFrame PointCloud { get; }
        bool RenderPointClouds { get; set; }

        public Guid SourceId { get; }
    }
}