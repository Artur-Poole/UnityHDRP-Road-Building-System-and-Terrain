using NUnit.Framework;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using UnityEngine.UIElements;

[ExecuteInEditMode()]

public class AdvancedSplineRoad : MonoBehaviour
{
    [Header("Right-click the component header to Rebuild Road")]

    [Header("Debug Settings")]
    [SerializeField] private bool drawMeshNodes = false;
    [SerializeField] private bool drawMeshJunctionNodes = false;


    [Header("Spline Info")]
    [SerializeField] SpineSampler m_splineSampler;
    [SerializeField] MeshFilter m_meshFilter;

    [Header("Road Settings")]
    [SerializeField] List<RoadSettings> m_roadSettings;
    [SerializeField][UnityEngine.Range(0f, 5f)] private float m_width;
    [SerializeField] int resolution;
    [SerializeField] float pointsPerMeter = 1f;
    [SerializeField] float m_RoadDepth = 1f;
    [Header("Sidewalk Settings")]
    [SerializeField][UnityEngine.Range(0f, 3f)] private float SidewalkHeight;
    [SerializeField][UnityEngine.Range(0f, 3f)] private float SidewalkTotalWidth;


    [Header("Intersections & Junctions")]
    [SerializeField] float m_curveStep;
    public List<Intersection> intersections;
    public List<JunctionEdge> junctionEdges;

    List<Vector3> m_vertsP1;
    List<Vector3> m_vertsP2;
    List<Vector3> m_vertsO1;
    List<Vector3> m_vertsO2;
    List<int> _samplesPerSpline;

    [ContextMenu("Rebuild Road")]
    private void RebuildRoad_ContextMenu()
    {
        Rebuild();
    }

