using NUnit.Framework;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using UnityEngine.UIElements;

[ExecuteInEditMode()]

public class SplineRoad : MonoBehaviour
{
    [Header("Right-click the component header to Rebuild Road")]

    [Header("Debug Settings")]
    [SerializeField] private bool drawMeshNodes = false;
    [SerializeField] private bool drawMeshJunctionNodes = false;


    [Header("Spline Info")]
    [SerializeField] SpineSampler m_splineSampler;
    [SerializeField] MeshFilter m_meshFilter;

    [Header("Road Settings")]
    [SerializeField][UnityEngine.Range(0f, 5f)] private float m_width;
    [SerializeField] int resolution;
    [SerializeField] bool UseNewDynamicScaling = false;
    [SerializeField][Tooltip("New Resolution system")] int pointsPerMeter = 10;
    [SerializeField] float m_curveStep;
    [Header("Sidewalk Settings")]
    [SerializeField][UnityEngine.Range(0f, 3f)] private float SidewalkHeight;
    [SerializeField][UnityEngine.Range(0f, 3f)] private float SidewalkTotalWidth;


    [Header("Intersections & Junctions")]
    public List<Intersection> intersections;
    public List<JunctionEdge> junctionEdges;

    List<Vector3> m_vertsP1;
    List<Vector3> m_vertsP2;
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

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.TransformPoint(m_vertsP2[i]), 0.25f);
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
    }

    private void GetVerts()
    {
        //Debug.Log("m_splineSampler.NumSplines: " + m_splineSampler.NumSplines);

        if (UseNewDynamicScaling)
        {
            GetScaledVerts();
        } 
        else
        {
            GetNonDynamicVerts();
        }

    }

    private void GetScaledVerts()
    {
        m_vertsP1 = new List<Vector3>();
        m_vertsP2 = new List<Vector3>();

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
                m_splineSampler.SampleSplineWidth(j, t, m_width, out p1, out p2);
                m_vertsP1.Add(transform.InverseTransformPoint(p1));
                m_vertsP2.Add(transform.InverseTransformPoint(p2));
            }

            //m_splineSampler.SampleSplineWidth(j, 1f, m_width, out p1, out p2);

            //m_vertsP1.Add(transform.InverseTransformPoint(p1));
            //m_vertsP2.Add(transform.InverseTransformPoint(p2));
        }
    }

    private void GetNonDynamicVerts()
    {
        m_vertsP1 = new List<Vector3>();
        m_vertsP2 = new List<Vector3>();

        float step = 1f / (float)resolution;
        Vector3 p1;
        Vector3 p2;

        for (int j = 0; j < m_splineSampler.NumSplines; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                float t = step * i;
                m_splineSampler.SampleSplineWidth(j, t, m_width, out p1, out p2);
                //Debug.Log($"t: {t} -> p1: {p1}, p2: {p2}");
                m_vertsP1.Add(transform.InverseTransformPoint(p1));
                m_vertsP2.Add(transform.InverseTransformPoint(p2));
            }

            //m_splineSampler.SampleSplineWidth(j, 1f, m_width, out p1, out p2);
            //////Debug.Log($"t: {t} -> p1: {p1}, p2: {p2}");
            //m_vertsP1.Add(transform.InverseTransformPoint(p1));
            //m_vertsP2.Add(transform.InverseTransformPoint(p2));
        }
    }

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
        m.SetTriangles(trisB, 1);
        m.SetTriangles(trisSidewalk, 2);

        m.SetUVs(0, uvs);

        //m.SetVertices(verts);
        m.RecalculateNormals();
        m.RecalculateBounds();

        m_meshFilter.sharedMesh = m;
    }

    private void GenerateNormalRoadsVertsTrisUVs(int offset, float uvOffset, List<Vector3> verts, List<int> sidewalkTris, List<int> tris, List<Vector2> uvs)
    {
        if (UseNewDynamicScaling)
        {
            // prefixSum to walk through m_vertsP1/m_vertsP2:
            int prefix = 0;

            for (int j = 0; j < m_splineSampler.NumSplines; j++)
            {
                int count = _samplesPerSpline[j];      // how many samples this spline got
                int segments = count - 1;            // how many quads we'll build

                // now build one quad per segment:
                for (int i = 1; i < count; i++)
                {


                    // get your four corner points from the precomputed lists:
                    int vi = prefix + i;
                    Vector3 p1 = m_vertsP1[vi - 1];
                    Vector3 p2 = m_vertsP2[vi - 1];
                    Vector3 p3 = m_vertsP1[vi];
                    Vector3 p4 = m_vertsP2[vi];

                    //// exactly the same indexing math you had, but dynamic:
                    //int baseTri = 4 * (segments * j + (i - 1));

                    // **NEW**: anchor your tris at the next free index
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

                    // UV along the length of each segment:
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

                    BuildSidewalk(verts, sidewalkTris, uvs, uvOffset, p3, p1);
                    BuildSidewalk(verts, sidewalkTris, uvs, uvOffset, p2, p4);

                }

                int startIdx = prefix;          // first sample of this spline
                int endIdx = prefix + count - 1;  // last sample

                Vector3 p1s = m_vertsP1[startIdx];
                Vector3 p2s = m_vertsP2[startIdx];
                Vector3 p1e = m_vertsP1[endIdx];
                Vector3 p2e = m_vertsP2[endIdx];

                // for the start cap, direction from start-left→start-right
                Vector3 forwardS = -1 * (p2s - p1s).normalized;
                Vector3 leftDirS = Vector3.Cross(Vector3.up, forwardS).normalized;
                Vector3 rightDirS = -leftDirS;

                // for the end cap, direction from end-left→end-right
                Vector3 forwardE = (p2e - p1e).normalized;
                Vector3 leftDirE = Vector3.Cross(Vector3.up, forwardE).normalized;
                Vector3 rightDirE = -leftDirE;

                //// START sidewalk caps
                BuildRoadCap(p1s, p1s + forwardS * SidewalkTotalWidth, uvOffset, verts, sidewalkTris, uvs);  // left sidewalk start
                BuildRoadCap(p2s - forwardS * SidewalkTotalWidth, p2s, uvOffset, verts, sidewalkTris, uvs);  // right sidewalk start

                //// END sidewalk caps
                BuildRoadCap(p1e + forwardS * SidewalkTotalWidth, p1e, uvOffset, verts, sidewalkTris, uvs);  // left sidewalk end
                BuildRoadCap(p2e, p2e - forwardS * SidewalkTotalWidth, uvOffset, verts, sidewalkTris, uvs);  // right sidewalk end

                prefix += count;
            }
        }
        else
        {
            for (int currentSplineIndex = 0; currentSplineIndex < m_splineSampler.NumSplines; currentSplineIndex++)
            {
                int splineOffset = resolution * currentSplineIndex;
                splineOffset += currentSplineIndex;

                // iterate verts and build a face
                for (int currentSplinePoint = 1; currentSplinePoint < resolution + 1; currentSplinePoint++)
                {
                    int vertOffset = splineOffset + currentSplinePoint;
                    Vector3 p1 = m_vertsP1[vertOffset - 1];
                    Vector3 p2 = m_vertsP2[vertOffset - 1];
                    Vector3 p3 = m_vertsP1[vertOffset];
                    Vector3 p4 = m_vertsP2[vertOffset];

                    offset = 4 * resolution * currentSplineIndex;
                    offset += 4 * (currentSplinePoint - 1);

                    int t1 = offset + 0;
                    int t2 = offset + 2;
                    int t3 = offset + 3;

                    int t4 = offset + 3;
                    int t5 = offset + 1;
                    int t6 = offset + 0;

                    verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                    tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });

                    float distance = Vector3.Distance(p1, p3) / 4f;
                    float uvDistance = uvOffset + distance;
                    uvs.AddRange(new List<Vector2> { new Vector2(uvOffset, 0), new Vector2(uvOffset, 1), new Vector2(uvDistance, 0), new Vector2(uvDistance, 1) });

                    uvOffset += distance;
                }
            }
        }   
    }

    void BuildSidewalk(List<Vector3> verts, List<int> tris, List<Vector2> uvs, float uvOffset, Vector3 a0, Vector3 a1)
    {
        Vector3 forward = (a1 - a0).normalized;
        Vector3 dir = Vector3.Cross(Vector3.up, forward).normalized;

        float h = SidewalkHeight;      // thickness
        float w = SidewalkTotalWidth;  // outward width
        float segLen = Vector3.Distance(a0, a1) / 4f; // how far we advance the U coord

        /* -------- vertex layout ----------
           0-3  inner wall     (bottom, top) x2
           4-7  outer wall     (bottom, top) x2
           8-11 duplicate top  (inner, outer) x2  *<-- only for the top quad*
        ----------------------------------- */
        int b = verts.Count;

        // wall vertices (shared by vertical faces only)
        verts.AddRange(new[] {
            /*0*/ a0,
            /*1*/ a0 + Vector3.up*h,
            /*2*/ a1,
            /*3*/ a1 + Vector3.up*h,

            /*4*/ a0 + dir*w,
            /*5*/ a0 + dir*w + Vector3.up*h,
            /*6*/ a1 + dir*w,
            /*7*/ a1 + dir*w + Vector3.up*h,
        });

        // duplicates for the horizontal (top) quad
        verts.AddRange(new[] {
            /*8*/  verts[b+1],           // iTop  (dup)
            /*9*/  verts[b+5],           // oTop  (dup)
            /*10*/ verts[b+7],           // oTop2 (dup)
            /*11*/ verts[b+3],           // iTop2 (dup)
        });

        /* ---------- triangles ---------- */

        // inner wall – now faces the opposite way
        tris.AddRange(new[] {
            b+0, b+2, b+1,   // ← swapped 1 & 2
            b+2, b+3, b+1
        });

        // outer wall (unchanged – already correct)
        tris.AddRange(new[] {
            b+4, b+5, b+6,
            b+6, b+5, b+7
        });

        // top face – now points up instead of down
        tris.AddRange(new[] {
            b+8,  b+10, b+9,  // ← swapped 9 & 10
            b+10, b+8,  b+11
        });

        /* ---------- UVs ---------- */

        // inner wall  (U = length, V = height)
        uvs.AddRange(new[] {
            new Vector2(uvOffset,          0f),
            new Vector2(uvOffset,          1f),
            new Vector2(uvOffset+segLen,   0f),
            new Vector2(uvOffset+segLen,   1f),
        });

        // outer wall  (U = length, V = height)
        uvs.AddRange(new[] {
            new Vector2(uvOffset,          0f),
            new Vector2(uvOffset,          1f),
            new Vector2(uvOffset+segLen,   0f),
            new Vector2(uvOffset+segLen,   1f),
        });

        // top face  (U = length, V = width) -- uses duplicated verts
        uvs.AddRange(new[] {
            new Vector2(uvOffset,          0f),        // iTop
            new Vector2(uvOffset,          w),         // oTop
            new Vector2(uvOffset+segLen,   w),         // oTop2
            new Vector2(uvOffset+segLen,   0f),        // iTop2
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
                m_splineSampler.SampleSplineWidth(splineIndex, t, m_width, out Vector3 p1, out Vector3 p2);
                m_splineSampler.SampleSplineWidth(splineIndex, t, m_width, out Vector3 p1o, out Vector3 p2o);

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
            }

            curvePoints.Reverse();
            intersection.curvePoints = curvePoints;

            int pointsOffset = verts.Count;
            for (int j = 1; j <= curvePoints.Count; j++)
            {
                Vector3 vertB;

                verts.Add(transform.InverseTransformPoint(center));
                Vector3 vertA = curvePoints[j - 1];
                if (j == curvePoints.Count)
                {
                    vertB = curvePoints[0];
                }
                else
                {
                    vertB = curvePoints[j];
                }

                verts.Add(vertA);
                verts.Add(vertB);

                trisB.Add(pointsOffset + ((j - 1) * 3) + 0);
                trisB.Add(pointsOffset + ((j - 1) * 3) + 1);
                trisB.Add(pointsOffset + ((j - 1) * 3) + 2);

                uvs.Add(new Vector2(transform.InverseTransformPoint(center).z, transform.InverseTransformPoint(center).x));
                uvs.Add(new Vector2(vertA.z, vertA.x));
                uvs.Add(new Vector2(vertB.z, vertB.x));

            }
        }
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
