using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using Lodis.Movement;
using FixedPoints;
using Types;

namespace Lodis.GridScripts
{
    /// <summary>
    /// Used to label which side of the grid an object belongs to
    /// </summary>
    [System.Serializable]
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
        private PanelBehaviour _panelRef;
        [Tooltip("The grid will use this game object to create the barriers. MUST HAVE A GRID MOVEMENT SCRIPT ATTACHED")]
        [SerializeField]
        private GridMovementBehaviour _barrierRef;
        [Tooltip("The dimensions to use when building the grid.")]
        [SerializeField]
        private Vector2 _dimensions;
        [SerializeField]
        private Vector3 _panelScale;
        [SerializeField]
        private Vector3 _barrierScale;
        [Tooltip("The amount of space that should be between each panel.")]
        [SerializeField]
        private float _panelSpacingX;
        [Tooltip("The amount of space that should be between each panel.")]
        [SerializeField]
        private float _panelSpacingZ;
        [Tooltip("The amount of space that should be between each opposing panel in the center of the grid. Defaults to z spacing if zero.")]
        [SerializeField]
        private float _panelSpacingMiddle;
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
        [Header("Fixed Point Stats")]
        [SerializeField] private FTransform _fixedGridTransform;
        [SerializeField] private FVector3 _fixedPanelScale;
        [SerializeField] private Fixed32 _fixedPanelSpacingX;
        [SerializeField] private Fixed32 _fixedPanelSpacingZ;
        [SerializeField] private Fixed32 _fixedPanelSpacingMiddle;

        //---
        private List<BarrierBehaviour> _barriers;
        private Coroutine _panelExchangeRoutine;
        private int _tempMaxColumns;
        private float _width;
        private float _height;
        private bool _invincibleBarriers;
        /// <summary>
        /// A reference to the panel object to use for building the grid
        /// </summary>
        public PanelBehaviour PanelRef
        {
            get
            {
                return _panelRef;
            }
        }

        /// <summary>
        /// The space in between each panel
        /// </summary>
        public float PanelSpacingX
        {
            get
            {
                return _panelSpacingX;
            }
        }
        
        /// <summary>
        /// The space in between each panel
        /// </summary>
        public float PanelSpacingZ
        {
            get
            {
                return _panelSpacingZ;
            }
        }

        /// <summary>
        /// The dimensions of the panels on the grid
        /// </summary>
        public Vector2 Dimensions
        {
            get 
            {
                return _dimensions;
            }
        }

        /// <summary>
        /// The dimensions of each panel
        /// </summary>
        public Vector3 PanelScale
        {
            get { return _panelScale; }
        }


        /// <summary>
        /// The width of the grid in relation to the world space
        /// </summary>
        public float Width { get => _width; }

        /// <summary>
        /// The height of the grid in relation to the world space
        /// </summary>
        public float Height { get => _height; }
        public int P1MaxColumns { get => _p1MaxColumns; }
        public bool InvincibleBarriers { get => _invincibleBarriers; set => _invincibleBarriers = value; }

        public int TempMaxColumns => _tempMaxColumns;

        public static GridBehaviour Grid { get; private set; }

