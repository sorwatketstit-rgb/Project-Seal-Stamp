using UnityEditor;
using UnityEngine;

namespace ProjectDawn.CozyBuilder.Editor
{
    public class CozyRendererGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmoForMyScript(CozyPoint scr, GizmoType gizmoType)
        {
            SetColor(gizmoType);
            scr.DrawPoint(Gizmos.color, CozyBuilderUserSettings.GizmosPointSize * 0.040f);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawPlane(CozyPlane scr, GizmoType gizmoType)
        {
            SetColor(gizmoType);
            scr.DrwaPlane(Gizmos.color);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawSpline(CozySpline scr, GizmoType gizmoType)
        {
            SetColor(gizmoType);
            scr.DrawSpline(Gizmos.color, CozyBuilderUserSettings.GizmosLineWidth * 4);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawSpline(CozyAttachment scr, GizmoType gizmoType)
        {
            SetColor(gizmoType);
            CozyGizmos.DrawPoint(scr.transform.position, CozyBuilderUserSettings.GizmosPointSize * 0.040f);
        }

        static void SetColor(GizmoType gizmoType)
        {
            Gizmos.color = (gizmoType & (GizmoType.Selected | GizmoType.InSelectionHierarchy)) != 0 ?
                CozyBuilderUserSettings.GizmosSelectedColor :
                CozyBuilderUserSettings.GizmosDefaultColor;
            if (gizmoType == GizmoType.Pickable)
                Gizmos.color = Color.black;
        }
    }
}