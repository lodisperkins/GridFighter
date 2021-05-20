using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridGame.VariableScripts;
using Lodis.Gameplay;

namespace Lodis.GridScripts
{
    /// <summary>
    /// Used to label which side of the grid an object belongs to
    /// </summary>
    public enum GridAlignment
    {
        NONE,
        LEFT,
        RIGHT,
        ANY
    }


    public class GridBehaviour : MonoBehaviour
    {
        [Tooltip("The grid will use this game object to create the panels. MUST HAVE A PANEL BEHAVIOUR ATTACHED")]
        [SerializeField]
        private GameObject _panelRef;
        [Tooltip("The grid will use this game object to create the barriers. MUST HAVE A GRID MOVEMENT SCRIPT ATTACHED")]
        [SerializeField]
        private GameObject _barrierRef;
        [Tooltip("The dimensions to use when building the grid.")]
        [SerializeField]
        private Vector2 _dimensions;
        [Tooltip("The amount of space that should be between each panel.")]
        [SerializeField]
        private float _panelSpacing;
        private PanelBehaviour[,] _panels;
        [Tooltip("How many columns to give player 1 when the game starts. Use this to decide how much territory to give both players.")]
        [SerializeField]
        private int _p1MaxColumns;
        [Tooltip("The coordinates of each barrier on the left hand side.")]
        [SerializeField]
        private Vector2[] _lhsBarrierPositions;
        [Tooltip("The coordinates of each barrier on the right hand side.")]
        [SerializeField]
        private Vector2[] _rhsBarrierPositions;
        [SerializeField]
        private GameObject _collisionPlaneRef;
        private PanelBehaviour _lhsPlayerSpawnPanel;
        private PanelBehaviour _rhsPlayerSpawnPanel;
        private List<BarrierBehaviour> _barriers;

        public PanelBehaviour LhsSpawnPanel
        {
            get
            {
                return _lhsPlayerSpawnPanel;
            }
        }

        public PanelBehaviour RhsSpawnPanel
        {
            get
            {
                return _rhsPlayerSpawnPanel;
            }
        }

        public GameObject PanelRef
        {
            get
            {
                return _panelRef;
            }
        }

        public float PanelSpacing
        {
            get
            {
                return _panelSpacing;
            }
        }

        public Vector2 Dimensions
        {
            get 
            {
                return _dimensions;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            DestroyTempPanels();
            _barriers = new List<BarrierBehaviour>();
        }

        /// <summary>
        /// Creates a grid using the given dimensions and spacing.
        /// </summary>
        public void CreateGrid()
        {
            if (!Gameplay.BlackBoardBehaviour.Grid)
                Gameplay.BlackBoardBehaviour.InitializeGrid();

            _panels = new PanelBehaviour[(int)_dimensions.x, (int)_dimensions.y];

            //The world spawn position for each gameobject in the grid
            Vector3 spawnPosition = transform.position;

            //The x and y position for each game object in the grid
            int xPos = 0;
            int yPos = 0;
            for (int i = 0; i < (int)_dimensions.x * (int)_dimensions.y; i++)
            {
                GameObject panel = (Instantiate(_panelRef, spawnPosition, new Quaternion(), transform));
                
                _panels[xPos, yPos] = panel.GetComponent<PanelBehaviour>();
                _panels[xPos, yPos].Position = new Vector2(xPos, yPos);
                //If the x position in the grid is equal to the given x dimension,
                //reset x position to be 0, and increase the y position.
                if (xPos == (int)_dimensions.x - 1)
                {
                    xPos = 0;
                    spawnPosition.x = transform.position.x;
                    yPos++;
                    spawnPosition.z += _panelRef.transform.localScale.z + _panelSpacing;
                    continue;
                }

                //Increase x position
                xPos++;
                spawnPosition.x += _panelRef.transform.localScale.x + _panelSpacing;
            }


            //After the grid is created, assign each panel a side.
            SetDefaultPanelAlignments();
            //Spawn barriers on both side and find the players' spawn location
            SpawnBarriers();

            //If the lhs spawn still hasn't been assigned, set it to be the first open panel in the list
            if (!_lhsPlayerSpawnPanel)
            {
                for (int x = 0; x < _p1MaxColumns; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        GetPanel(x, y, out _lhsPlayerSpawnPanel, false, GridAlignment.LEFT);
                    }
                    if (_lhsPlayerSpawnPanel)
                    {
                        break;
                    }
                }
            }

            //If the rhs spawn still hasn't been assigned, set it to be the first open panel in the list
            if (!_rhsPlayerSpawnPanel)
            {
                for (int x = (int)_dimensions.x; x > _dimensions.x - _p1MaxColumns; x--)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        GetPanel(x, y, out _rhsPlayerSpawnPanel, false, GridAlignment.RIGHT);
                    }
                    if (_rhsPlayerSpawnPanel)
                    {
                        break;
                    }
                }
            }

