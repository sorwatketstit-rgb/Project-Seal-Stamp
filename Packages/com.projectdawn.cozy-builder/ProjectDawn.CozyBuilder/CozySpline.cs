using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [Tooltip("Defines the type of spline topology.")]
    public enum SplineTopology
    {
        [Tooltip("A simple line strip topology.")]
        LineStrip,

        [Tooltip("A topology with multiple row strips.")]
        CutmulRowStrip,
    }

    [AddComponentMenu("Cozy Builder/Geometry/Cozy Spline")]
    public unsafe class CozySpline : MonoBehaviour, ICozyBuilder
    {
        [Tooltip("Defines the type of spline topology.")]
        public SplineTopology Topology = SplineTopology.LineStrip;

        public int Detail = 25;

        [SerializeField]
        List<CozyPoint> m_Points = new();

        float[] m_Lenghts;
        float m_Length;

        public float3 Position { get => transform.position; set => transform.position = value; }

        public List<CozyPoint> Points => m_Points;

        public bool IsValid => m_Points.Count > 1;
        public bool IsCreated => m_Lenghts != null && m_Points.Count > 0;

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            if (!IsValid)
                return 0;

            HashBuffer hash = new HashBuffer();
            hash.Write(Detail);
            hash.Write(Topology);
            foreach (var point in Points)
                hash.Write(point.Position);
            return hash.GetHash();
        }

        public float CalculateLength()
        {
            return m_Length;
        }

        public float3 SampleSpline(float t)
        {
            var length = math.saturate(t) * m_Length;
            var rawT = GetRawT(length);
            return SampleSplineWithRawT(rawT);
        }

        public float3 SampleSplineWithRawT(float t)
        {
            if (!IsValid)
                return 0;

            int pointCount = Points.Count;

            // Get control points
            float3* controlPoints = stackalloc float3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                if (Points[i])
                    controlPoints[i] = Points[i].Position;
            }


            // Handle the case for 2 control points (simple linear interpolation)
            if (pointCount == 2)
            {
                return math.lerp(controlPoints[0], controlPoints[1], t);
            }

            switch (Topology)
            {
                case SplineTopology.LineStrip:
                    {
                        float a = t * (pointCount - 1);
                        int currPointIndex = (int)a;

                        float localT = a - currPointIndex;


                        float3 p0 = controlPoints[currPointIndex];
                        float3 p1 = controlPoints[Mathf.Clamp(currPointIndex + 1, 0, pointCount - 1)];

                        return math.lerp(p0, p1, localT);
                    }
                case SplineTopology.CutmulRowStrip:
                    {
                        // Catmull-Rom spline interpolation for more than 2 points
                        // Calculate which segment to interpolate within
                        int numSections = pointCount - 1;
                        int currPointIndex = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);

                        // Calculate local t (for the current segment)
                        float localT = t * numSections - currPointIndex;

                        // Get the control points for the current segment and surrounding points
                        float3 p0 = controlPoints[Mathf.Clamp(currPointIndex - 1, 0, pointCount - 1)];
                        float3 p1 = controlPoints[currPointIndex];
                        float3 p2 = controlPoints[Mathf.Clamp(currPointIndex + 1, 0, pointCount - 1)];
                        float3 p3 = controlPoints[Mathf.Clamp(currPointIndex + 2, 0, pointCount - 1)];

                        // Perform Catmull-Rom interpolation
                        //return CatmullRomCurve(p0, p1, p2, p3, localT, Alpha);
                        return CatmullRom(p0, p1, p2, p3, localT);
                    }
                default:
                    throw new System.NotImplementedException();
            }
        }

        // Helper function to perform Catmull-Rom interpolation
        public static float3 CatmullRom(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2.0f * p1) +
                (-p0 + p2) * t +
                (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
                (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
            );
        }

#if UNITY_EDITOR
        public void DrawSpline(Color color, float thickness)
        {
            if (!IsValid)
                return;
            if (!IsCreated)
                return;
            UnityEditor.Handles.color = color;
            Gizmos.color = color;
            switch (Topology)
            {
                case SplineTopology.LineStrip:
                    {
                        float3 previousPoint = m_Points[0].Position;
                        for (int i = 1; i < m_Points.Count; i++)
                        {
                            float3 currentPoint = m_Points[i].Position;
                            CozyGizmos.DrawLine(previousPoint, currentPoint, thickness);
                            previousPoint = currentPoint;

                        }
                        break;
                    }
                case SplineTopology.CutmulRowStrip:
                    {
                        float3 previousPoint = SampleSpline(0);
                        for (int i = 1; i < Detail; i++)
                        {
                            float3 currentPoint = SampleSpline(i / (Detail - 1f));
                            CozyGizmos.DrawLine(previousPoint, currentPoint, thickness);
                            previousPoint = currentPoint;

                        }
                        break;
                    }
                default:
                    throw new System.NotImplementedException();
            }
        }
