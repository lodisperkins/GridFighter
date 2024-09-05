using UnityEngine;
using UnityEditor;
using Types;

[CustomPropertyDrawer(typeof(Fixed32))]
public class Fixed32Drawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the properties
        SerializedProperty rawValueProp = property.FindPropertyRelative("RawValue");

        // Label
        EditorGUI.LabelField(position, label);

        // Calculate the positions for the fields
        Rect floatValueRect = new Rect(position.x, position.y + 30, position.width, 16);
        Rect rawValueRect = new Rect(position.x, position.y + 45, position.width, 16);

        // Convert the raw value to a float and display it
        float floatValue = (float)rawValueProp.longValue / (1 << 16);

        // Draw the float field and update the raw value if the float value changes
        float newFloatValue = EditorGUI.FloatField(floatValueRect, "Float Value", floatValue);

        if (newFloatValue != floatValue)
        {
            rawValueProp.longValue = (long)(newFloatValue * (1 << 16));
        }

        // Display the raw value as read-only
        EditorGUI.LabelField(rawValueRect, $"Raw Value: {rawValueProp.longValue}");
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) + 36; // Adjust height to fit both fields
    }
}
