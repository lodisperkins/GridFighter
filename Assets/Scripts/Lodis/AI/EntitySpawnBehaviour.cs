using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using FixedPoints;

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
        [Tooltip("Will rotate the entity to face opponent side if enabled. Rotation in the grid movement behaviour for the entity will override this.")]
        [SerializeField]
        private bool _setAlignmentRotation;
        [SerializeField]
        private UnityEvent _onEntitySpawn = new UnityEvent();
        private static EntitySpawnBehaviour _instance;

        public  bool SetAlignmentRotation { get => _setAlignmentRotation; set => _setAlignmentRotation = value; }
        /// <s
        /// ummary>
        /// Gets the static instance of the sound manager. Creates one if none exists
        /// </summary>
        public static EntitySpawnBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(EntitySpawnBehaviour)) as EntitySpawnBehaviour;

                if (!_instance)
                {
                    GameObject blackBoard = new GameObject("EntitySpawn");
                    _instance = blackBoard.AddComponent<EntitySpawnBehaviour>();
                }

                return _instance;
            }
        }

        public GridAlignment Alignment { get => _alignment; set => _alignment = value; }
        protected UnityEvent OnEntitySpawn { get => _onEntitySpawn; }

        public void AddOnSpawnEvent(UnityAction action)
        {
            OnEntitySpawn.AddListener(action);
        }
        /// <summary>
        /// Creates a new instance of the entity and places it on the grid
        /// </summary>
        /// <param name="entity">The entity to create a new instance of</param>
        /// <param name="position">The position in world space to spawn the entity</param>
        /// <param name="gridAlignment">The side of the grid this entity will belong to</param>
        public void SpawnEntity(GameObject entity, FVector2 position, GridAlignment gridAlignment = GridAlignment.ANY)
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
            moveScript.gameObject.SetActive(true);

            moveScript.Alignment = gridAlignment;

            if (SetAlignmentRotation && moveScript.Alignment == GridAlignment.RIGHT)
                moveScript.transform.rotation = Quaternion.Euler(0, -90, 0);
            else if (SetAlignmentRotation && moveScript.Alignment == GridAlignment.LEFT)
                moveScript.transform.rotation = Quaternion.Euler(0, 90, 0);

            OnEntitySpawn?.Invoke();
        }

        /// <summary>
        /// Creates a new instance of the entity and places it on the grid
        /// </summary>
        /// <param name="entity">The entity to create a new instance of</param>
        /// <param name="position">The position in world space to spawn the entity</param>
        /// <param name="gridAlignment">The side of the grid this entity will belong to</param>
        public virtual void SpawnEntity(GameObject entity)
        {
            //Try to get the move script attached
            GridMovementBehaviour moveScript = entity.GetComponent<GridMovementBehaviour>();
            if (!moveScript)
            {
                Debug.LogError("You can't spawn a game object that doesn't have a grid movement script. Game object was " + entity.name);
            }
            PanelBehaviour targetPanel;

            //Set spawn point and create instance
            BlackBoardBehaviour.Instance.Grid.GetPanel(condition =>
            {
                PanelBehaviour panel = (PanelBehaviour)condition[0];
                return panel.Alignment == moveScript.Alignment;
            }, out targetPanel);

            moveScript.Position = targetPanel.Position;
            Instantiate(moveScript.gameObject, null);
            moveScript.gameObject.SetActive(true);

            moveScript.Alignment = Alignment;

            if (SetAlignmentRotation && moveScript.Alignment == GridAlignment.RIGHT)
                moveScript.transform.rotation = Quaternion.Euler(0, -90, 0);
            else if (SetAlignmentRotation && moveScript.Alignment == GridAlignment.LEFT)
                moveScript.transform.rotation = Quaternion.Euler(0, 90, 0);
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
            _entity = serializedObject.FindProperty("_entity");
            _entitySpawnPoint = serializedObject.FindProperty("_entitySpawnPoint");
            _alignment = serializedObject.FindProperty("_alignment");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("SpawnEntity"))
            {
                Debug.LogError("Can't spawn entities as spawning has not been converted to used fixed points.");
                //EntitySpawnBehaviour.Instance.SpawnEntity((GameObject)_entity.objectReferenceValue, _entitySpawnPoint.vector2Value, (GridAlignment)_alignment.enumValueIndex);
            }
        }
    }


#endif
}
