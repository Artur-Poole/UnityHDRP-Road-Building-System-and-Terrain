using System;
using UnityEngine;
using UnityEditor.Splines;
using UnityEngine.Splines;
using NUnit.Framework;
using System.Collections.Generic;


public struct SelectedSplineElementInfo
{
    public System.Object target;
    public int targetIndex;
    public int knotIndex;

    public SelectedSplineElementInfo(System.Object obj, int index, int knot)
    {
        target = obj;
        targetIndex = index;
        knotIndex = knot;
    }
}

[Serializable]
struct SelectableSplineElement : IEquatable<SelectableSplineElement>
{
    public System.Object target;
    public int targetIndex;
    public int knotIndex;
    public int tangentIndex;

    public SelectableSplineElement(ISelectableElement element)
    {
        target = element.SplineInfo.Object;
        targetIndex = element.SplineInfo.Index;
        knotIndex = element.KnotIndex;
        tangentIndex = element is SelectableTangent tangent ? tangent.TangentIndex : -1;
    }

    public bool Equals(SelectableSplineElement other)
    {
        return target == other.target && targetIndex == other.targetIndex && knotIndex == other.knotIndex && tangentIndex == other.tangentIndex;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is SelectableSplineElement other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(target, targetIndex, knotIndex, tangentIndex);
    }
}

public static class SplineToolUtility
{

    


    public static bool HasSelection()
    {
        return SplineSelection.HasActiveSplineSelection();
    }

    public static List<SelectedSplineElementInfo> GetSelection()
    {

        // Get internal struct data
        //List<SelectableSplineElement> elements = SplineSelection.selection;
        //List<SelectableSplineElement<SplineKnot>> elements = new List<SelectableSplineElement>(SplineSelection.selection);
        List<UnityEditor.Splines.SelectableSplineElement> elements = SplineSelection.selection;

        // make new publilc struct data
        List<SelectedSplineElementInfo> infos = new List<SelectedSplineElementInfo>();

        foreach (UnityEditor.Splines.SelectableSplineElement element in elements)
        {
            infos.Add(new SelectedSplineElementInfo(element.target, element.targetIndex, element.knotIndex));
        }

        return infos;
    }


}