    private void OnDrawGizmos()
    {
        if (m_vertsP1 != null && drawMeshNodes)
        {
            for (int i = 0; i < m_vertsP1.Count; i++)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.TransformPoint(m_vertsP1[i]), 0.25f);
                Gizmos.DrawSphere(transform.TransformPoint(m_vertsO1[i]), 0.5f);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.TransformPoint(m_vertsP2[i]), 0.25f);
                Gizmos.DrawSphere(transform.TransformPoint(m_vertsO2[i]), 0.5f);
            }
            
            if (junctionEdges != null && drawMeshJunctionNodes)
            {
                foreach (JunctionEdge edge in junctionEdges)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(edge.right, 1f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(edge.left, 1f);
                }
            }
        }

        if (intersections != null && drawMeshJunctionNodes)
        {
            for (int i = 0; i < intersections.Count; i++)
            {
                //Debug.Log("yo?");

                if (intersections[i].curvePoints != null)
                {
                    for (int j = 0; j <  intersections[i].curvePoints.Count; j++)
                    {
                        //Debug.Log("Test");
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawSphere(transform.TransformPoint(intersections[i].curvePoints[j]), 0.25f);

                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(transform.TransformPoint(intersections[i].outerCurvePoints[j]), 0.25f);
                    }
                }
            }
        }
    }

    private void GetVerts()
    {
        //Debug.Log("m_splineSampler.NumSplines: " + m_splineSampler.NumSplines);


        GetScaledVerts();
        


    }

    private void GetScaledVerts()
    {
        m_vertsP1 = new List<Vector3>();
        m_vertsP2 = new List<Vector3>();
        m_vertsO1 = new List<Vector3>();
        m_vertsO2 = new List<Vector3>();

        float step = 1f / (float)resolution;
        Vector3 p1;
        Vector3 p2;
        if (_samplesPerSpline == null)
        {
            _samplesPerSpline = new List<int> ();
        } 
        else
        {
            _samplesPerSpline.Clear();
        }

        for (int j = 0; j < m_splineSampler.NumSplines; j++)
        {

            float i_roadWidth;
            float i_roadDepth;
            float i_sideWidth;
            float i_sideHeight;
            // get road data....
            if (j < m_roadSettings.Count)
            {
                i_roadWidth = m_roadSettings[j].RoadWidth;
                i_roadDepth = m_roadSettings[j].RoadDepth;
                i_sideHeight = m_roadSettings[j].SideWalkHeight;
                i_sideWidth = m_roadSettings[j].SideWalkWidth;
            }
            else
            {
                i_roadWidth = m_width;
                i_roadDepth = m_RoadDepth;
                i_sideHeight = SidewalkHeight;
                i_sideWidth = SidewalkTotalWidth;
                m_roadSettings.Add(new RoadSettings(i_roadDepth, i_roadWidth, i_sideHeight, i_sideWidth));
            }



                // 1) grab the Spline object for this index (you might need to adjust to your API)
                Spline spline = m_splineSampler.GetSplines(j);

            // 2) measure its length in world-space
            float length = SplineUtility.CalculateLength(spline, transform.localToWorldMatrix);

            // 3) compute how many samples we need
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(length * pointsPerMeter));
            _samplesPerSpline.Add(sampleCount);

            // 4) step t from 0 to 1 inclusive
            float deltaT = 1f / (sampleCount - 1);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = deltaT * i;
                m_splineSampler.SampleSplineWidth(j, t, m_roadSettings[j].RoadWidth, out p1, out p2);
                m_vertsP1.Add(transform.InverseTransformPoint(p1)); // say left side...
                m_vertsP2.Add(transform.InverseTransformPoint(p2)); // say right side...

                m_splineSampler.SampleSplineWidth(j, t, m_roadSettings[j].RoadWidth + m_roadSettings[j].SideWalkWidth, out p1, out p2);
                m_vertsO1.Add(transform.InverseTransformPoint(p1)); // say left side...
                m_vertsO2.Add(transform.InverseTransformPoint(p2)); // say right side...
            }
        }
    }

    /// <summary>
    /// Master BuildMesh function... 
    /// </summary>
    private void BuildMesh()
    {
        Mesh m = new Mesh();
        junctionEdges = new List<JunctionEdge>();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<int> trisSidewalk = new List<int>();

        List<Vector2> uvs = new List<Vector2>();

        int offset = 0;
        float uvOffset = 0f;

        GenerateNormalRoadsVertsTrisUVs(offset, uvOffset, verts, trisSidewalk, tris, uvs);

        offset = verts.Count;
        Vector3 center = new Vector3();
        List<int> trisB = new List<int>();

        GenerateIntersectionsVertsTrisUVs(center, verts, trisB, uvs);

        int numVerts = verts.Count;

        m.subMeshCount = 3;

        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();

        m.SetTriangles(trisB, 1);
        m.RecalculateNormals();

        m.SetTriangles(trisSidewalk, 2);
        m.RecalculateNormals();


        m.SetUVs(0, uvs);

        //m.SetVertices(verts);
        m.RecalculateBounds();

        m_meshFilter.sharedMesh = m;
    }

    private void GenerateNormalRoadsVertsTrisUVs(int offset, float uvOffset, List<Vector3> verts, List<int> sidewalkTris, List<int> tris, List<Vector2> uvs)
    {
        
        int splineIndex = 0;

        for (int j = 0; j < m_splineSampler.NumSplines; j++)
        {
            int count = _samplesPerSpline[j];      // how many samples this spline got
            int segments = count - 1;            // how many quads we'll build

            // Iterate through knots of spline...
            for (int i = 1; i < count; i++)
            {

                // get left and right previous and current
                int vi = splineIndex + i;
                Vector3 p1 = m_vertsP1[vi - 1];
                Vector3 p2 = m_vertsP2[vi - 1];
                Vector3 p3 = m_vertsP1[vi];
                Vector3 p4 = m_vertsP2[vi];

                // anchor on vert count for Triangles Indexing
                int vBase = verts.Count;

                verts.AddRange(new[] { p1, p2, p3, p4 });

                tris.AddRange(new[]
                {
                    vBase + 0,
                    vBase + 2,
                    vBase + 3,

                    vBase + 3,
                    vBase + 1,
                    vBase + 0
                });

                // UV along the length of each segment -- UV scale is off
                float segmentLen = Vector3.Distance(p1, p3) / 4f;
                float uvNext = uvOffset + segmentLen;
                uvs.AddRange(new[]
                {
                    new Vector2(uvOffset, 0),
                    new Vector2(uvOffset, 1),
                    new Vector2(uvNext,   0),
                    new Vector2(uvNext,   1)
                });

                uvOffset = uvNext;

                vBase = verts.Count;

                verts.AddRange(new[] { p1 + Vector3.down * m_RoadDepth, p2 + Vector3.down * m_RoadDepth, p3 + Vector3.down * m_RoadDepth, p4 + Vector3.down * m_RoadDepth });

                sidewalkTris.AddRange(new[]
                {
                    vBase + 0,
                    vBase + 3,
                    vBase + 2,

                    vBase + 3,
                    vBase + 0,
                    vBase + 1
                });

                uvs.AddRange(new[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1,   0),
                    new Vector2(1,   1)
                });

                BuildSidewalk(verts, sidewalkTris, uvs, uvOffset,
                     m_vertsP1[vi], m_vertsP1[vi - 1],   // inner L next, prev
                     m_vertsO1[vi], m_vertsO1[vi - 1]
                    );
                BuildSidewalk(verts, sidewalkTris, uvs, uvOffset,
                    m_vertsP2[vi - 1], m_vertsP2[vi],   // inner R prev, next
                    m_vertsO2[vi - 1], m_vertsO2[vi]);  // outer R prev, next


                BuildUnderRoad(offset, uvOffset, verts, sidewalkTris, tris, uvs,
                      m_vertsP1[vi], m_vertsP1[vi - 1],   // inner L next, prev
                     m_vertsO1[vi], m_vertsO1[vi - 1]);

                BuildUnderRoad(offset, uvOffset, verts, sidewalkTris, tris, uvs,
                    m_vertsP2[vi - 1], m_vertsP2[vi],   // inner R prev, next
                    m_vertsO2[vi - 1], m_vertsO2[vi]);  // outer R prev, next


            }

            int startIdx = splineIndex;          // first sample of this spline
            int endIdx = splineIndex + count - 1;  // last sample

            Vector3 p1s = m_vertsP1[startIdx];
            Vector3 p2s = m_vertsP2[startIdx];
            Vector3 p1e = m_vertsP1[endIdx];
            Vector3 p2e = m_vertsP2[endIdx];

            // for the start cap, direction from start-left->start-right
            Vector3 forwardS = -1 * (p2s - p1s).normalized;
            Vector3 leftDirS = Vector3.Cross(Vector3.up, forwardS).normalized;
            Vector3 rightDirS = -leftDirS;

            // for the end cap, direction from end-left->end-right
            Vector3 forwardE = (p2e - p1e).normalized;
            Vector3 leftDirE = Vector3.Cross(Vector3.up, forwardE).normalized;
            Vector3 rightDirE = -leftDirE;

            //// START sidewalk caps
            BuildRoadCap(p1s, p1s + forwardS * SidewalkTotalWidth, uvOffset, verts, sidewalkTris, uvs);  // left sidewalk start
            BuildRoadCap(p2s - forwardS * SidewalkTotalWidth, p2s, uvOffset, verts, sidewalkTris, uvs);  // right sidewalk start

            //// END sidewalk caps
            BuildRoadCap(p1e + forwardS * SidewalkTotalWidth, p1e, uvOffset, verts, sidewalkTris, uvs);  // left sidewalk end
            BuildRoadCap(p2e, p2e - forwardS * SidewalkTotalWidth, uvOffset, verts, sidewalkTris, uvs);  // right sidewalk end

            // START BIG CAP
            BuildBigCap(verts, sidewalkTris, uvs, m_vertsO1[startIdx], m_vertsO2[startIdx] );
            // END BIG CAP
            BuildBigCap(verts, sidewalkTris, uvs, m_vertsO2[endIdx], m_vertsO1[endIdx] );
            splineIndex += count;
        }
    }

    private void BuildBigCap(List<Vector3> verts, List<int> sidewalkTris, List<Vector2> uvs, Vector3 leftInitial, Vector3 rightInitial)
    {
        int b = verts.Count;

        float segLen = Vector3.Distance(leftInitial, rightInitial);

        verts.AddRange(new[]{
            leftInitial,
            leftInitial + Vector3.down * m_RoadDepth,
            rightInitial + Vector3.down * m_RoadDepth,
            rightInitial
        });

        sidewalkTris.AddRange(new[]{
            b + 1, b + 0, b + 3,
            b + 3, b + 2, b +1
        });

        uvs.AddRange(new[]{
            //new Vector2(0, 0f), // v0
            //new Vector2(0, 1f), // v1
            //new Vector2(1, 0f), // v2
            //new Vector2(1, 1f)  // v3
            /* left-top,  left-bottom */  new Vector2(0, 0),  new Vector2(0, 1),
            /* right-bottom, right-top */ new Vector2(segLen,1),  new Vector2(segLen,0)
        });
    }

    // REVISION NEEDED -- Requires independent Verts for UV faces otherwise get mixed UV's along different faces
    // Reason that side faces of roads have a gradient from the material to complete blackness... side face shares UV normal
    private void BuildUnderRoad(int offset, float uvOffset, List<Vector3> verts, List<int> sidewalkTris, List<int> tris, List<Vector2> uvs, Vector3 i0, Vector3 i1, Vector3 o0, Vector3 o1)
    {
        float depth = m_RoadDepth;
        float segLen = Vector3.Distance(i0, i1);

        int b = verts.Count;

        verts.AddRange(new[]{
            i0,            i0 + Vector3.down*depth,
            o0 + Vector3.down*depth, o0,
            i1,            i1 + Vector3.down*depth,
            o1 + Vector3.down*depth, o1
        });

        // two quads per wall, one quad top
        sidewalkTris.AddRange(new[]{
            // inner wall
            //b + 4, b + 0, b + 1,  b + 4, b + 1, b + 5,
            // outer wall
            b + 3, b + 7, b + 2,  b + 7, b + 6, b + 2,
            // sidewalk deck
            b + 1, b + 2, b + 5,  b + 2, b + 6, b + 5
        });

        // simple UVs (stretch-along-length)
        uvs.AddRange(new[]{
            // inner wall
            new Vector2(uvOffset,0), new Vector2(uvOffset,1),
            new Vector2(uvOffset,1), new Vector2(uvOffset,0),

            // next ring (same order)
            new Vector2(uvOffset+segLen,0), new Vector2(uvOffset+segLen,1),
            new Vector2(uvOffset+segLen,1), new Vector2(uvOffset+segLen,0)
        });

        verts.AddRange(new[] { i0 + Vector3.down * depth, o0 + Vector3.down * depth, i1 + Vector3.down * depth, o1 + Vector3.down * depth });   // 4 duplicates
        int d = b + 8;  // index of the first duplicate

        sidewalkTris.AddRange(new[]{
            //d+0, d+2, d+1,   // deck quad
            //d+1, d+2, d+3
            // deck quad (reversed winding)
            d+0, d+1, d+2,
            d+1, d+3, d+2
        });

        uvs.AddRange(new[]{
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(0,1), new Vector2(1,1)
        });
    }

    void BuildSidewalk(List<Vector3> verts, List<int> tris, List<Vector2> uvs, float uvOffset, Vector3 i0, Vector3 i1, Vector3 o0, Vector3 o1)
    {

        float h = SidewalkHeight;
        float segLen = Vector3.Distance(i0, i1) / 4f;
        //float segLen2 = Vector3.Distance(i0, i1) / 4f;

        /* layout
           i0b i0t  o0t o0b
           i1b i1t  o1t o1b
        */
        int b = verts.Count;

        verts.AddRange(new[]{
            i0,            i0 + Vector3.up*h,
            o0 + Vector3.up*h, o0,
            i1,            i1 + Vector3.up*h,
            o1 + Vector3.up*h, o1
        });

        // two quads per wall, one quad top
        tris.AddRange(new[]{
            b+0,b+4,b+1,  b+1,b+4,b+5,        // inner wall
            b+3,b+2,b+7,  b+7,b+2,b+6,        // outer wall
            //b+1,b+5,b+2,  b+2,b+5,b+6         // sidewalk deck
        });

        // simple UVs (stretch-along-length) -- Revise UV not scaled properly
        uvs.AddRange(new[]{

            // inner ring
            new Vector2(uvOffset,0),
            new Vector2(uvOffset,1),
            new Vector2(uvOffset,0),
            new Vector2(uvOffset,1),



            //// next ring (same order)
            new Vector2(uvOffset+segLen*2,0),
            new Vector2(uvOffset+segLen*2,1),
            new Vector2(uvOffset+segLen*2,0),
            new Vector2(uvOffset+segLen*2,1)
        });


        verts.AddRange(new[] { i0 + Vector3.up * h, o0 + Vector3.up * h, i1 + Vector3.up * h, o1 + Vector3.up * h });   // 4 duplicates
        int d = b + 8;  // index of the first duplicate

        tris.AddRange(new[]{
            d+0, d+2, d+1,   // deck quad
            d+1, d+2, d+3
        });

        uvs.AddRange(new[]{
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(0,1),
            new Vector2(1,1)
        });
    }

    void BuildRoadCap(Vector3 p1, Vector3 p2, float uvOffset, List<Vector3> verts, List<int> tris, List<Vector2> uvs)
    {
        float h = SidewalkHeight;

        float segmentLen = Vector3.Distance(p1, p2);

        // four verts (base left, base right, top right, top left)
        Vector3 v0 = p1;
        Vector3 v1 = p2;
        Vector3 v2 = p2 + Vector3.up * h;
        Vector3 v3 = p1 + Vector3.up * h;

        int b = verts.Count;
        verts.AddRange(new[] { v0, v1, v2, v3 });

        // single quad: (0,1,2)  (2,3,0) wound so normal points outward
        tris.AddRange(new[]{
          b+0, b+1, b+2,
          b+2, b+3, b+0
        });

        uvs.AddRange(new[]{
            new Vector2(uvOffset,           0f), // v0
            new Vector2(uvOffset + segmentLen, 0f), // v1
            new Vector2(uvOffset + segmentLen, 1f), // v2
            new Vector2(uvOffset,           1f)  // v3
        });

        uvOffset += segmentLen;
    }


    private void GenerateIntersectionsVertsTrisUVs(Vector3 center, List<Vector3> verts, List<int> trisB, List<Vector2> uvs)
    {
        for (int i = 0; i < intersections.Count; i++)
        {
            Intersection intersection = intersections[i];

            if (intersection == null) break;
            if (intersection.GetJunctions() == null) break;
            intersection.ResetJunctionEdges();
            center = new Vector3();

            foreach (JunctionInfo junction in intersection.GetJunctions())
            {
                int splineIndex = junction.splineIndex;
                float t = junction.knotIndex == 0 ? 0f : 1f;

                m_splineSampler.SampleSplineWidth(splineIndex, t, m_roadSettings[splineIndex].RoadWidth, out Vector3 p1, out Vector3 p2);
                m_splineSampler.SampleSplineWidth(splineIndex, t, m_roadSettings[splineIndex].RoadWidth + m_roadSettings[splineIndex].SideWalkWidth, out Vector3 p1o, out Vector3 p2o);
                if (junction.knotIndex == 0)
                {
                    intersection.AddJunctionEdge(new JunctionEdge(p2, p1, p2o, p1o));
                }
                else
                {
                    intersection.AddJunctionEdge(new JunctionEdge(p1, p2, p1o, p2o));
                }

                center += p1;
                center += p2;
            }

            List<JunctionEdge> juncEdges = intersection.junctionEdges;
            List<Vector3> points = new List<Vector3>();
            center = center / (juncEdges.Count * 2);

            juncEdges.Sort((x, y) => SortPoints(center, x.Center, y.Center));

            List<Vector3> curvePoints = new List<Vector3>();
            List<Vector3> curveOuterPoints = new List<Vector3>();
            // add additional points
            Vector3 mid;
            Vector3 c;
            Vector3 b;
            Vector3 a;
            BezierCurve curve;

            for (int j = 1; j <= juncEdges.Count; j++)
            {
                a = transform.InverseTransformPoint(juncEdges[j - 1].right);
                curvePoints.Add(a);
                b = (j < juncEdges.Count) ? transform.InverseTransformPoint(juncEdges[j].left) : transform.InverseTransformPoint(juncEdges[0].left);
                mid = Vector3.Lerp(a, b, 0.5f);
                Vector3 dir = transform.InverseTransformPoint(center) - mid;
                mid = mid - dir;
                c = Vector3.Lerp(mid, transform.InverseTransformPoint(center), intersection.innerCurveStrengthList[j - 1]);

                curve = new BezierCurve(a, c, b);
                for (float localTime = 0f; localTime < 1f; localTime += m_curveStep)
                {
                    Vector3 pos = CurveUtility.EvaluatePosition(curve, localTime);
                    curvePoints.Add(pos);
                }

                curvePoints.Add(b);



                a = transform.InverseTransformPoint(juncEdges[j - 1].rightOuter);
                curveOuterPoints.Add(a);
                b = (j < juncEdges.Count) ? transform.InverseTransformPoint(juncEdges[j].leftOuter) : transform.InverseTransformPoint(juncEdges[0].leftOuter);
                mid = Vector3.Lerp(a, b, 0.5f);
                dir = transform.InverseTransformPoint(center) - mid;
                mid = mid - dir;
                c = Vector3.Lerp(mid, transform.InverseTransformPoint(center), intersection.outerCurveStrengthList[j - 1]);

                curve = new BezierCurve(a, c, b);
                for (float localTime = 0f; localTime < 1f; localTime += m_curveStep)
                {
                    Vector3 pos = CurveUtility.EvaluatePosition(curve, localTime);
                    curveOuterPoints.Add(pos);
                }

                curveOuterPoints.Add(b);
            }

            curvePoints.Reverse();
            curveOuterPoints.Reverse();

            intersection.curvePoints = curvePoints;
            intersection.outerCurvePoints = curveOuterPoints;

            int pointsOffset = verts.Count;
            for (int j = 0; j < curvePoints.Count; j++)
            {
                // ---------- pick edge verts ----------
                Vector3 vertA = curvePoints[j];
                Vector3 vertB = (j == curvePoints.Count - 1) ? curvePoints[0] : curvePoints[j + 1];

                /* ---------- TOP (faces up) ---------- */
                int topStart = verts.Count;                    // first of this trio
                verts.Add(transform.InverseTransformPoint(center)); // center-top
                verts.Add(vertA);                                   // edge A-top
                verts.Add(vertB);                                   // edge B-top

                trisB.Add(topStart);           // (0,1,2) winding = upward
                trisB.Add(topStart + 1);
                trisB.Add(topStart + 2);

                uvs.Add(new Vector2(center.z, center.x));
                uvs.Add(new Vector2(vertA.z, vertA.x));
                uvs.Add(new Vector2(vertB.z, vertB.x));

                /* ---------- BOTTOM (faces down) ---------- */
                int bottomStart = verts.Count;                 // first of new trio
                verts.Add(transform.InverseTransformPoint(center) - Vector3.up * m_RoadDepth);
                verts.Add(vertA - Vector3.up * m_RoadDepth);
                verts.Add(vertB - Vector3.up * m_RoadDepth);

                // reverse order so the normal points downward
                trisB.Add(bottomStart);
                trisB.Add(bottomStart + 2);
                trisB.Add(bottomStart + 1);

                uvs.Add(new Vector2(center.z, center.x));
                uvs.Add(new Vector2(vertA.z, vertA.x));
                uvs.Add(new Vector2(vertB.z, vertB.x));
            }

            for (int j = 1; j <= curvePoints.Count / 2; j++)
            {
                Vector3 pointA = curvePoints[j - 1];
                Vector3 pointB = curvePoints[j];
                Vector3 pointAo = curveOuterPoints[j - 1];
                Vector3 pointBo = curveOuterPoints[j];

                // check our junction poitns

                if (DoVectorsCrossRoad(pointA, pointB) == false && DoVectorsCrossRoad(pointAo, pointBo) == false)
                {
                    //Debug.Log("Road Cross clear");
                    BuildSidewalk(verts, trisB, uvs, 0f,
                    curveOuterPoints[j - 1], curveOuterPoints[j],  // outer R prev, next
                    curvePoints[j - 1], curvePoints[j]   // inner R prev, next
                );

                    BuildSidewalk(verts, trisB, uvs, 0f,
                        curveOuterPoints[curvePoints.Count - j - 1], curveOuterPoints[curvePoints.Count - j],  // outer R prev, next
                        curvePoints[curvePoints.Count - j - 1], curvePoints[curvePoints.Count - j]   // inner R prev, next
                    );
                }

                BuildUnderRoad(0, 0f, verts, trisB, trisB, uvs,
                    //curvePoints[j - 1], curvePoints[j],   // inner R prev, next
                    //curveOuterPoints[j - 1], curveOuterPoints[j]  // outer R prev, next
                    curvePoints[j], curvePoints[j - 1],   // inner R prev, next
                    curveOuterPoints[j], curveOuterPoints[j - 1]  // outer R prev, next
                );

                BuildUnderRoad(0, 0f, verts, trisB, trisB, uvs,
                    curvePoints[curvePoints.Count - j], curvePoints[curvePoints.Count - j - 1],   // inner R prev, next
                    curveOuterPoints[curvePoints.Count - j], curveOuterPoints[curvePoints.Count - j - 1]  // outer R prev, next
                );

            }

        }
    }

    private bool DoVectorsCrossRoad(Vector3 pointA, Vector3 pointB)
    {
        if (intersections == null ) return false;

        // so we care only if we are going form an outer to an outer and an inner top an inner
        if (m_vertsP1.Contains(pointA) && m_vertsP2.Contains(pointB))
        {
            return true;
        } else if (m_vertsP1.Contains(pointB) && m_vertsP2.Contains(pointA))
        {
            return true;
        }
        return false;
    }

    private int SortPoints(Vector3 center1, Vector3 center2, Vector3 center3)
    {
        float dist1 = Vector3.Distance(center1, center2);
        float dist2 = Vector3.Distance(center1, center3);
        if (dist1 == dist2)
        {
            return 0;
        }
        else if (dist1 > dist2)
        {
           return 1;        
        } else
        {
            return -1;
        }
    }

    public void AddJunction(Intersection intersection)
    {
        Debug.Log("AddIntersection");
        if (intersection == null)
        {
            Debug.Log("intersection is null...");
            return;
        }

        intersections.Add(intersection);

        Rebuild();
    }


    private void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
        JunctionBuildOverlay.OnChangeValueEvent += Rebuild;
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
        JunctionBuildOverlay.OnChangeValueEvent -= Rebuild;
    }

    private void OnSplineChanged(Spline arg1, int arg2, SplineModification arg3)
    {
        Rebuild();
    }

    private void Rebuild()
    {
        GetVerts();
        BuildMesh();
    }
}