        /// <summary>
        /// Creates a grid using the given dimensions and spacing.
        /// </summary>
        public void CreateGrid()
        {
            if (_panelSpacingMiddle <= 0)
                _panelSpacingMiddle = _panelSpacingX;

            float spacingVal = 0;
            Fixed32 fixedSpacingVal = 0;

            if (!BlackBoardBehaviour.Instance.Grid)
                BlackBoardBehaviour.Instance.InitializeGrid();

            _panels = new PanelBehaviour[(int)_dimensions.x, (int)_dimensions.y];

            //The world spawn position for each gameobject in the grid
            Vector3 spawnPosition = transform.position;
            FVector3 fixedSpawnPosition = _fixedGridTransform.WorldPosition;

            //The x and y position for each game object in the grid
            int xPos = 0;
            int yPos = 0;
            for (int i = 0; i < (int)_dimensions.x * (int)_dimensions.y; i++)
            {
                GameObject panel = (Instantiate(_panelRef.gameObject, spawnPosition, new Quaternion(), transform));
                panel.transform.localScale = _panelScale;
                _panels[xPos, yPos] = panel.GetComponent<PanelBehaviour>();
                _panels[xPos, yPos].Position = new FVector2(xPos, yPos);
                _panels[xPos, yPos].FixedWorldPosition = fixedSpawnPosition;

                //If the x position in the grid is equal to the given x dimension,
                //reset x position to be 0, and increase the y position.
                if (xPos == (int)_dimensions.x - 1)
                {
                    xPos = 0;
                    spawnPosition.x = transform.position.x;
                    fixedSpawnPosition.X = _fixedGridTransform.WorldPosition.X;
                    yPos++;
                    
                    spawnPosition.z += _panelRef.transform.localScale.z + _panelSpacingZ;
                    fixedSpawnPosition.Z += 1 + _fixedPanelSpacingZ;
                    continue;
                }

                spacingVal = xPos == _p1MaxColumns - 1 ? _panelSpacingMiddle : _panelSpacingX;
                fixedSpacingVal = xPos == _p1MaxColumns - 1 ? _fixedPanelSpacingMiddle : _fixedPanelSpacingX;

                //Increase x position
                xPos++;
                spawnPosition.x += _panelRef.transform.localScale.x + spacingVal;
                fixedSpawnPosition.X += 1 + fixedSpacingVal;
            }


            //After the grid is created, assign each panel a side.
            SetPanelAlignments(_p1MaxColumns);
            _tempMaxColumns = _p1MaxColumns;
            //Spawn barriers on both side and find the players' spawn location
            SpawnBarriers();

            var localScale = _panelRef.transform.localScale;
            _width = (_dimensions.x * localScale.x) + (PanelSpacingX * _dimensions.x) + _panelSpacingMiddle;
            _height = (_dimensions.y * localScale.z) + (PanelSpacingZ * _dimensions.y);

            Grid = this;

            if (!_collisionPlaneRef)
                return;

            //Spawn the collision plane underneath the grid
            EntityDataBehaviour collisionPlane = Instantiate(_collisionPlaneRef).GetComponent<EntityDataBehaviour>();


            float collisionPlaneOffsetX = ((_dimensions.x - 1) * localScale.x) + (PanelSpacingX * (_dimensions.x - 2) + _panelSpacingMiddle);
            float collisionPlaneOffsetY = ((_dimensions.y - 1) * localScale.z) + (PanelSpacingZ * (_dimensions.y - 1));

            collisionPlane.FixedTransform.LocalScale = new FVector3(_width / 10, collisionPlane.transform.localScale.y, _height / 10);
            collisionPlane.FixedTransform.WorldPosition += new FVector3(collisionPlaneOffsetX / 2, 0, collisionPlaneOffsetY / 2);

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
        /// Removes rows from one side of the grid and gives it to the other
        /// </summary>
        /// <param name="amountOfRows">The amount of rows to give to the other side</param>
        /// <param name="receiver">The side receiving the rows</param>
        /// <param name="seconds">How long will the side have control over the rows</param>
        public void ExchangeRowsByTimer(int amountOfRows, GridAlignment receiver, float seconds)
        {
            if (_panelExchangeRoutine != null)
                StopCoroutine(_panelExchangeRoutine);

            _panelExchangeRoutine = StartCoroutine(StartRowExchangeCountdown(amountOfRows, receiver, seconds));
        }

        /// <summary>
        /// Reset rows to default
        /// </summary>
        public void CancelRowExchange()
        {
            if (_panelExchangeRoutine != null)
                StopCoroutine(_panelExchangeRoutine);

            SetPanelAlignments(_p1MaxColumns);

            _panelExchangeRoutine = null;
            _tempMaxColumns = _p1MaxColumns;
        }

        private IEnumerator StartRowExchangeCountdown(int amountOfRows, GridAlignment receiver, float seconds)
        {
            int newMaxColumns = _tempMaxColumns;

            if (receiver == GridAlignment.LEFT)
                newMaxColumns = _tempMaxColumns + amountOfRows;
            else if (receiver == GridAlignment.RIGHT)
                newMaxColumns = _tempMaxColumns - amountOfRows;

            _tempMaxColumns = Mathf.Clamp(newMaxColumns, 0, (int)Dimensions.x);

            SetPanelAlignments(_tempMaxColumns);

            yield return new WaitForSeconds(seconds);

            SetPanelAlignments(_p1MaxColumns);

            _panelExchangeRoutine = null;
            _tempMaxColumns = _p1MaxColumns;
        }

        /// <summary>
        /// Labels each panel to be on either the left side on the right side of the grid based on 
        /// the value given for the maximum columns for player1.
        /// </summary>
        public void SetPanelAlignments(int columns)
        {
            //Clamp the amount of columns
            if (columns > _dimensions.x)
                columns = (int)_dimensions.x;

            //Loops through the list of panels and sets their label based on position.
            foreach (PanelBehaviour panel in _panels)
            {
                if (panel.Position.X < columns)
                {
                    panel.Alignment = GridAlignment.LEFT;
                }
                else
                {
                    panel.Alignment = GridAlignment.RIGHT;
                }

            }
        }

        public bool CheckIfPositionInRange(FVector2 position)
        {
            return position.X >= 0 && position.X < Dimensions.x && position.Y >= 0 && position.Y < Dimensions.y;
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

                if (GetPanel((int)position.x, (int)position.y, out spawnPanel, false))
                {
                    Vector3 spawnPosition = new Vector3(spawnPanel.transform.position.x, spawnPanel.transform.position.y + _barrierRef.transform.localScale.y / 2, spawnPanel.transform.position.z);
                    barrierObject = Instantiate(_barrierRef.gameObject, spawnPosition, new Quaternion(), transform);
                    barrierObject.transform.localScale = _barrierScale;
                }

                if (_barriers == null)
                {
                    _barriers = new List<BarrierBehaviour>();
                }
                BarrierBehaviour barrier = barrierObject.GetComponent<BarrierBehaviour>();

                if (InvincibleBarriers) barrier.SetInvincibilityByCondition(condition => !InvincibleBarriers);

                _barriers.Add(barrier);
                Movement.GridMovementBehaviour movement = barrierObject.GetComponent<Movement.GridMovementBehaviour>();
                movement.Position = spawnPanel.Position;
                movement.Alignment = GridAlignment.LEFT;
                barrierObject.transform.forward = Vector3.right;
            }
            //Spawns barriers for the right side
            foreach (Vector2 position in _rhsBarrierPositions)
            {
                GameObject barrierObject = null;
                PanelBehaviour spawnPanel = null;

                if (GetPanel((int)position.x, (int)position.y, out spawnPanel, false))
                {
                    Vector3 spawnPosition = new Vector3(spawnPanel.transform.position.x, spawnPanel.transform.position.y + _barrierRef.transform.localScale.y / 2, spawnPanel.transform.position.z);
                    barrierObject = Instantiate(_barrierRef.gameObject, spawnPosition, new Quaternion(), transform);
                    barrierObject.transform.localScale = _barrierScale;
                }
                if (_barriers == null)
                {
                    _barriers = new List<BarrierBehaviour>();
                }

                if (barrierObject == null)
                    return;

                BarrierBehaviour barrier = barrierObject.GetComponent<BarrierBehaviour>();

                if (InvincibleBarriers) barrier.SetInvincibilityByCondition(condition => !InvincibleBarriers);

                _barriers.Add(barrier);
                Movement.GridMovementBehaviour movement = barrierObject.GetComponent<Movement.GridMovementBehaviour>();
                movement.Position = spawnPanel.Position;
                movement.Alignment = GridAlignment.RIGHT;
                barrierObject.transform.forward = Vector3.left;
            }
        }

