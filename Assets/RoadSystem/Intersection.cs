using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[Serializable]
public struct JunctionEdge
{
    public Vector3 left;
    public Vector3 leftOuter;
    public Vector3 right;
    public Vector3 rightOuter;

    public Vector3 Center => (left + right) / 2;

    public JunctionEdge(Vector3 p1, Vector3 p2, Vector3 p1o, Vector3 p2o)
    {
        this.left = p1;
        this.right = p2;
        this.leftOuter = p1o;
        this.rightOuter = p2o;
    }
}

[Serializable]
public struct JunctionInfo
{
    public int splineIndex;
    public int knotIndex;
    public Spline spline;
    public BezierKnot knot;

    public JunctionInfo(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
    {
        this.splineIndex = splineIndex;
        this.knotIndex = knotIndex;
        this.spline = spline;
        this.knot = knot;
    }
}

[Serializable]
public class Intersection
{

    public List<JunctionInfo> junctions;
    public List<JunctionEdge> junctionEdges;
    public List<Vector3> curvePoints;
    public List<Vector3> outerCurvePoints;
    public List<float> innerCurveStrengthList;
    public List<float> outerCurveStrengthList;

    public void AddJunction(int splineIndex, int knotIndex, Spline spline, BezierKnot knot)
    {
        if (junctions == null)
        {
            junctions = new List<JunctionInfo>();
            innerCurveStrengthList = new List<float>();
            outerCurveStrengthList = new List<float>();
        }
        innerCurveStrengthList.Add(0.3f);
        outerCurveStrengthList.Add(0.3f);
        junctions.Add(new JunctionInfo(splineIndex, knotIndex, spline, knot));
    }

    public Vector3 Center
    {
        get
        {
            Vector3 center = new Vector3();
            foreach (JunctionInfo junction in junctions)
            {
                center += (Vector3)junction.knot.Position;
            }
            center = center / junctions.Count;
            return center;
        }
    }

    public IEnumerable<JunctionInfo> GetJunctions()
    {
        return junctions;
    }

    internal void AddJunctionEdge(JunctionEdge junctionEdge)
    {
        if (junctionEdges == null) junctionEdges = new List<JunctionEdge>();

        junctionEdges.Add(junctionEdge);
    }

    internal void ResetJunctionEdges()
    {
        if (junctionEdges == null)
        {
            junctionEdges = new List<JunctionEdge>();
            return;
        } else
        {
            junctionEdges.Clear();
        }
    }
}
