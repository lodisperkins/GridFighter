using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Can be attached to a GameObject to create debug buttons in the Inspector for testing purposes.
/// </summary>
public class ButtonDebug : MonoBehaviour
{
    [Serializable]
    public class DebugButton
    {
        public string buttonName = "New Button";
        public UnityEvent onClick = new UnityEvent();
    }

    public List<DebugButton> buttons = new List<DebugButton>();
}

#if UNITY_EDITOR
[CustomEditor(typeof(ButtonDebug))]
public class ButtonDebugEditor : Editor
{
    private SerializedProperty buttonsProp;
    private string newButtonName = "";

    private void OnEnable()
    {
        buttonsProp = serializedObject.FindProperty("buttons");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Set up section for adding new buttons
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add New Debug Button", EditorStyles.boldLabel);
        newButtonName = EditorGUILayout.TextField("Button Name", newButtonName);

        if (GUILayout.Button("Add Button"))
        {
            if (!string.IsNullOrWhiteSpace(newButtonName))
            {
                AddNewButton(newButtonName);
                newButtonName = "";
            }
            else
            {
                EditorGUILayout.HelpBox("Button name cannot be empty.", MessageType.Warning);
            }
        }

        // Display existing buttons

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Debug Buttons", EditorStyles.boldLabel);

        for (int i = 0; i < buttonsProp.arraySize; i++)
        {
            // Display each button's properties
            SerializedProperty buttonProp = buttonsProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = buttonProp.FindPropertyRelative("buttonName");
            SerializedProperty eventProp = buttonProp.FindPropertyRelative("onClick");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            nameProp.stringValue = EditorGUILayout.TextField("Name", nameProp.stringValue);

            // Delete button for the debug button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                buttonsProp.DeleteArrayElementAtIndex(i);
                break;
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(eventProp);

            // Invoke button for testing
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button($"Invoke \"{nameProp.stringValue}\""))
            {
                ((ButtonDebug)target).buttons[i].onClick.Invoke();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddNewButton(string name)
    {
        buttonsProp.arraySize++;
        SerializedProperty newButtonProp = buttonsProp.GetArrayElementAtIndex(buttonsProp.arraySize - 1);

        SerializedProperty nameProp = newButtonProp.FindPropertyRelative("buttonName");
        nameProp.stringValue = name;
    }
}
#endif