using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace ProjectDawn.CozyBuilder.Editor
{
    public static class CozyBuilderUserSettings
    {
        public static Color GizmosDefaultColor
        {
            get
            {
                ColorUtility.TryParseHtmlString(EditorPrefs.GetString("CozyBuilder_GizmosDefaultColor", "#FFFFFFFF"), out var color);
                return color;
            }
            set
            {
                var text = "#" + ColorUtility.ToHtmlStringRGBA(value);
                EditorPrefs.SetString("CozyBuilder_GizmosDefaultColor", text);
            }
        }

        public static Color GizmosSelectedColor
        {
            get
            {
                ColorUtility.TryParseHtmlString(EditorPrefs.GetString("CozyBuilder_GizmosSelectedColor", "#FF6600FF"), out var color);
                return color;
            }
            set
            {
                var text = "#" + ColorUtility.ToHtmlStringRGBA(value);
                EditorPrefs.SetString("CozyBuilder_GizmosSelectedColor", text);
            }
        }

        public static float GizmosPointSize
        {
            get
            {
                return EditorPrefs.GetFloat("CozyBuilder_GizmosPointSize", 0.5f);
            }
            set
            {
                EditorPrefs.SetFloat("CozyBuilder_GizmosPointSize", value);
            }
        }

        public static float GizmosLineWidth
        {
            get
            {
                return EditorPrefs.GetFloat("CozyBuilder_GizmosLineWidth", 0.5f);
            }
            set
            {
                EditorPrefs.SetFloat("CozyBuilder_GizmosLineWidth", value);
            }
        }

        public static float GizmosZOffset
        {
            get
            {
                return EditorPrefs.GetFloat("CozyBuilder_GizmosZOffset", 0.5f);
            }
            set
            {
                CozyGizmos.GetLineMaterial().SetFloat("_HandleZOffset", 0.016f * value);
                CozyGizmos.GetPointMaterial().SetFloat("_ZOffset", 0.008f * value);
                EditorPrefs.SetFloat("CozyBuilder_GizmosZOffset", value);
            }
        }
    }

    class CozyBuilderSettingsProvider : SettingsProvider
    {
        static class Styles
        {
            public static readonly GUIContent DefaultColor = EditorGUIUtility.TrTextContent(
                "Default Color",
                "Specifies the default color used for rendering points and elements when not selected.");

            public static readonly GUIContent SelectedColor = EditorGUIUtility.TrTextContent(
                "Selected Color",
                "Defines the color used to highlight points and elements when they are selected.");

            public static readonly GUIContent PointSize = EditorGUIUtility.TrTextContent(
                "Point Size",
                "Adjusts the size of the gizmo representation for points in the scene view.");

            public static readonly GUIContent LineWidth = EditorGUIUtility.TrTextContent(
                "Line Width",
                "Sets the thickness of the gizmo lines displayed for connections or edges.");

            public static readonly GUIContent ZOffset = EditorGUIUtility.TrTextContent(
                "Z Offset",
                "Sets the offset at which gizmos are considered occluded.");
        }

        public CozyBuilderSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            label = "Cozy Builder";
        }

        private Label CreateSectionLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("preferences-window__title");
            return label;
        }

        public override void OnGUI(string searchContext)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10); // shift left to compensate margin, tweak this value
            GUILayout.BeginVertical();

            EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Color defaultColor = EditorGUILayout.ColorField(Styles.DefaultColor, CozyBuilderUserSettings.GizmosDefaultColor);
            if (EditorGUI.EndChangeCheck())
            {
                CozyBuilderUserSettings.GizmosDefaultColor = defaultColor;
            }

            EditorGUI.BeginChangeCheck();
            Color selectedColor = EditorGUILayout.ColorField(Styles.SelectedColor, CozyBuilderUserSettings.GizmosSelectedColor);
            if (EditorGUI.EndChangeCheck())
            {
                CozyBuilderUserSettings.GizmosSelectedColor = selectedColor;
            }

            EditorGUI.BeginChangeCheck();
            float pointSize = EditorGUILayout.Slider(Styles.PointSize, CozyBuilderUserSettings.GizmosPointSize, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                CozyBuilderUserSettings.GizmosPointSize = pointSize;
            }

            EditorGUI.BeginChangeCheck();
            float lineWidth = EditorGUILayout.Slider(Styles.LineWidth, CozyBuilderUserSettings.GizmosLineWidth, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                CozyBuilderUserSettings.GizmosLineWidth = lineWidth;
            }

            EditorGUI.BeginChangeCheck();
            float zOffset = EditorGUILayout.Slider(Styles.ZOffset, CozyBuilderUserSettings.GizmosZOffset, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                CozyBuilderUserSettings.GizmosZOffset = zOffset;
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        //public override void OnActivate(string searchContext, VisualElement rootElement)
        //{
        //    rootElement.Add(CreateSectionLabel("Cozy Builder"));

        //    // Create a horizontal container
        //    var row = new VisualElement();
        //    row.style.flexDirection = FlexDirection.Row;
        //    row.style.alignItems = Align.Stretch;
        //    row.style.marginBottom = 4;

        //    var pointSizeSlider = new Slider("Point Size", 0f, 1f)
        //    {
        //        value = CozyBuilderUserSettings.PointSize,
        //        style = { width = 400 }
        //    };
        //    // Create float field
        //    var pointSize = new FloatField
        //    {
        //        value = CozyBuilderUserSettings.PointSize,
        //        style = { width = 60 }
        //    };
        //    pointSizeSlider.RegisterValueChangedCallback(evt =>
        //    {
        //        pointSize.SetValueWithoutNotify(evt.newValue);
        //        CozyBuilderUserSettings.PointSize = evt.newValue;
        //    });
        //    pointSize.RegisterValueChangedCallback(evt =>
        //    {
        //        var clamped = Mathf.Clamp(evt.newValue, 0f, 1f);
        //        pointSizeSlider.SetValueWithoutNotify(clamped);
        //        CozyBuilderUserSettings.PointSize = clamped;
        //    });
        //    row.Add(pointSizeSlider);
        //    row.Add(pointSize);

        //    rootElement.Add(row);
        //}

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider() => new CozyBuilderSettingsProvider("Preferences/CozyBuilder", SettingsScope.User);
    }
}
