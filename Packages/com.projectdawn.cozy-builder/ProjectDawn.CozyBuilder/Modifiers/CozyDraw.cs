using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

namespace ProjectDawn.CozyBuilder
{
    /// <summary>
    /// Cozy Draw generates draw calls for each cell. For prefabs, use Models/Brick/Brick, which includes four variations.
    /// </summary>
    [AddComponentMenu("Cozy Builder/Modifier/Cozy Draw")]
    public class CozyDraw : MonoBehaviour, ICozyGridModifier, ICozySegmentsModifier
    {
        [Tooltip("List of prefabs to be instantiated.")]
        public List<GameObject> Prefabs;

        [Tooltip("Material that will override the material of instantiated prefabs.")]
        public Material OverrideMaterial;

        [Tooltip("Seed used for randomization.")]
        public uint Seed = 1;

        [Tooltip("Rotation applied to prefabs in degrees.")]
        public float3 Rotation;

        [Tooltip("Defines how the prefab's width is scaled.")]
        public Scaling WidthScaling = Scaling.Stretch;

        [Tooltip("Defines how the prefab's height is scaled.")]
        public Scaling HeightScaling = Scaling.Stretch;

        [Tooltip("Defines how the prefab's depth is scaled.")]
        public Scaling DepthScaling = Scaling.Stretch;

        [Tooltip("Size of the grid cells.")]
        [FormerlySerializedAs("MinPerimeter")]
        public float3 CellSize = 0.2f;

        [Tooltip("Enable to use a global rotation for all instantiated prefabs.")]
        public bool GlobalRotation;

        [Tooltip("Minimum area threshold for placing prefabs.")]
        public float MinArea = 0.001f;

        public uint CalculateHash()
        {
            if (!isActiveAndEnabled)
                return 0;
            if (Prefabs.IsNull())
                return 0;
            HashBuffer hash = new HashBuffer();
            hash.Write(Seed);
            hash.Write(OverrideMaterial ? OverrideMaterial.GetHashCode() : 0);
            hash.Write(WidthScaling);
            hash.Write(HeightScaling);
            hash.Write(DepthScaling);
            hash.Write(CellSize);
            hash.Write(GlobalRotation);
            hash.Write(MinArea);
            return hash.GetHash();
        }

        public void Modify(ref CozyBuilderContext context, NativeList<Cell> cells, CozyPlane plane)
        {
            Random random = new Random(Seed);

            quaternion globalRoation = quaternion.identity;
            if (GlobalRotation)
            {
                float3 p0 = plane.SamplePlane(new float2(1f, 0.5f));
                float3 p1 = plane.SamplePlane(new float2(0f, 0.5f));
                float3 p2 = plane.SamplePlane(new float2(0.5f, 1));
                float3 p3 = plane.SamplePlane(new float2(0.5f, 0));

                float3 forward = math.normalizesafe(p0 - p1);
                float3 right = -math.normalizesafe(p2 - p3);
                float3 up = math.cross(forward, right);
                globalRoation = quaternion.LookRotation(forward, up);
            }

            float minAreaSq = MinArea * MinArea;

            foreach (Cell cell in cells)
            {
                Rect rect = cell.Rect;
                //if (rect.width * rect.height < 0.0001f)
                //    continue;

                float3 p0 = plane.SamplePlane(new float2(rect.x + rect.width, rect.y + rect.height * 0.5f));
                float3 p1 = plane.SamplePlane(new float2(rect.x, rect.y + rect.height * 0.5f));
                float3 p2 = plane.SamplePlane(new float2(rect.x + rect.width * 0.5f, rect.y + rect.height));
                float3 p3 = plane.SamplePlane(new float2(rect.x + rect.width * 0.5f, rect.y));

                if (math.distancesq(p0, p1) * math.distancesq(p2, p3) < minAreaSq)
                    continue;

                float3 center = (p0 + p1 + p2 + p3) * 0.25f;

                float3 forward = math.normalizesafe(p0 - p1);
                float3 right = -math.normalizesafe(p2 - p3);
                float3 up = math.cross(forward, right);
                quaternion rotation = quaternion.LookRotation(forward, up);

                if (GlobalRotation)
                    rotation = globalRoation;

                if (math.any(math.isnan(rotation.value)))
                    continue;

                rotation = math.mul(rotation, quaternion.EulerXYZ(math.radians(Rotation)));

                float3 scale = 1;
                if (WidthScaling == Scaling.Stretch)
                {
                    float scaleY = math.distance(p2, p3) / CellSize.y;
                    scale.x = scaleY;
                }
                if (HeightScaling == Scaling.Stretch)
                {
                    float scaleX = (math.distance(p0, p1) + cell.Border.x + cell.Border.y) / CellSize.x;
                    scale.z = scaleX;

                    center += math.rotate(rotation, new float3(0, 0, (cell.Border.x - cell.Border.y) * 0.5f));
                }
                if (DepthScaling == Scaling.Stretch)
                {
                    scale.y = cell.Depth / CellSize.z;
                    center += math.rotate(rotation, new float3(0, cell.Z, 0));
                }

                GameObject prefab = Prefabs[random.NextInt(0, Prefabs.Count)];
                context.Instantiate(prefab, center, rotation, scale, OverrideMaterial);
            }
        }

        public void Modify(ref CozyBuilderContext context, NativeList<Segment> segments, CozySpline spline)
        {
            Random random = new Random(Seed);

            // Initial right vector (could be any orthogonal to the spline at the start)
            float3 forward = math.normalizesafe(spline.SampleSpline(segments[0].X + segments[0].Width) - spline.SampleSpline(segments[0].X));
            float3 right = math.rotate(quaternion.EulerXYZ(math.radians(Rotation)), math.up());

            quaternion rot = quaternion.LookRotation(forward, right);

            foreach (Segment segment in segments)
            {
                float3 p0 = spline.SampleSpline(segment.X + segment.Width);
                float3 p1 = spline.SampleSpline(segment.X);

                float3 center = (p0 + p1) * 0.50f;

                float3 newForward = math.normalizesafe(p0 - p1);

                // Adjust the right vector using parallel transport
                float acosAngle = math.dot(forward, newForward);
                if (acosAngle < 0.99f)
                {
                    float3 rotationAxis = math.cross(forward, newForward);
                    float angle = math.acos(acosAngle);
                    quaternion rotationAdjustment = quaternion.AxisAngle(rotationAxis, angle);
                    right = math.mul(rotationAdjustment, right);

                    rot = math.mul(rot, rotationAdjustment);
                }

                // Update forward and calculate up
                forward = newForward;
                float3 up = math.cross(forward, right);

                quaternion rotation = quaternion.LookRotation(forward, right);

                float3 scale = 1;
                if (WidthScaling == Scaling.Stretch)
                {
                    float scaleY = 1f / CellSize.y;
                    scale.x = scaleY;
                }
                if (HeightScaling == Scaling.Stretch)
                {
                    float scaleX = math.distance(p0, p1) / CellSize.x;
                    scale.z = scaleX;
                }
                if (DepthScaling == Scaling.Stretch)
                {
                    scale.y = segment.Depth / CellSize.z;
                    center += math.rotate(rotation, new float3(0, segment.Z, 0));
                }

                GameObject prefab = Prefabs[random.NextInt(0, Prefabs.Count)];
                context.Instantiate(prefab, center, rotation, scale, OverrideMaterial);
            }
        }

        void OnEnable() { }
    }
}