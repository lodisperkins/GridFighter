using UnityEditor;
using UnityEngine;
using FixedPoints;

[CustomPropertyDrawer(typeof(FTransform))]
public class FTransformDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Indent the fields to match the Transform layout
        EditorGUI.indentLevel++;

        // Get the properties for _worldPosition, _worldRotation, and _worldScale
        SerializedProperty positionProp = property.FindPropertyRelative("_localPosition");
        SerializedProperty rotationProp = property.FindPropertyRelative("_localRotation");
        SerializedProperty scaleProp = property.FindPropertyRelative("_worldScale");

        // Calculate positions for the fields (avoiding label overlap)
        Rect posRect = new Rect(position.x, position.y, position.width, 16);
        Rect rotRect = new Rect(position.x, position.y + 18, position.width, 16);
        Rect scaleRect = new Rect(position.x, position.y + 36, position.width, 16);

        // Detect right-click to show context menu
        if (Event.current.type == EventType.ContextClick)
        {
            Vector2 mousePos = Event.current.mousePosition;
            if (position.Contains(mousePos))
            {
                ShowContextMenu(property);
                Event.current.Use(); // Consume the event to prevent other handlers
            }
        }

        // Draw each vector (position, rotation in degrees, and scale) as float fields
        DrawFVector3Field(posRect, positionProp, "Position");
        DrawFQuaternionField(rotRect, rotationProp, "Rotation (Degrees)");  // Display rotation in degrees
        DrawFVector3Field(scaleRect, scaleProp, "Scale");

        EditorGUI.indentLevel--;
    }

    private void ShowContextMenu(SerializedProperty property)
    {
        // Create a menu with options
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Copy From Unity Transform (Global)"), false, () => CopyFromUnityTransform(property));
        menu.AddItem(new GUIContent("Copy From Unity Transform (Local)"), false, () => CopyFromUnityTransformLocal(property));
        menu.ShowAsContext();
    }

    private void CopyFromUnityTransform(SerializedProperty property)
    {
        // Get the currently selected GameObject in the scene
        if (Selection.activeTransform != null)
        {
            Transform unityTransform = Selection.activeTransform;

            // Convert local transform to global transform
            Vector3 globalPosition = unityTransform.position;
            Quaternion globalRotation = unityTransform.rotation;
            Vector3 globalScale = unityTransform.lossyScale; // Using lossyScale to get global scale

            // Get the properties of FTransform to update them
            SerializedProperty positionProp = property.FindPropertyRelative("_localPosition");
            SerializedProperty rotationProp = property.FindPropertyRelative("_localRotation");
            SerializedProperty scaleProp = property.FindPropertyRelative("_worldScale");

            // Copy the global position, rotation, and scale to the FTransform
            CopyVector3ToFVector3(globalPosition, positionProp);
            CopyQuaternionToFQuaternion(globalRotation, rotationProp);
            CopyVector3ToFVector3(globalScale, scaleProp);

            // Apply the changes to the SerializedObject
            property.serializedObject.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("No active Unity Transform selected.");
        }
    }

    private void CopyFromUnityTransformLocal(SerializedProperty property)
    {
        // Get the currently selected GameObject in the scene
        if (Selection.activeTransform != null)
        {
            Transform unityTransform = Selection.activeTransform;

            // Convert local transform to global transform
            Vector3 globalPosition = unityTransform.localPosition;
            Quaternion globalRotation = unityTransform.localRotation;
            Vector3 globalScale = unityTransform.localScale; // Using lossyScale to get global scale

            // Get the properties of FTransform to update them
            SerializedProperty positionProp = property.FindPropertyRelative("_localPosition");
            SerializedProperty rotationProp = property.FindPropertyRelative("_localRotation");
            SerializedProperty scaleProp = property.FindPropertyRelative("_worldScale");

            // Copy the global position, rotation, and scale to the FTransform
            CopyVector3ToFVector3(globalPosition, positionProp);
            CopyQuaternionToFQuaternion(globalRotation, rotationProp);
            CopyVector3ToFVector3(globalScale, scaleProp);

            // Apply the changes to the SerializedObject
            property.serializedObject.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("No active Unity Transform selected.");
        }
    }

    private void CopyVector3ToFVector3(Vector3 unityVec, SerializedProperty fVectorProp)
    {
        // Convert Unity Vector3 to Fixed32 FVector3 and copy it to the SerializedProperty
        SerializedProperty xProp = fVectorProp.FindPropertyRelative("X.RawValue");
        SerializedProperty yProp = fVectorProp.FindPropertyRelative("Y.RawValue");
        SerializedProperty zProp = fVectorProp.FindPropertyRelative("Z.RawValue");

        xProp.longValue = (long)(unityVec.x * (1 << 16));
        yProp.longValue = (long)(unityVec.y * (1 << 16));
        zProp.longValue = (long)(unityVec.z * (1 << 16));
    }

    private void CopyQuaternionToFQuaternion(Quaternion unityQuat, SerializedProperty fQuatProp)
    {
        // Convert Unity Quaternion to Fixed32 FQuaternion and copy it to the SerializedProperty
        SerializedProperty xProp = fQuatProp.FindPropertyRelative("X.RawValue");
        SerializedProperty yProp = fQuatProp.FindPropertyRelative("Y.RawValue");
        SerializedProperty zProp = fQuatProp.FindPropertyRelative("Z.RawValue");
        SerializedProperty wProp = fQuatProp.FindPropertyRelative("W.RawValue");

        xProp.longValue = (long)(unityQuat.x * (1 << 16));
        yProp.longValue = (long)(unityQuat.y * (1 << 16));
        zProp.longValue = (long)(unityQuat.z * (1 << 16));
        wProp.longValue = (long)(unityQuat.w * (1 << 16));
    }

    private void DrawFVector3Field(Rect position, SerializedProperty vectorProp, string label)
    {
        // Get the Fixed32 X, Y, Z components from FVector3
        SerializedProperty xProp = vectorProp.FindPropertyRelative("X.RawValue");
        SerializedProperty yProp = vectorProp.FindPropertyRelative("Y.RawValue");
        SerializedProperty zProp = vectorProp.FindPropertyRelative("Z.RawValue");

        // Convert the raw values to floats for display
        float xValue = (float)xProp.longValue / (1 << 16);
        float yValue = (float)yProp.longValue / (1 << 16);
        float zValue = (float)zProp.longValue / (1 << 16);

        // Draw the fields and update the Fixed32 values
        EditorGUI.BeginChangeCheck();
        Vector3 floatVector = EditorGUI.Vector3Field(position, label, new Vector3(xValue, yValue, zValue));
        if (EditorGUI.EndChangeCheck())
        {
            xProp.longValue = (long)(floatVector.x * (1 << 16));
            yProp.longValue = (long)(floatVector.y * (1 << 16));
            zProp.longValue = (long)(floatVector.z * (1 << 16));
        }
    }

    private void DrawFQuaternionField(Rect position, SerializedProperty quaternionProp, string label)
    {
        // Get the Fixed32 X, Y, Z, W components from FQuaternion
        SerializedProperty xProp = quaternionProp.FindPropertyRelative("X.RawValue");
        SerializedProperty yProp = quaternionProp.FindPropertyRelative("Y.RawValue");
        SerializedProperty zProp = quaternionProp.FindPropertyRelative("Z.RawValue");
        SerializedProperty wProp = quaternionProp.FindPropertyRelative("W.RawValue");

        // Convert quaternion to Euler angles in degrees for display
        Quaternion unityQuat = new Quaternion(
            (float)xProp.longValue / (1 << 16),
            (float)yProp.longValue / (1 << 16),
            (float)zProp.longValue / (1 << 16),
            (float)wProp.longValue / (1 << 16)
        );
        Vector3 eulerAngles = unityQuat.eulerAngles;

        // Draw the fields and update the FQuaternion values in degrees
        EditorGUI.BeginChangeCheck();
        Vector3 newEulerAngles = EditorGUI.Vector3Field(position, label, eulerAngles);
        if (EditorGUI.EndChangeCheck())
        {
            Quaternion newQuat = Quaternion.Euler(newEulerAngles);

            xProp.longValue = (long)(newQuat.x * (1 << 16));
            yProp.longValue = (long)(newQuat.y * (1 << 16));
            zProp.longValue = (long)(newQuat.z * (1 << 16));
            wProp.longValue = (long)(newQuat.w * (1 << 16));
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) + 54; // Adjust height for the added fields
    }
}
