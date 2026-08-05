using Lodis.UI;
using UnityEditor;
using UnityEngine;

namespace LodisEditor.UI
{
    /// <summary>
    /// Organizes PageManagerBehaviour data into a readable overview while preserving
    /// the original serialized fields and references for normal editing.
    /// </summary>
    [CustomEditor(typeof(PageManagerBehaviour))]
    public class PageManagerBehaviourEditor : Editor
    {
        private SerializedProperty _rootPageProperty;
        private SerializedProperty _eventSystemProperty;
        private SerializedProperty _loadSceneOnFirstPageProperty;
        private SerializedProperty _previousPageOnCancelProperty;
        private SerializedProperty _sceneIndexProperty;
        private SerializedProperty _changePageManuallyProperty;
        private SerializedProperty _useEventSystemSchemeProperty;
        private SerializedProperty _onPreviousPagePressedProperty;

        private bool _showHierarchy = true;
        private bool _showRawData;

        /// <summary>
        /// Caches the serialized fields used by the custom inspector.
        /// </summary>
        private void OnEnable()
        {
            _rootPageProperty = serializedObject.FindProperty("_rootPage");
            _eventSystemProperty = serializedObject.FindProperty("_eventSystem");
            _loadSceneOnFirstPageProperty = serializedObject.FindProperty("_loadSceneOnFirstPage");
            _previousPageOnCancelProperty = serializedObject.FindProperty("_previousPageOnCancel");
            _sceneIndexProperty = serializedObject.FindProperty("_sceneIndex");
            _changePageManuallyProperty = serializedObject.FindProperty("_changePageManually");
            _useEventSystemSchemeProperty = serializedObject.FindProperty("_useEventSystemScheme");
            _onPreviousPagePressedProperty = serializedObject.FindProperty("_onPreviousPagePressed");
        }

        /// <summary>
        /// Draws a structured inspector view with summaries, a page tree, and the
        /// original serialized data folded away for detailed edits.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawRuntimeSummary();
            EditorGUILayout.Space();
            DrawConfigurationSummary();
            EditorGUILayout.Space();
            DrawHierarchySection();
            EditorGUILayout.Space();
            DrawRawDataSection();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Shows the current navigation state while the game is running so page flow
        /// problems can be diagnosed without expanding serialized data.
        /// </summary>
        private void DrawRuntimeSummary()
        {
            PageManagerBehaviour manager = (PageManagerBehaviour)target;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Runtime Summary", EditorStyles.boldLabel);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.LabelField("State", "Enter Play Mode to inspect the active page.");
                    return;
                }