        /// <summary>
        /// Gives the field barriers the name of the character they're owned by based on side.
        /// </summary>
        /// <param name="lhsOwnerName">The name of the character on the left side.</param>
        /// <param name="rhsOwnerName">The name of the character on the right side.</param>
        public void AssignOwners(string lhsOwnerName, string rhsOwnerName = "")
        {
            if (_barriers == null) return;

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
                if (panel.Position.X >= min && panel.Position.X <= max)
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
        public bool GetPanel(FVector2 position, out PanelBehaviour panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        { 
            panel = null;

            if (_panels == null)
                return false;

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (position.X < 0 || position.X >= _dimensions.x || position.Y < 0 || position.Y >= _dimensions.y || float.IsNaN(position.X) || float.IsNaN(position.Y))
                return false;
            else if (!canBeOccupied && _panels[(int)position.X, (int)position.Y].Occupied)
                return false;
            else if (_panels[(int)position.X, (int)position.Y].Alignment != alignment && alignment != GridAlignment.ANY)
                return false;

            

            panel = _panels[Mathf.RoundToInt(position.X), Mathf.RoundToInt(position.Y)];

            return true;
        }

        /// <summary>
        /// Finds and outputs the panel that makes the given condition true.
        /// </summary>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="player">The player whose panels will be searched. Searches through all panels if null.</param>
        /// <returns>Returns true if the panel is found in the list and custom condition is met.</returns>
        public bool GetPanel(Condition customCondition, out PanelBehaviour panel, GameObject player = null)
        { 
            panel = null;

            if (_panels == null)
                return false;

            int xMin = 0;
            int xMax = (int)Dimensions.x;

            if (player == BlackBoardBehaviour.Instance.Player1 && player != null)
                xMax = TempMaxColumns;
            else if (player == BlackBoardBehaviour.Instance.Player2 && player != null)
                xMin = TempMaxColumns;

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = 0; y < Dimensions.y; y++)
                {
                    if (customCondition(_panels[x,y]))
                    {
                        panel = _panels[x, y];
                        return true;
                    }    
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the panel that is closest to the given location in the world
        /// </summary>
        /// <param name="location">The location to look for the panel in world space</param>
        /// <param name="panel">The panel reference to assign</param>
        /// <param name="canBeOccupied">Whether or not panels that are occupied should be ignored</param>
        /// <param name="alignment">The side of the grid to look for this panel</param>
        /// <returns></returns>
        public bool GetPanelAtLocationInWorld(Vector3 location, out PanelBehaviour panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        {
            panel = null;

            if (_panels == null)
                return false;

            int x = Mathf.RoundToInt((location.x / (PanelRef.transform.localScale.x + PanelSpacingX)));
            int y = Mathf.RoundToInt((location.z / (PanelRef.transform.localScale.z + PanelSpacingZ)));

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

        /// <summary>
        /// Gets the panel that is closest to the given location in the world
        /// </summary>
        /// <param name="location">The location to look for the panel in world space</param>
        /// <param name="panel">The panel reference to assign</param>
        /// <param name="canBeOccupied">Whether or not panels that are occupied should be ignored</param>
        /// <param name="alignment">The side of the grid to look for this panel</param>
        /// <returns></returns>
        public void GetGridCoordinateFromLocation(Vector3 location, out FVector2 panelCoordinate)
        {
            int x = Mathf.RoundToInt((location.x / (PanelRef.transform.localScale.x + PanelSpacingX)));
            int y = Mathf.RoundToInt((location.z / (PanelRef.transform.localScale.z + PanelSpacingZ)));

            panelCoordinate = new FVector2(x, y);
        }

        /// <summary>
        /// Gets a list of panels that are withing a range of 1
        /// </summary>
        /// <param name="position">The position of the panel to find neighbors for</param>
        /// <param name="canBeOccupied">Whether or not to ignore panels that are ooccupied</param>
        /// <param name="alignment">The side of the grid to look for neighbors. Panels found on the other side will be ignored</param>
        /// <returns></returns>
        public List<PanelBehaviour> GetPanelNeighbors(FVector2 position, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY, bool includeDiagonals = true)
        {
            List<PanelBehaviour> neighbors = new List<PanelBehaviour>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    FVector2 offset = new FVector2(x, y);
                    FVector2 target = offset + position;

                    if (target == position)
                        continue;

                    if (offset.Magnitude > 1 && !includeDiagonals)
                        continue;

                    PanelBehaviour panel = null;
                    if (GetPanel(target, out panel, canBeOccupied, alignment))
                        neighbors.Add(panel);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Gets a list of panels that are within a given range
        /// </summary>
        /// <param name="position">The position of the panel to find neighbors for</param>
        /// <param name="canBeOccupied">Whether or not to ignore panels that are ooccupied</param>
        /// <param name="alignment">The side of the grid to look for neighbors. Panels found on the other side will be ignored</param>
        /// <returns></returns>
        public List<PanelBehaviour> GetPanelNeighbors(FVector2 position, int range, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY, bool includeDiagonals = true)
        {
            List<PanelBehaviour> neighbors = new List<PanelBehaviour>();

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    FVector2 offset = new FVector2(x, y);
                    FVector2 target = offset + position;

                    if (target == position)
                        continue;

                    if (offset.Magnitude > 1 && !includeDiagonals)
                        continue;

                    PanelBehaviour panel = null;
                    if (GetPanel(target, out panel, canBeOccupied, alignment))
                        neighbors.Add(panel);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Clamps the given position to be within the bounds of the panels in the alignment.
        /// </summary>
        /// <param name="panelPosition">The value to clamp.</param>
        /// <param name="gridAlignment">The alignment to clamp the position within.</param>
        /// <returns>The clamped value. Will return the same value if it's already in range.</returns>
        public FVector2 ClampPanelPosition(FVector2 panelPosition, GridAlignment gridAlignment)
        {
            switch (gridAlignment)
            {
                case GridAlignment.NONE:
                case GridAlignment.ANY:
                    panelPosition.X = (Types.Fixed32)Mathf.Clamp(panelPosition.X, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
                    panelPosition.Y = (Types.Fixed32)Mathf.Clamp(panelPosition.Y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);
                    break;
                case GridAlignment.LEFT:
                    panelPosition.X = Mathf.Clamp(panelPosition.X, 0, TempMaxColumns - 1);
                    panelPosition.Y = (Types.Fixed32)Mathf.Clamp(panelPosition.Y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);
                    break;
                case GridAlignment.RIGHT:
                    panelPosition.X = (Types.Fixed32)Mathf.Clamp(panelPosition.X, TempMaxColumns - 1, BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1);
                    panelPosition.Y = (Types.Fixed32)Mathf.Clamp(panelPosition.Y, 0, BlackBoardBehaviour.Instance.Grid.Dimensions.y - 1);
                    break;
            }

            return panelPosition;
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

