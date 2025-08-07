using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;




/// <summary>
/// SplineSampler script to do xxx
/// 
/// 
/// 
/// Developed by Game Dev Guide
/// Edited by AP
/// </summary>

[ExecuteInEditMode()]
public class SpineSampler : MonoBehaviour
{
    [SerializeField] GameObject markerPrefab;


    [SerializeField] private SplineContainer m_splineContainer;

    [SerializeField] private int m_splineIndex;

    [SerializeField] [Range(0f, 1f)] private float m_time;

    float3 position;
    float3 tangent;
    float3 upVector;
    float3 p1;
    float3 p2;

    public int NumSplines { get; internal set; }

    private void Awake()
    {
        NumSplines = m_splineContainer.Splines.Count;
    }

    public Spline GetSplines(int index)
    {
        return m_splineContainer.Splines[index];
    }

    private void Start()
    {
        //markers.Clear();
    }

    private void Update()
    {
        if (NumSplines != m_splineContainer.Splines.Count)
        {
            NumSplines = m_splineContainer.Splines.Count;
        }

    }

    private void OnDrawGizmos()
    {
        Handles.matrix = transform.localToWorldMatrix;
        Handles.SphereHandleCap(0, position, Quaternion.identity, 1f, EventType.Repaint);
        //JunctionBuildOverlay.OnSelectionChanged();
    }

    internal void SampleSplineWidth(int splineIndex, float t, float m_width, out Vector3 p1, out Vector3 p2)
    {
        m_splineContainer.Evaluate(splineIndex, t, out position, out tangent, out upVector);
        //Debug.Log(position);

        float3 right = Vector3.Cross(tangent, upVector).normalized;
        p1 = position + (right * m_width);
        p2 = position - (right * m_width);
    }
}
