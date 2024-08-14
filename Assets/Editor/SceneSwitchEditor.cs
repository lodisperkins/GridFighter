using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class SceneSwitcherEditor : EditorWindow
{
    private string[] sceneNames;
    private int selectedSceneIndex;

    [MenuItem("Tools/Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcherEditor>("Scene Switcher");
    }

    private void OnEnable()
    {
        // Load all scene names in the project
        LoadScenes();
    }

    private void LoadScenes()
    {
        int sceneCount = EditorBuildSettings.scenes.Length;
        sceneNames = new string[sceneCount];

        for (int i = 0; i < sceneCount; i++)
        {
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[i].path);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Select a Scene to Load", EditorStyles.boldLabel);

        if (sceneNames == null || sceneNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No scenes found in Build Settings!", MessageType.Warning);
            return;
        }

        selectedSceneIndex = EditorGUILayout.Popup("Scenes", selectedSceneIndex, sceneNames);

        if (GUILayout.Button("Load Scene"))
        {
            LoadSelectedScene();
        }
    }

    private void LoadSelectedScene()
    {
        if (selectedSceneIndex >= 0 && selectedSceneIndex < sceneNames.Length)
        {
            string scenePath = EditorBuildSettings.scenes[selectedSceneIndex].path;
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