#endif

        public void Build(ref CozyBuilderContext context)
        {
            switch (Topology)
            {
                case SplineTopology.LineStrip:
                    {
                        m_Lenghts = new float[m_Points.Count];
                        m_Length = 0;

                        float3 previousPoint = m_Points[0].Position;
                        for (int i = 1; i < m_Points.Count; i++)
                        {
                            float3 currentPoint = m_Points[i].Position;

                            float diff = math.distance(previousPoint, currentPoint);
                            m_Length += diff;
                            m_Lenghts[i] = m_Length;
                            previousPoint = currentPoint;
                        }
                        break;
                    }
                case SplineTopology.CutmulRowStrip:
                    {
                        m_Lenghts = new float[Detail];
                        m_Length = 0;

                        float3 previousPoint = m_Points[0].Position;
                        for (int i = 1; i < Detail; i++)
                        {
                            float t = i / (Detail - 1f);
                            float3 currentPoint = SampleSplineWithRawT(t);

                            float diff = math.distance(previousPoint, currentPoint);
                            m_Length += diff;
                            m_Lenghts[i] = m_Length;
                            previousPoint = currentPoint;
                        }
                        break;
                    }
                default:
                    throw new System.NotImplementedException();
            }
        }

        public float GetRawT(float3 point)
        {
            switch (Topology)
            {
                case SplineTopology.LineStrip:
                    {
                        float closestT = 0;
                        float closestDistance = float.MaxValue;
                        float3 previous = m_Points[0].Position;
                        for (int i = 1; i < m_Points.Count; i++)
                        {
                            float3 currentPoint = m_Points[i].Position;
                            float t = ClosestTOnLine(previous, currentPoint, point);
                            float distance = math.distancesq(math.lerp(previous, currentPoint, t), point);

                            if (closestDistance > distance)
                            {
                                closestT = t + i - 1;
                                closestDistance = distance;
                            }

                            previous = currentPoint;
                        }
                        return closestT / (m_Points.Count - 1);
                    }
                case SplineTopology.CutmulRowStrip:
                    {
                        float closestT = 0;
                        float closestDistance = float.MaxValue;
                        float3 previous = m_Points[0].Position;
                        for (int i = 1; i < Detail; i++)
                        {
                            float3 currentPoint = SampleSplineWithRawT(i / (Detail - 1f));
                            float t = ClosestTOnLine(previous, currentPoint, point);
                            float distance = math.distancesq(math.lerp(previous, currentPoint, t), point);

                            if (closestDistance > distance)
                            {
                                closestT = t + i - 1;
                                closestDistance = distance;
                            }

                            previous = currentPoint;
                        }
                        return closestT / (Detail - 1);
                    }
                default:
                    throw new System.NotImplementedException();
            }
        }

        // Takes length*normalized_t and gets raw_t
        public float GetRawT(float length)
        {
            // Based on https://youtu.be/o9RK6O2kOKo?si=0jofKOa-YheUadb-
            float accumulatedT = 0;
            float step = 1f / (m_Lenghts.Length - 1f);
            float previousSegmentLength = m_Lenghts[0];
            for (int i = 1; i < m_Lenghts.Length; i++)
            {
                float segmentLength = m_Lenghts[i];
                float d = (segmentLength - previousSegmentLength);
                if (length > segmentLength || d < math.EPSILON)
                {
                    previousSegmentLength = segmentLength;
                    accumulatedT += step;
                    continue;
                }

                float l = (length - previousSegmentLength) / d;

                return accumulatedT + step * l;
            }

            return 1.0f;
        }

        // Takes raw_t and gets length
        public float GetLength(float t)
        {
            // Step size between segments
            float step = 1f / (m_Lenghts.Length - 1f);
            float accumulatedT = 0;
            float previousSegmentLength = m_Lenghts[0];

            for (int i = 1; i < m_Lenghts.Length; i++)
            {
                float segmentLength = m_Lenghts[i];
                float d = (segmentLength - previousSegmentLength);

                if (t > accumulatedT + step || d < math.EPSILON)
                {
                    previousSegmentLength = segmentLength;
                    accumulatedT += step;
                    continue;
                }

                // Calculate the normalized position within the segment
                float l = (t - accumulatedT) / step;

                return previousSegmentLength + l * d;
            }

            return m_Lenghts[m_Lenghts.Length - 1]; // Return the maximum length if t is 1.0f
        }

        public float GetPointTAtIndex(int index)
        {
            float rawT = (float)index / (m_Points.Count - 1);
            float length = GetLength(rawT);
            return length / CalculateLength();
        }

        public static float ClosestTOnLine(float3 from, float3 to, float3 point)
        {
            float3 towards = to - from;

            float lengthSq = math.lengthsq(towards);
            if (lengthSq < math.EPSILON)
                return 0;

            float t = math.dot(point - from, towards) / lengthSq;

            // Force within the segment
            t = math.saturate(t);

            return t;
        }
    }
}