                EditorGUILayout.LabelField("Current Page", GetPageName(manager.CurrentPage));
                EditorGUILayout.LabelField("Root Page", GetPageName(manager.RootPage));
                EditorGUILayout.LabelField("Can Go Back", CanGoBack(manager) ? "Yes" : "No");
                EditorGUILayout.LabelField("Event System Scheme", manager.UseEventSystemScheme ? "Enabled" : "Disabled");
            }
        }

        /// <summary>
        /// Surfaces the top-level PageManagerBehaviour settings in one compact block
        /// so common configuration is easier to scan than the raw inspector order.
        /// </summary>
        private void DrawConfigurationSummary()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_eventSystemProperty);
                EditorGUILayout.PropertyField(_loadSceneOnFirstPageProperty);
                EditorGUILayout.PropertyField(_sceneIndexProperty);
                EditorGUILayout.PropertyField(_previousPageOnCancelProperty);
                EditorGUILayout.PropertyField(_changePageManuallyProperty);
                EditorGUILayout.PropertyField(_useEventSystemSchemeProperty);
                EditorGUILayout.PropertyField(_onPreviousPagePressedProperty);
            }
        }

        /// <summary>
        /// Displays the page graph as a readable tree with key references and event
        /// counts so nested page data can be inspected without opening every array.
        /// </summary>
        private void DrawHierarchySection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showHierarchy = EditorGUILayout.Foldout(_showHierarchy, "Page Hierarchy", true);
                if (!_showHierarchy)
                    return;

                if (_rootPageProperty == null || _rootPageProperty.FindPropertyRelative("PageName") == null)
                {
                    EditorGUILayout.HelpBox("Unable to read the root page data.", MessageType.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(GetPageName(_rootPageProperty)))
                {
                    EditorGUILayout.HelpBox("Assign a root page to see the hierarchy overview.", MessageType.Info);
                    return;
                }

                DrawPageNode(_rootPageProperty, 0);
            }
        }

        /// <summary>
        /// Keeps the original serialized layout available for detailed editing so the
        /// new inspector view does not remove any existing workflow or references.
        /// </summary>
        private void DrawRawDataSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showRawData = EditorGUILayout.Foldout(_showRawData, "Raw Serialized Data", true);
                if (!_showRawData)
                    return;

                EditorGUILayout.PropertyField(_rootPageProperty, true);
            }
        }

        /// <summary>
        /// Recursively draws a single page and its children as an indented tree node.
        /// </summary>
        private void DrawPageNode(SerializedProperty pageProperty, int depth)
        {
            SerializedProperty pageRootProperty = pageProperty.FindPropertyRelative("PageRoot");
            SerializedProperty firstSelectedProperty = pageProperty.FindPropertyRelative("FirstSelected");
            SerializedProperty keepRootVisibleProperty = pageProperty.FindPropertyRelative("KeepRootVisible");
            SerializedProperty pageNameProperty = pageProperty.FindPropertyRelative("PageName");
            SerializedProperty childrenProperty = pageProperty.FindPropertyRelative("_children");
            SerializedProperty goToParentConditionProperty = pageProperty.FindPropertyRelative("GoToParentCondition");
            SerializedProperty goToChildConditionProperty = pageProperty.FindPropertyRelative("GoToChildCondition");
            SerializedProperty onActiveProperty = pageProperty.FindPropertyRelative("OnActive");
            SerializedProperty onInactiveProperty = pageProperty.FindPropertyRelative("OnInactive");
            SerializedProperty onGoToParentProperty = pageProperty.FindPropertyRelative("OnGoToParent");
            SerializedProperty onGoToChildProperty = pageProperty.FindPropertyRelative("OnGoToChild");

            string pageName = pageNameProperty.stringValue;
            if (string.IsNullOrEmpty(pageName))
                pageName = "<Unnamed Page>";

            string header = $"{pageName}  ({childrenProperty.arraySize} child{(childrenProperty.arraySize == 1 ? string.Empty : "ren")})";
            using (new EditorGUI.IndentLevelScope(depth))
            {
                pageProperty.isExpanded = EditorGUILayout.Foldout(pageProperty.isExpanded, header, true);
            }
            if (!pageProperty.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope(depth + 1))
            {
                EditorGUILayout.LabelField("Page Root", GetObjectName(pageRootProperty.objectReferenceValue));
                EditorGUILayout.LabelField("First Selected", GetObjectName(firstSelectedProperty.objectReferenceValue));
                EditorGUILayout.LabelField("Keep Root Visible", keepRootVisibleProperty.boolValue ? "Yes" : "No");
                EditorGUILayout.LabelField("Go To Parent Condition", GetObjectName(goToParentConditionProperty.objectReferenceValue));
                EditorGUILayout.LabelField("Go To Child Condition", GetObjectName(goToChildConditionProperty.objectReferenceValue));
                EditorGUILayout.LabelField("Events", BuildEventSummary(onActiveProperty, onInactiveProperty, onGoToParentProperty, onGoToChildProperty));

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = pageRootProperty.objectReferenceValue != null;
                    if (GUILayout.Button("Ping Page Root"))
                        EditorGUIUtility.PingObject(pageRootProperty.objectReferenceValue);

                    GUI.enabled = firstSelectedProperty.objectReferenceValue != null;
                    if (GUILayout.Button("Ping First Selected"))
                        EditorGUIUtility.PingObject(firstSelectedProperty.objectReferenceValue);

                    GUI.enabled = true;
                }

                for (int i = 0; i < childrenProperty.arraySize; i++)
                {
                    DrawPageNode(childrenProperty.GetArrayElementAtIndex(i), depth + 1);
                }
            }
        }

        /// <summary>
        /// Builds a concise event summary so callback density is visible at a glance.
        /// </summary>
        private static string BuildEventSummary(
            SerializedProperty onActiveProperty,
            SerializedProperty onInactiveProperty,
            SerializedProperty onGoToParentProperty,
            SerializedProperty onGoToChildProperty)
        {
            return
                $"Active: {GetEventCount(onActiveProperty)}, " +
                $"Inactive: {GetEventCount(onInactiveProperty)}, " +
                $"Parent: {GetEventCount(onGoToParentProperty)}, " +
                $"Child: {GetEventCount(onGoToChildProperty)}";
        }

        /// <summary>
        /// Returns the persistent listener count for a serialized UnityEvent.
        /// </summary>
        private static int GetEventCount(SerializedProperty eventProperty)
        {
            SerializedProperty callsProperty = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            return callsProperty?.arraySize ?? 0;
        }

        /// <summary>
        /// Returns the page name for a runtime Page reference, or a readable fallback.
        /// </summary>
        private static string GetPageName(Page page)
        {
            return page?.PageName ?? "<None>";
        }

        /// <summary>
        /// Returns the page name for a serialized Page entry, or a readable fallback.
        /// </summary>
        private static string GetPageName(SerializedProperty pageProperty)
        {
            SerializedProperty pageNameProperty = pageProperty.FindPropertyRelative("PageName");
            if (pageNameProperty == null || string.IsNullOrEmpty(pageNameProperty.stringValue))
                return string.Empty;

            return pageNameProperty.stringValue;
        }

        /// <summary>
        /// Formats an object reference name consistently for the inspector summary.
        /// </summary>
        private static string GetObjectName(Object targetObject)
        {
            return targetObject != null ? targetObject.name : "<None>";
        }

        /// <summary>
        /// Recreates the basic back-navigation state for editor display without
        /// requiring extra runtime API on PageManagerBehaviour.
        /// </summary>
        private static bool CanGoBack(PageManagerBehaviour manager)
        {
            if (manager == null || manager.RootPage == null || manager.CurrentPage == null)
                return false;

            if (manager.CurrentPage.GoToParentCondition?.Value == false)
                return false;

            return manager.CurrentPage.PageParent != null || manager.CurrentPage == manager.RootPage;
        }
    }
}
