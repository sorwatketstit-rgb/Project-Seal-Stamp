using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using ProjectDawn.CozyBuilder;

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty propertyX = property.FindPropertyRelative("Start");
        SerializedProperty propertyY = property.FindPropertyRelative("End");

        // Get the MinMaxRange attribute values
        MinMaxRangeAttribute range = (MinMaxRangeAttribute)attribute;

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Layout rects
        float fieldWidth = 50;
        Rect minFieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
        Rect sliderRect = new Rect(minFieldRect.xMax + 4, position.y, position.width - fieldWidth * 2f - 8, position.height);
        Rect maxFieldRect = new Rect(sliderRect.xMax + 4, position.y, fieldWidth, position.height);

        // Draw min and max float fields
        EditorGUI.BeginChangeCheck();
        float min = EditorGUI.FloatField(minFieldRect, propertyX.floatValue);
        float max = EditorGUI.FloatField(maxFieldRect, propertyY.floatValue);

        // Draw the slider
        EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, range.Min, range.Max);

        // Update the property if changed
        if (EditorGUI.EndChangeCheck())
        {
            max = math.max(max, min);
            propertyX.floatValue = Mathf.Clamp(min, range.Min, range.Max);
            propertyY.floatValue = Mathf.Clamp(max, range.Min, range.Max);
        }

        EditorGUI.EndProperty();
    }
}
