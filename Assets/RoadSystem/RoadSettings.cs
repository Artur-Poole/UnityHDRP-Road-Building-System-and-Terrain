using System;
using UnityEngine;

[Serializable]
public struct RoadSettings
{
    public float RoadDepth;
    public float RoadWidth;
    public bool HasSidewalk;
    public float SideWalkHeight;
    public float SideWalkWidth;

    public RoadSettings(float RoadDepth, float RoadWidth, bool HasSidewalk, float SideWalkHeight, float SideWalkWidth)
    {
        this.RoadDepth = RoadDepth;
        this.RoadWidth = RoadWidth;
        this.HasSidewalk = HasSidewalk;
        this.SideWalkHeight = SideWalkHeight;
        this.SideWalkWidth = SideWalkWidth;
    }

}
