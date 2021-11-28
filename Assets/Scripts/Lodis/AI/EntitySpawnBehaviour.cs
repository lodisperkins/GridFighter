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
        [Tooltip("The reference to the entity to spawn")]
        [SerializeField]
        private GameObject _entity;
        [Tooltip("The position in world space to spawn the entity")]
        [SerializeField]
        private Vector2 _entitySpawnPoint;
        [Tooltip("The side of the grid this entity will belong to")]
        [SerializeField]
        private GridAlignment _alignment;

        /// <summary>
        /// Creates a new instance of the entity and places it on the grid
        /// </summary>
        /// <param name="entity">The entity to create a new instance of</param>
        /// <param name="position">The position in world space to spawn the entity</param>
        /// <param name="gridAlignment">The side of the grid this entity will belong to</param>
        public static void SpawnEntity(GameObject entity, Vector2 position, GridAlignment gridAlignment = GridAlignment.ANY)
        {
            //Try to get the move script attached
            GridMovementBehaviour moveScript = entity.GetComponent<GridMovementBehaviour>();
            if (!moveScript)
            {
                Debug.LogError("You can't spawn a game object that doesn't have a grid movement script. Game object was " + entity.name);
            }

            //Set spawn point and create instance
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
