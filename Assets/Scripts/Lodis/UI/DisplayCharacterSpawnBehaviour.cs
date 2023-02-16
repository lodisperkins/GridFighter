using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;

namespace Lodis.AI
{
    public class DisplayCharacterSpawnBehaviour : EntitySpawnBehaviour
    {
        private GameObject _previousCharacterInstance;

        /// <summary>
        /// Creates a new instance of the entity and places it on the grid
        /// </summary>
        /// <param name="entity">The entity to create a new instance of</param>
        /// <param name="position">The position in world space to spawn the entity</param>
        /// <param name="gridAlignment">The side of the grid this entity will belong to</param>
        public override void SpawnEntity(GameObject entity)
        {
            //Try to get the move script attached
            GridMovementBehaviour moveScript = entity.GetComponent<GridMovementBehaviour>();
            if (!moveScript)
            {
                Debug.LogError("You can't spawn a game object that doesn't have a grid movement script. Game object was " + entity.name);
            }
            PanelBehaviour targetPanel;
            moveScript.Alignment = Alignment;

            //Set spawn point and create instance
            BlackBoardBehaviour.Instance.Grid.GetPanel(condition =>
            {
                PanelBehaviour panel = (PanelBehaviour)condition[0];
                return panel.Alignment == moveScript.Alignment;
            }, out targetPanel);

            moveScript.Position = targetPanel.Position;
            Destroy(_previousCharacterInstance);
            _previousCharacterInstance = Instantiate(moveScript.gameObject, null);
            moveScript.gameObject.SetActive(true);


            if (SetAlignmentRotation && moveScript.Alignment == GridAlignment.RIGHT)
                moveScript.transform.rotation = Quaternion.Euler(0, -90, 0);
            else if (SetAlignmentRotation && moveScript.Alignment == GridAlignment.LEFT)
                moveScript.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(DisplayCharacterSpawnBehaviour))]
    [CanEditMultipleObjects]
    public class DisplayCharacterSpawnEditor : Editor
    {
        private SerializedProperty _entity;
        private SerializedProperty _entitySpawnPoint;
        private SerializedProperty _alignment;

        private void Awake()
        {
            _entity = serializedObject.FindProperty("_entity");
            _entitySpawnPoint = serializedObject.FindProperty("_entitySpawnPoint");
            _alignment = serializedObject.FindProperty("_alignment");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("SpawnEntity"))
            {
                EntitySpawnBehaviour.Instance.SpawnEntity((GameObject)_entity.objectReferenceValue, _entitySpawnPoint.vector2Value, (GridAlignment)_alignment.enumValueIndex);
            }
        }
    }


#endif
}
