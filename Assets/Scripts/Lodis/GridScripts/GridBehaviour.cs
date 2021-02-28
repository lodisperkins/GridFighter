using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GridGame.VariableScripts;

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
        [SerializeField]
        private GameObject _panelRef;
        [SerializeField]
        private GameObject _barrierRef;
        [SerializeField]
        private Vector2 _dimensions;
        [SerializeField]
        private float _panelSpacing;
        [SerializeField]
        private PanelBehaviour[,] _panels;
        [Tooltip("How many columns to give player 1 when the game starts. Use this to decide how much territory to give both players.")]
        [SerializeField]
        private int _p1MaxColumns;
        [SerializeField]
        private Vector2[] _lhsBarrierPositions;
        [SerializeField]
        private Vector2[] _rhsBarrierPositions;

        // Start is called before the first frame update
        void Awake()
        {
            DestroyTempPanels();
            CreateGrid();
        }

        /// <summary>
        /// Creates a grid using the given dimensions and spacing.
        /// </summary>
        public void CreateGrid()
        {
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
                    spawnPosition.z += _panelRef.transform.localScale.z * _panelSpacing;
                    continue;
                }

                //Increase x position
                xPos++;
                spawnPosition.x += _panelRef.transform.localScale.x * _panelSpacing;
            }


            //After the grid is created, assign each panel a side.
            SetDefaultPanelAlignments();
            SpawnBarriers();
        }

        public void DestroyTempPanels()
        {
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        public void DestroyTempPanelsInEditor()
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

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

        private void SpawnBarriers()
        {
            foreach (Vector2 position in _lhsBarrierPositions)
            {
                GameObject barrierObject = null;
                PanelBehaviour spawnPanel = null;
                if(GetPanel(position, out spawnPanel, false))
                {
                    Vector3 spawnPosition = new Vector3(spawnPanel.transform.position.x, spawnPanel.transform.position.y + _barrierRef.transform.localScale.y / 2, spawnPanel.transform.position.z);
                    barrierObject = Instantiate(_barrierRef, spawnPosition, new Quaternion(), transform);
                }
                Movement.GridMovementBehaviour movement = barrierObject.GetComponent<Movement.GridMovementBehaviour>();
                movement.Position = spawnPanel.Position;
            }
            
            foreach (Vector2 position in _rhsBarrierPositions)
            {
                GameObject barrierObject = null;
                PanelBehaviour spawnPanel = null;
                if (GetPanel(position, out spawnPanel, false))
                {
                    Vector3 spawnPosition = new Vector3(spawnPanel.transform.position.x, spawnPanel.transform.position.y + _barrierRef.transform.localScale.y / 2, spawnPanel.transform.position.z);
                    barrierObject = Instantiate(_barrierRef, spawnPosition, new Quaternion(), transform);
                }
                Movement.GridMovementBehaviour movement = barrierObject.GetComponent<Movement.GridMovementBehaviour>();
                movement.Position = spawnPanel.Position;
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
            if (position.x < 0 || position.x >= _dimensions.x || position.y < 0 || position.y >= _dimensions.y)
                return false;
            else if (!canBeOccupied && _panels[(int)position.x, (int)position.y].Occupied)
                return false;
            else if (_panels[(int)position.x, (int)position.y].Alignment != alignment && alignment != GridAlignment.ANY)
                return false;

            panel = _panels[(int)position.x, (int)position.y];

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

