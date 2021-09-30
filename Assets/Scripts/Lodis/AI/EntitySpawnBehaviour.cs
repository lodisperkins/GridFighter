using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Lodis.AI
{
    public class EntitySpawnBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _entityMovementScript;
        [SerializeField]
        private Vector2 _entitySpawnPoint;
        [SerializeField]
        private GridAlignment _alignment;

        public static void SpawnEntity(GameObject entity, Vector2 position, GridAlignment gridAlignment = GridAlignment.ANY)
        {
            GridMovementBehaviour moveScript = entity.GetComponent<GridMovementBehaviour>();
            if (!moveScript)
            {
                Debug.LogError("You can't spawn a game object that doesn't have a grid movement script. Game object was " + entity.name);
            }
            moveScript.Position = position;
            Instantiate(moveScript.gameObject, null);

            moveScript.Alignment = gridAlignment;
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(EntitySpawnBehaviour))]
    [CanEditMultipleObjects]
    public class EntitySpawnEditor : Editor
    {
        private SerializedProperty _entity;
        private SerializedProperty _entitySpawnPoint;
        private SerializedProperty _alignment;

        private void Awake()
        {
            _entity = serializedObject.FindProperty("_entityMovementScript");
            _entitySpawnPoint = serializedObject.FindProperty("_entitySpawnPoint");
            _alignment = serializedObject.FindProperty("_alignment");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("SpawnEntity"))
            {
                EntitySpawnBehaviour.SpawnEntity((GameObject)_entity.objectReferenceValue, _entitySpawnPoint.vector2Value, (GridAlignment)_alignment.enumValueIndex);
            }
        }
    }


#endif
}