            GameObject collisionPlane = Instantiate(_collisionPlaneRef, transform);

            float collisionPlaneScaleX = (_dimensions.x * _panelRef.transform.localScale.x) + (PanelSpacing * _dimensions.x);
            float collisionPlaneScaleY = (_dimensions.y * _panelRef.transform.localScale.z) + (PanelSpacing* _dimensions.y);

            float collisionPlaneOffsetX = ((_dimensions.x - 1) * _panelRef.transform.localScale.x) + (PanelSpacing * (_dimensions.x - 1));
            float collisionPlaneOffsetY = ((_dimensions.y - 1) * _panelRef.transform.localScale.z) + (PanelSpacing* (_dimensions.y - 1));

            collisionPlane.transform.localScale = new Vector3(collisionPlaneScaleX / 10, collisionPlane.transform.localScale.y, collisionPlaneScaleY / 10);
            collisionPlane.transform.position += new Vector3(collisionPlaneOffsetX / 2, 0, collisionPlaneOffsetY / 2);
        }

        /// <summary>
        /// Destroys panels that may have existed before the game starts
        /// </summary>
        public void DestroyTempPanels()
        {
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Destroys all panels in the grid. (Only for use in editor)
        /// </summary>
        public void DestroyTempPanelsInEditor()
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        /// <summary>
        /// Destroys all panels in the grid.
        /// </summary>
        public void DestroyGrid()
        {
            if (_panels == null)
                return;

            transform.DetachChildren();

            for (int i = 0; i < _dimensions.x; i++)
            {
                for (int j = 0; j < _dimensions.y; j++)
                {
                    Destroy(_panels[i, j].gameObject);
                }
            }
            _panels = null;
        }

        /// <summary>
        /// Labels each panel to be on either the left side on the right side of the grid based on 
        /// the value given for the maximum columns for player1.
        /// </summary>
        public void SetDefaultPanelAlignments()
        {
            //Clamp the amount of columns
            if (_p1MaxColumns > _dimensions.x)
                _p1MaxColumns = (int)_dimensions.x;

            //Loops through the list of panels and sets their label based on position.
            foreach (PanelBehaviour panel in _panels)
            {
                if (panel.Position.x < _p1MaxColumns)
                {
                    panel.Alignment = GridAlignment.LEFT;
                }
                else
                {
                    panel.Alignment = GridAlignment.RIGHT;
                }
            }
        }

        /// <summary>
        /// Spawns barriers at the given barrier positions for both sides.
        /// </summary>
        private void SpawnBarriers()
        {
            //Spawns barriers for the left side
            foreach (Vector2 position in _lhsBarrierPositions)
            {
                GameObject barrierObject = null;
                PanelBehaviour spawnPanel = null;

                if (GetPanel(position, out spawnPanel, false))
                {
                    Vector3 spawnPosition = new Vector3(spawnPanel.transform.position.x, spawnPanel.transform.position.y + _barrierRef.transform.localScale.y / 2, spawnPanel.transform.position.z);
                    barrierObject = Instantiate(_barrierRef, spawnPosition, new Quaternion(), transform);

                    //Searches for a potential player spawn position behind the barrier
                    Vector2 potentialPlayerSpawn = new Vector2(position.x - 1, position.y);
                    if (!_lhsPlayerSpawnPanel)
                        GetPanel(potentialPlayerSpawn, out _lhsPlayerSpawnPanel, false, GridAlignment.LEFT);
                }

                _barriers.Add(barrierObject.GetComponent<BarrierBehaviour>());
                Movement.GridMovementBehaviour movement = barrierObject.GetComponent<Movement.GridMovementBehaviour>();
                movement.MoveToPanel(spawnPanel.Position);
                movement.Alignment = GridAlignment.LEFT;
            }
            //Spawns barriers for the right side
            foreach (Vector2 position in _rhsBarrierPositions)
            {
                GameObject barrierObject = null;
                PanelBehaviour spawnPanel = null;

                if (GetPanel(position, out spawnPanel, false))
                {
                    Vector3 spawnPosition = new Vector3(spawnPanel.transform.position.x, spawnPanel.transform.position.y + _barrierRef.transform.localScale.y / 2, spawnPanel.transform.position.z);
                    barrierObject = Instantiate(_barrierRef, spawnPosition, new Quaternion(), transform);

                    //Searches for a potential player spawn position behind the barrier
                    Vector2 potentialPlayerSpawn = new Vector2(position.x + 1, position.y);
                    if (!_rhsPlayerSpawnPanel)
                        GetPanel(potentialPlayerSpawn, out _rhsPlayerSpawnPanel, false, GridAlignment.RIGHT);
                }

                _barriers.Add(barrierObject.GetComponent<BarrierBehaviour>());
                Movement.GridMovementBehaviour movement = barrierObject.GetComponent<Movement.GridMovementBehaviour>();
                movement.MoveToPanel(spawnPanel.Position);
                movement.Alignment = GridAlignment.RIGHT;
            }
        }

        public void AssignOwners(string lhsOwnerName, string rhsOwnerName = "")
        {
            foreach (BarrierBehaviour barrier in _barriers)
            {
                if (barrier.GetComponent<Movement.GridMovementBehaviour>().Alignment == GridAlignment.LEFT)
                    barrier.Owner = lhsOwnerName;
                else if (barrier.GetComponent<Movement.GridMovementBehaviour>().Alignment == GridAlignment.RIGHT)
                    barrier.Owner = rhsOwnerName;
            }

        }

        /// <summary>
        /// Set the alignment of the panels in the given range to the given alignment.
        /// </summary>
        /// <param name="min">The farthest left column to change.(Inclusive)</param>
        /// <param name="max">The farthest right column to change.(Inclusive)</param>
        /// <param name="alignment">The new alingment for the panels.</param>
        public void SetPanelAlignmentInRange(int min, int max, GridAlignment alignment)
        {
            if (min < 0)
                min = 0;

            if (max > _dimensions.x)
                max = (int)_dimensions.x;

            foreach (PanelBehaviour panel in _panels)
            {
                if (panel.Position.x >= min && panel.Position.x <= max)
                {
                    panel.Alignment = alignment;
                }
            }
        }

        /// <summary>
        /// Finds and outputs the panel at the given location.
        /// </summary>
        /// <param name="x">The x position  of the panel.</param>
        /// <param name="y">The y position of the panel.</param>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        /// <param name="alignment">Will return false if the panel found doesn't match this alignment.</param>
        /// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        public bool GetPanel(int x, int y, out PanelBehaviour panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        {
            panel = null;

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (x < 0 || x >= _dimensions.x || y < 0 || y >= _dimensions.y)
                return false;
            else if (!canBeOccupied && _panels[x, y].Occupied)
                return false;
            else if (_panels[x, y].Alignment != alignment && alignment != GridAlignment.ANY)
                return false;

            panel = _panels[x, y];

            return true;
        }


        /// <summary>
        /// Finds and outputs the panel at the given location.
        /// </summary>
        /// <param name="position">The position of the panel on the grid.</param>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        /// <param name="alignment">Will return false if the panel found doesn't match this alignment.</param>
        /// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        public bool GetPanel(Vector2 position, out PanelBehaviour panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        { 
            panel = null;

            if (_panels == null)
                return false;

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (position.x < 0 || position.x >= _dimensions.x || position.y < 0 || position.y >= _dimensions.y || float.IsNaN(position.x) || float.IsNaN(position.y))
                return false;
            else if (!canBeOccupied && _panels[(int)position.x, (int)position.y].Occupied)
                return false;
            else if (_panels[(int)position.x, (int)position.y].Alignment != alignment && alignment != GridAlignment.ANY)
                return false;

            panel = _panels[(int)position.x, (int)position.y];

            return true;
        }

        public bool GetPanelAtLocationInWorld(Vector3 location, out PanelBehaviour panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        {
            panel = null;

            if (_panels == null)
                return false;

            int x = Mathf.RoundToInt((location.x / (PanelRef.transform.localScale.x + PanelSpacing)));
            int y = Mathf.RoundToInt((location.z / (PanelRef.transform.localScale.z + PanelSpacing)));

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (x < 0 || x >= _dimensions.x || y < 0 || y >= _dimensions.y || float.IsNaN(x) || float.IsNaN(y))
                return false;
            else if (!canBeOccupied && _panels[x, y].Occupied)
                return false;
            else if (_panels[x, y].Alignment != alignment && alignment != GridAlignment.ANY)
                return false;

            panel = _panels[x, y];

            return true;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GridBehaviour))]
    class GridEditor : Editor
    {
        private GridBehaviour _grid;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _grid = (GridBehaviour)target;

            if (GUILayout.Button("View Grid"))
            {
                _grid.DestroyTempPanelsInEditor();
                _grid.CreateGrid();
            }
        }
    }

#endif
}

