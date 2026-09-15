#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace ProjectDawn.CozyBuilder
{
    public static class CozyGizmos
    {
        public static Color DefaultColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        public static Color SelectedColor = new Color(1, 0.4f, 0, 1);

        private static readonly int kPropUseGuiClip = Shader.PropertyToID("_UseGUIClip");

        private static readonly int kPropHandleZTest = Shader.PropertyToID("_HandleZTest");

        private static readonly int kPropColor = Shader.PropertyToID("_Color");

        private static readonly int kPropArcCenterRadius = Shader.PropertyToID("_ArcCenterRadius");

        private static readonly int kPropArcNormalAngle = Shader.PropertyToID("_ArcNormalAngle");

        private static readonly int kPropArcFromCount = Shader.PropertyToID("_ArcFromCount");

        private static readonly int kPropArcThicknessSides = Shader.PropertyToID("_ArcThicknessSides");

        private static readonly int kPropHandlesMatrix = Shader.PropertyToID("_HandlesMatrix");

        static Mesh QuadMesh;
        static Material PointMaterial;
        static Material LineMaterial;
        static GraphicsBuffer ArcIndexBuffer;

        public static Material GetPointMaterial()
        {
            if (PointMaterial == null)
                PointMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.projectdawn.cozy-builder/Models/Primitives/Point.mat");
            return PointMaterial;
        }

        public static Material GetLineMaterial()
        {
            if (LineMaterial == null)
                LineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.projectdawn.cozy-builder/Models/Primitives/Line.mat");
            return LineMaterial;
        }

        public static void DrawLine(float3 previous, float3 next, float thickness)
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawLineThickness(previous, next, thickness, 2);

                DrawLineThickness(previous, next, thickness, 1);
            }
            else
            {
                float distance = Vector3.Distance((previous + next) * 0.5f, Camera.current.transform.position);

                // Adjust the size of the sphere based on the distance from the camera
                float constantScreenSize = 0.020f; // Adjust this value to control the perceived size
                float sphereSize = distance * constantScreenSize;

                float length = math.distance(previous, next);
                if (length > math.EPSILON)
                {
                    float3 mid = (previous + next) * 0.5f;
                    float acosAngle = math.dot(math.right(), math.normalizesafe(next - previous));

                    quaternion rotationAdjustment;
                    if (acosAngle < 0.9999f)
                    {
                        float3 rotationAxis = math.normalizesafe(math.cross(math.right(), math.normalizesafe(next - previous)));
                        float angle = math.acos(acosAngle);
                        rotationAdjustment = quaternion.AxisAngle(rotationAxis, angle);
                    }
                    else
                    {
                        rotationAdjustment = quaternion.identity;
                    }
                    Gizmos.matrix = float4x4.TRS(mid, rotationAdjustment, new float3(length, sphereSize, sphereSize));
                    Gizmos.DrawCube(float3.zero, new float3(1, 1, 1));
                    Gizmos.matrix = float4x4.identity;
                }
            }
        }

        public static void DrawPoint(float3 position, float size)
        {
            if (PointMaterial == null)
                PointMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.projectdawn.cozy-builder/Models/Primitives/Point.mat");
            if (QuadMesh == null)
                QuadMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Packages/com.projectdawn.cozy-builder/Models/Primitives/Icosphere.mesh");

            float distance = Vector3.Distance(position, Camera.current.transform.position);

            // Adjust the size of the sphere based on the distance from the camera
            float constantScreenSize = size; // Adjust this value to control the perceived size
            float sphereSize = distance * constantScreenSize;

            if (Event.current.type == EventType.Repaint)
            {
                PointMaterial.SetColor("_GizmoBatchColor", Gizmos.color);
                PointMaterial.SetFloat("_Scale", sphereSize);

                PointMaterial.SetPass(1);
                Graphics.DrawMeshNow(QuadMesh, Matrix4x4.Translate(position));

                PointMaterial.SetPass(0);
                Graphics.DrawMeshNow(QuadMesh, Matrix4x4.Translate(position));
            }
            else
            {
                Gizmos.DrawSphere(position, sphereSize);
            }
        }

        static void DrawLineThickness(Vector3 p1, Vector3 p2, float thickness, int pass)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = SetupArcMaterial();

            material.SetVector(kPropArcCenterRadius, new Vector4(p1.x, p1.y, p1.z, 0f));
            material.SetVector(kPropArcFromCount, new Vector4(p2.x, p2.y, p2.z, 0f));
            material.SetVector(kPropArcThicknessSides, new Vector4(thickness, 8f, 0f, 0f));
            material.SetPass(pass);
            GraphicsBuffer arcIndexBuffer = GetArcIndexBuffer(60, 8);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, arcIndexBuffer, 48);
        }

        static GraphicsBuffer GetArcIndexBuffer(int segments, int sides)
        {
            int num = (segments - 1) * sides * 2 * 3;
            if (ArcIndexBuffer != null && ArcIndexBuffer.count == num)
            {
                return ArcIndexBuffer;
            }

            ArcIndexBuffer?.Dispose();
            AssemblyReloadEvents.beforeAssemblyReload += DisposeArcIndexBuffer;
            EditorApplication.quitting += DisposeArcIndexBuffer;
            ArcIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, num, 2);
            ushort[] array = new ushort[num];
            int num2 = 0;
            for (int i = 0; i < segments - 1; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int num3 = i * sides + j;
                    int num4 = i * sides + (j + 1) % sides;
                    int num5 = (i + 1) * sides + j;
                    int num6 = (i + 1) * sides + (j + 1) % sides;
                    array[num2] = (ushort)num3;
                    array[num2 + 1] = (ushort)num5;
                    array[num2 + 2] = (ushort)num4;
                    array[num2 + 3] = (ushort)num4;
                    array[num2 + 4] = (ushort)num5;
                    array[num2 + 5] = (ushort)num6;
                    num2 += 6;
                }
            }

            ArcIndexBuffer.SetData(array);
            return ArcIndexBuffer;
        }

        static void DisposeArcIndexBuffer()
        {
            ArcIndexBuffer?.Dispose();
            ArcIndexBuffer = null;
        }

        static Material SetupArcMaterial()
        {
            if (LineMaterial == null)
                LineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.projectdawn.cozy-builder/Models/Primitives/Line.mat");

            float lineTransparency = 1.0f;

            Color value = Gizmos.color * lineTransparency;
            LineMaterial.SetFloat(kPropUseGuiClip, Camera.current ? 0f : 1f);
            LineMaterial.SetFloat(kPropHandleZTest, (float)UnityEditor.Handles.zTest);
            LineMaterial.SetColor(kPropColor, value);
            LineMaterial.SetMatrix(kPropHandlesMatrix, Gizmos.matrix);
            return LineMaterial;
        }

        static Mesh GetQuadMesh()
        {
            // Create a new mesh
            Mesh mesh = new Mesh();
            mesh.name = "Quad";

            // Define vertices for a 1x1 unit quad centered at the origin
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-0.5f, -0.5f, 0); // Bottom-left
            vertices[1] = new Vector3(0.5f, -0.5f, 0);  // Bottom-right
            vertices[2] = new Vector3(-0.5f, 0.5f, 0);  // Top-left
            vertices[3] = new Vector3(0.5f, 0.5f, 0);   // Top-right

            // Define triangles (two triangles to form a quad)
            int[] triangles = new int[6];
            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 1;
            triangles[4] = 2;
            triangles[5] = 3;

            // Define UV coordinates
            Vector2[] uv = new Vector2[4];
            uv[0] = new Vector2(0, 0); // Bottom-left
            uv[1] = new Vector2(1, 0); // Bottom-right
            uv[2] = new Vector2(0, 1); // Top-left
            uv[3] = new Vector2(1, 1); // Top-right

            // Assign data to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;

            // Recalculate normals and bounds for lighting and rendering
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
#endif