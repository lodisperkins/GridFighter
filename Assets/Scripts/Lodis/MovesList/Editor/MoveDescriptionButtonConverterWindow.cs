using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Lodis.UI;
using System.Collections.Generic;

namespace Lodis.MovesList.Editor
{
    public class MoveDescriptionButtonConverterWindow : EditorWindow
    {
        private List<MoveDescriptionBehaviour> _moveDescriptionButtons = new List<MoveDescriptionBehaviour>();
        private Sprite[] _sharedBackgroundImages = new Sprite[4];
        private Vector2 _scrollPosition;
        private const float ICON_SIZE = 50f;

        [MenuItem("Tools/Lodis/Move Description Button Converter")]
        public static void ShowWindow()
        {
            var window = GetWindow<MoveDescriptionButtonConverterWindow>("Button Converter");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            RefreshButtonList();
        }

        private void RefreshButtonList()
        {
            _moveDescriptionButtons.Clear();

            // Find all MoveDescriptionBehaviour components in the scene
            MoveDescriptionBehaviour[] buttons = FindObjectsOfType<MoveDescriptionBehaviour>(true);
            foreach (var button in buttons)
            {
                _moveDescriptionButtons.Add(button);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Move Description Button Converter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool converts buttons with MoveDescriptionBehaviour to use a separate background and icon image setup.\n\n" +
                "- Background Image: Uses the existing Image component on the button\n" +
                "- Icon Image: Creates a new child Image with 50x50 RectTransform\n" +
                "- Set the shared Background Images below (applied to all buttons)",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Shared background images section
            EditorGUILayout.LabelField("Background Images (shared by all buttons):", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            int newSize = EditorGUILayout.IntField("Array Size", _sharedBackgroundImages.Length);
            if (newSize != _sharedBackgroundImages.Length && newSize > 0)
            {
                System.Array.Resize(ref _sharedBackgroundImages, newSize);
            }

            for (int i = 0; i < _sharedBackgroundImages.Length; i++)
            {
                _sharedBackgroundImages[i] = (Sprite)EditorGUILayout.ObjectField(
                    $"  Element {i}",
                    _sharedBackgroundImages[i],
                    typeof(Sprite),
                    false);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Refresh Button List", GUILayout.Height(25)))
            {
                RefreshButtonList();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Found {_moveDescriptionButtons.Count} MoveDescriptionBehaviour(s)", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _moveDescriptionButtons.Count; i++)
            {
                var button = _moveDescriptionButtons[i];
                if (button == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.ObjectField(button, typeof(MoveDescriptionBehaviour), true);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = button.gameObject;
                }

                if (GUILayout.Button("Convert", GUILayout.Width(60)))
                {
                    ConvertButton(button, _sharedBackgroundImages);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Convert All Buttons", GUILayout.Height(35)))
            {
                ConvertAllButtons();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ConvertButton(MoveDescriptionBehaviour moveDescription, Sprite[] backgroundSprites)
        {
            if (moveDescription == null)
            {
                Debug.LogWarning("MoveDescriptionBehaviour is null, skipping conversion.");
                return;
            }

            GameObject buttonObject = moveDescription.gameObject;

            Undo.RegisterCompleteObjectUndo(buttonObject, "Convert Move Description Button");

            // Get the existing Image component on the button (this becomes the background)
            Image existingImage = buttonObject.GetComponent<Image>();
            if (existingImage == null)
            {
                Debug.LogWarning($"No Image component found on {buttonObject.name}, skipping.");
                return;
            }

            // Use SerializedObject to access private fields
            SerializedObject serializedMoveDesc = new SerializedObject(moveDescription);
            SerializedProperty backgroundImageProp = serializedMoveDesc.FindProperty("_backgroundImage");
            SerializedProperty iconImageProp = serializedMoveDesc.FindProperty("_iconImage");
            SerializedProperty backgroundSpritesProp = serializedMoveDesc.FindProperty("_backgroundSprites");

            // Set the background image to the existing image component
            backgroundImageProp.objectReferenceValue = existingImage;

            // Check if an icon image child already exists
            Image iconImage = null;
            Transform existingIconTransform = buttonObject.transform.Find("IconImage");

            if (existingIconTransform != null)
            {
                iconImage = existingIconTransform.GetComponent<Image>();
            }

            if (iconImage == null)
            {
                // Create a new child GameObject for the icon
                GameObject iconObject = new GameObject("IconImage");
                Undo.RegisterCreatedObjectUndo(iconObject, "Create Icon Image");

                iconObject.transform.SetParent(buttonObject.transform, false);

                // Add RectTransform and set size to 50x50
                RectTransform iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
                iconRect.anchoredPosition = Vector2.zero;

                // Add Image component
                iconImage = iconObject.AddComponent<Image>();
                iconImage.raycastTarget = false;
            }

            // Set the icon image reference
            iconImageProp.objectReferenceValue = iconImage;

            // Set the background sprites array if provided
            if (backgroundSprites != null && backgroundSprites.Length > 0)
            {
                backgroundSpritesProp.arraySize = backgroundSprites.Length;
                for (int i = 0; i < backgroundSprites.Length; i++)
                {
                    backgroundSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = backgroundSprites[i];
                }
            }

            serializedMoveDesc.ApplyModifiedProperties();

            EditorUtility.SetDirty(moveDescription);
            EditorUtility.SetDirty(buttonObject);

            Debug.Log($"Successfully converted button: {buttonObject.name}");
        }

        private void ConvertAllButtons()
        {
            int convertedCount = 0;

            for (int i = 0; i < _moveDescriptionButtons.Count; i++)
            {
                if (_moveDescriptionButtons[i] != null)
                {
                    ConvertButton(_moveDescriptionButtons[i], _sharedBackgroundImages);
                    convertedCount++;
                }
            }

            Debug.Log($"Converted {convertedCount} button(s).");

            // Refresh the list after conversion
            RefreshButtonList();
        }
    }
}
