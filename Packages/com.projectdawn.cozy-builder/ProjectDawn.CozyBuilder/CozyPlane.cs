using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    [AddComponentMenu("Cozy Builder/Geometry/Cozy Plane")]
    public class CozyPlane : MonoBehaviour
    {
        [Tooltip("First spline of the plane. If the surface follows the Y axis, it is recommended that this spline be placed below SplineB.")]
        public CozySpline SplineA;

        [Tooltip("Second spline of the plane. If the surface follows the Y axis, it is recommended that this spline be placed above SplineA.")]
        public CozySpline SplineB;

        public float3 Position { get => transform.position; set => transform.position = value; }

        public bool IsValid => SplineA && SplineA.IsValid && SplineB && SplineB.IsValid;

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            if (SplineA == null | SplineB == null)
                return 0;
            HashBuffer hash = new HashBuffer();
            hash.Write(SplineA.CalculateHash());
            hash.Write(SplineB.CalculateHash());
            hash.Write(transform.position);
            return hash.GetHash();
        }

        public float3 SamplePlane(float2 uv)
        {
            float3 a = SplineA.SampleSpline(uv.x);
            float3 b = SplineB.SampleSpline(uv.x);
            return math.lerp(a, b, uv.y);
        }

        public float2 GetUv(float3 point)
        {
            float aT = SplineA.GetRawT(point);
            float bT = SplineB.GetRawT(point);

            float3 a = SplineA.SampleSplineWithRawT(aT);
            float3 b = SplineB.SampleSplineWithRawT(bT);

            float2 uv;
            uv.y = CozySpline.ClosestTOnLine(a, b, point);
            uv.x = math.lerp(aT, bT, uv.y);

            return uv;
        }

        public Rect GetRect(float3 point, float2 size)
        {
            float aT = SplineA.GetRawT(point);
            float bT = SplineB.GetRawT(point);

            float3 a = SplineA.SampleSplineWithRawT(aT);
            float3 b = SplineB.SampleSplineWithRawT(bT);

            float v = CozySpline.ClosestTOnLine(a, b, point);

            float2 uv;
            uv.x = math.lerp(SplineA.GetLength(aT) / SplineA.CalculateLength(), SplineB.GetLength(bT) / SplineB.CalculateLength(), v);
            uv.y = v;

            float horizontalLength = math.min(SplineA.CalculateLength(), SplineB.CalculateLength());
            float width = size.x / horizontalLength;

            float verticalLength = math.distance(a, b);
            float height = size.y / verticalLength;

            return new Rect(uv.x - width * 0.5f, uv.y - height * 0.5f, width, height);
        }

        public float CalculateRowLength() => math.min(SplineA.CalculateLength(), SplineB.CalculateLength());
        public float CalculateColumnLength(float u)
        {
            float3 a = SplineA.SampleSpline(u);
            float3 b = SplineB.SampleSpline(u);
            return math.distance(a, b);
        }

#if UNITY_EDITOR
        Mesh Mesh;
        uint ArtifactHash;

        public void DrwaPlane(Color color)
        {
            if (!IsValid)
                return;
            if (!SplineA.IsCreated || !SplineB.IsCreated)
                return;
            color.a = 0.3f;
            var hash = CalculateHash();
            if (ArtifactHash != hash)
            {
                Mesh = null;
                ArtifactHash = hash;
            }

            if (Mesh == null)
                Mesh = GenerateMesh(math.max(SplineA.Detail, SplineB.Detail));

            Gizmos.color = color;
            Gizmos.DrawMesh(Mesh, 0);
        }

        Mesh GenerateMesh(int iterations)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> indices = new List<int>();

            Vector3 normal = new Vector3(0, 0, 0);
            Color color = new Color(3, 3, 3, 1);

            // Iterate over the segments
            for (int i = 1; i < iterations; i++)
            {
                // Get points from SplineA and SplineB
                Vector3 a = SplineA.SampleSpline((float)(i - 1) / (iterations - 1));
                Vector3 b = SplineA.SampleSpline((float)(i) / (iterations - 1));

                Vector3 c = SplineB.SampleSpline((float)(i - 1) / (iterations - 1));
                Vector3 d = SplineB.SampleSpline((float)(i) / (iterations - 1));

                // Add vertices in the same order as in the GL version (quad)
                vertices.Add(a); // 1st vertex
                vertices.Add(c); // 2nd vertex
                vertices.Add(d); // 3rd vertex
                vertices.Add(b); // 4th vertex

                // Add indices for the two triangles that make up the quad
                int baseIndex = (i - 1) * 4;
                indices.Add(baseIndex + 0); // First triangle: c -> d -> b
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);

                indices.Add(baseIndex + 0); // Second triangle: c -> b -> a
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 3);

                normals.Add(normal); // 1st vertex
                normals.Add(normal); // 2nd vertex
                normals.Add(normal); // 3rd vertex
                normals.Add(normal); // 4th vertex

                colors.Add(color); // 1st vertex
                colors.Add(color); // 2nd vertex
                colors.Add(color); // 3rd vertex
                colors.Add(color); // 4th vertex
            }

            // Assign the vertices and indices to the mesh
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);

            // Recalculate bounds and normals if necessary (optional)
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }
#endif
    }
}