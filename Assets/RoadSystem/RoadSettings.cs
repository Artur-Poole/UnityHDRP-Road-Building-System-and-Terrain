using System;
using UnityEngine;

[Serializable]
public struct RoadSettings
{
    public float RoadDepth;
    public float RoadWidth;
    public float SideWalkHeight;
    public float SideWalkWidth;

    public RoadSettings(float RoadDepth, float RoadWidth, float SideWalkHeight, float SideWalkWidth)
    {
        this.RoadDepth = RoadDepth;
        this.RoadWidth = RoadWidth;
        this.SideWalkHeight = SideWalkHeight;
        this.SideWalkWidth = SideWalkWidth;
    }

}
