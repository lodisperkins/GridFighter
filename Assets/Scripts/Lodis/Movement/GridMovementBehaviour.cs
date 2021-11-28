using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.GridScripts;
using Lodis.Gameplay;
using UnityEditor;
using UnityEngine.Events;


namespace Lodis.Movement
{
    public delegate bool Condition(object[] args = null);


    [RequireComponent(typeof(GridGame.GameEventListener))]
    public class GridMovementBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The position of the object on the grid.")]
        private Vector2 _position;
        private Vector3 _targetPosition;
        private PanelBehaviour _targetPanel = null;
        [SerializeField]
        [Tooltip("This is how close the object has to be to the panel it's moving towards to say its reached it.")]
        private float _targetTolerance = 0.05f;
        [SerializeField]
        [Tooltip("The current direction the object is moving in.")]
        private Vector2 _moveDirection;
        [SerializeField]
        [Tooltip("How fast the object can move towards a panel.")]
        private float _speed;
        [Tooltip("Whether or not this object is moving towards a panel")]
        [SerializeField]
        private bool _isMoving;
        [Tooltip("Whether or not this object can move to other panels")]
        [SerializeField]
        private bool _canMove = true;
        [Tooltip("If true, the object can cancel its movement in one direction, and start moving in another direction.")]
        public bool canCancelMovement;
        [Tooltip("The side of the grid that this object can move on by default.")]
        [SerializeField]
        private GridAlignment _defaultAlignment = GridAlignment.ANY;
        private GridAlignment _tempAlignment;
        private PanelBehaviour _currentPanel;
        private Condition _movementEnableCheck;
        private GridGame.GameEventListener _moveEnabledEventListener;
        private GridGame.GameEventListener _moveDisabledEventListener;
        private GridGame.GameEventListener _onMoveBegin;
        private GridGame.GameEventListener _onMoveBeginTemp;
        private GridGame.GameEventListener _onMoveEnd;
        private GridGame.GameEventListener _onMoveEndTemp;
        [Tooltip("Whether or not to move to the default aligned side if this object is on a panel belonging to the opposite side")]
        [SerializeField]
        private bool _moveToAlignedSideIfStuck = true;
        [Tooltip("Whether or not this object should always rotate to face the opposite side")]
        [SerializeField]
        private bool _alwaysLookAtOpposingSide = true;
        [SerializeField]
        private bool _canBeWalkedThrough = false;
        private PanelBehaviour _previousPanel;
        private KnockbackBehaviour _knockbackBehaviour;
        private MeshFilter _meshFilter;
        private Collider _collider;
        [SerializeField]
        [Tooltip("If true, the object will instantly move to its current position when the start function is called.")]
        private bool _moveOnStart = true;
        private Coroutine _MoveRoutine;

        /// <summary>
        /// Whether or not this object should move to its current panel when spawned
        /// </summary>
        public bool MoveOnStart
        {
            get
            {
                return _moveOnStart;
            }
            set 
            {
                _moveOnStart = value;
            }
        }


        /// <summary>
        /// How much time it takes to move between panels
        /// </summary>
        public float Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        /// <summary>
        /// The current panel this object is resting on
        /// </summary>
        public PanelBehaviour CurrentPanel
        {
            get
            {
                return _currentPanel;
            }
        }

        /// <summary>
        /// The previous panel this object was resting on
        /// </summary>
        public PanelBehaviour PreviousPanel
        {
            get
            {
                return _previousPanel;
            }
        }


        /// <summary>
        /// The current velocity of the object moving on the grid.
        /// </summary>
        public Vector2 MoveDirection
        {
            get { return _moveDirection; }
            set { _moveDirection = value; }
        }

        /// <summary>
        /// The position of the object on the grid.
        /// </summary>
        public Vector2 Position
        {
            get { return _currentPanel.Position; }
            set { _position = value; }
        }

        /// <summary>
        /// True if the object is moving between panels
        /// </summary>
        public bool IsMoving
        {
            get
            {
                return _isMoving;
            }
        }

        public bool CanMove
        {
            get
            {
                return _canMove;
            }
        }

        /// <summary>
        /// The side of the grid this object is aligned with. Effects
        /// which side of the grid this object can freely move on.
        /// </summary>
        public GridAlignment Alignment
        {
            get
            {
                return _defaultAlignment;
            }
            set
            {
                _defaultAlignment = value;
            }
        }

        public bool MoveToAlignedSideWhenStuck
        {
            get
            {
                return _moveToAlignedSideIfStuck;
            }
            set
            {
                _moveToAlignedSideIfStuck = value;
            }
        }

        public bool AlwaysLookAtOpposingSide
        {
            get
            {
                return _alwaysLookAtOpposingSide;
            }
            set
            {
                _alwaysLookAtOpposingSide = value;
            }
        }

        /// <summary>
        /// The panel this object is moving towards
        /// </summary>
        public PanelBehaviour TargetPanel
        {
            get
            {
                return _targetPanel;
            }
        }

        private void Awake()
        {
            //initialize events
            _moveEnabledEventListener = GetComponent<GridGame.GameEventListener>();
            _moveDisabledEventListener = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveBegin = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveBeginTemp = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveEnd = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveEndTemp = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _knockbackBehaviour = GetComponent<KnockbackBehaviour>();
            //Set the starting position
            _targetPosition = transform.position;
            _meshFilter = GetComponent<MeshFilter>();
            _collider = GetComponent<Collider>();

            if (CompareTag("Player") || CompareTag("Entity"))
                BlackBoardBehaviour.Instance.AddEntityToList(gameObject);
        }

        private void Start()
        {
            //Set the starting panel to be occupied
            if (BlackBoardBehaviour.Instance.Grid.GetPanel(_position, out _currentPanel, true, Alignment))
            {
                _currentPanel.Occupied = true;

                if (MoveOnStart)
                    MoveToPanel(_currentPanel, true);
            }
            else
                Debug.LogError(name + " could not find starting panel");

            if (_knockbackBehaviour)
                _knockbackBehaviour.AddOnKnockBackAction(() => SetIsMoving(false));

            _tempAlignment = _defaultAlignment;
        }

        /// <summary>
        /// Add a listener to the on move disabled event.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnMoveDisabledAction(UnityAction action)
        {
            if ((object)_moveDisabledEventListener != null)
                _moveDisabledEventListener.AddAction(action);
        }

        /// <summary>
        /// Add a listener to the on move enabled event.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnMoveEnabledAction(UnityAction action)
        {
            if ((object)_moveEnabledEventListener != null)
                _moveEnabledEventListener.AddAction(action);
        }

        /// <summary>
        /// Add a listener to the on move begin event.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnMoveBeginAction(UnityAction action)
        {
            if ((object)_onMoveBegin != null)
                _onMoveBegin.AddAction(action);
        }

        /// <summary>
        /// Add a listener to the on move begin event.
        /// The listeners for this event are cleared after being invoked.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnMoveBeginTempAction(UnityAction action)
        {
            if ((object)_onMoveBeginTemp != null)
                _onMoveBeginTemp.AddAction(action);
        }

        /// <summary>
        /// Add a listener to the on move end event.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnMoveEndAction(UnityAction action)
        {
            if ((object)_onMoveEnd != null)
                _onMoveEnd.AddAction(action);
        }

        /// <summary>
        /// Add a listener to the on move end event.
        /// The listeners for this event are cleared after being invoked.
        /// </summary>
        /// <param name="action"></param>
        public void AddOnMoveEndTempAction(UnityAction action)
        {
            if ((object)_onMoveEndTemp != null)
                _onMoveEndTemp.AddAction(action);
        }

        /// <summary>
        /// Disables movement on the grid.
        /// </summary>
        /// <param name="enableCondition">When this condition is true, movement will be enabled.</param>
        /// <param name="waitForEndOfMovement">If the object is moving, its movement will be disabled once its reached
        /// its destination. If false, movement is stopped immediately.</param>
        /// <param name="overridesMoveCondition">This condition to enable movement will override the move condition from an earlier call.</param>
        public void DisableMovement(Condition enableCondition, bool waitForEndOfMovement = true, bool overridesMoveCondition = false)
        {
            if (!_canMove && !overridesMoveCondition)
                return;

            if (IsMoving && waitForEndOfMovement)
            { 
                AddOnMoveEndTempAction(() => { _canMove = false; StopAllCoroutines(); _isMoving = false; });
            }
            else
            {
                _canMove = false;
                StopAllCoroutines();
                _isMoving = false;
            }

            _movementEnableCheck = enableCondition;
            _moveDisabledEventListener.Invoke(gameObject);
            _tempAlignment = Alignment;
        }

        /// <summary>
        /// Disables movement on the grid.
        /// </summary>
        /// <param name="moveEvent">When this event is raised, movement will be enabled.</param>
        /// <param name="intendedSender">Thhe game object that will send the event. If null, movement will be 
        /// enabled regardless of what raises the event.</param>
        /// <param name="waitForEndOfMovement">If the object is moving, its movement will be disabled once its reached
        /// its destination. If false, movement is stopped immediately.</param>
        /// <param name="overridesMoveCondition">This condition to enable movement will override the move condition from an earlier call.</param>
        public void DisableMovement(GridGame.Event moveEvent, GameObject intendedSender = null, bool waitForEndOfMovement = true, bool overridesMoveCondition = false)
        {
            if (!_canMove)
                return;

            if (IsMoving && waitForEndOfMovement)
            {
                AddOnMoveEndTempAction(() => { _canMove = false; StopAllCoroutines(); });
            }
            else
            { 
                _canMove = false;
                StopAllCoroutines();
            }

            _moveEnabledEventListener.Event = moveEvent;
            _moveEnabledEventListener.IntendedSender = intendedSender;
            _moveEnabledEventListener.AddAction(() => { _canMove = true; });
            _moveDisabledEventListener.Invoke(gameObject);
            _tempAlignment = Alignment;
        }

        /// <summary>
        /// Invokes events based on what the is moving is being set to and updates the value.
        /// </summary>
        /// <param name="value"></param>
        private void SetIsMoving(bool value)
        {
            bool isMoving = _isMoving;
            _isMoving = value;

            //If is moving is being set to true, invoke onMoveBegin
            if (!isMoving && value != isMoving)
            {
                _onMoveBegin?.Invoke(gameObject);
                _onMoveBeginTemp?.Invoke(gameObject);
                _onMoveBeginTemp?.ClearActions();
            }
            //If is moving is being set to false, invoke onMoveEnd
            else if (isMoving && value != isMoving)
            {
                _onMoveEnd?.Invoke(gameObject);
                _onMoveEndTemp?.Invoke(gameObject);
                _onMoveEndTemp?.ClearActions();
            }
        }

        /// <summary>
        /// Gradually moves the gameObject from its current position to the position given.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        private IEnumerator LerpPosition(Vector3 newPosition)
        {
            float lerpVal = 0;
            Vector3 startPosition = transform.position;

            while (transform.position != newPosition)
            {
                //Sets the current position to be the current position in the interpolation
                transform.position = Vector3.Lerp(startPosition, newPosition, lerpVal += Time.deltaTime * _speed);
                //Waits until the next fixed update before resuming to be in line with any physics calls
                yield return new WaitForFixedUpdate();
            }

            if (CurrentPanel)
                CurrentPanel.Occupied = !_canBeWalkedThrough;

            MoveDirection = Vector2.zero;
            _tempAlignment = Alignment;
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="panelPosition">The position of the panel on the grid that the gameObject will travel to.</param>
        /// <param name="snapPosition">If true, the gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(Vector2 panelPosition, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE, bool canBeOccupied = false, bool reservePanel = true)
        {
            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;
            else
                _tempAlignment = tempAlignment;

            if (IsMoving && !canCancelMovement || !_canMove)
                return false;
            else if (canCancelMovement && IsMoving)
                StopAllCoroutines();

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out _targetPanel, _position == panelPosition || canBeOccupied, tempAlignment))
                return false;

            _previousPanel = _currentPanel;

            //Sets the new position to be the position of the panel added to half the gameObjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            float offset = 0;
            if (!_meshFilter)
                offset = transform.localScale.y / 2;
            else
                offset = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;

            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, offset, 0);
            _targetPosition = newPosition;

            SetIsMoving(true);

            MoveDirection = panelPosition - _position;

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                _MoveRoutine = StartCoroutine(LerpPosition(newPosition));
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            if (reservePanel)
                _currentPanel.Occupied = !_canBeWalkedThrough;
            _position = _currentPanel.Position;

            return true;
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="x">The x position of the panel on the grid that the gameObject will traver to.</param>
        /// /// <param name="y">The y position of the panel on the grid that the gameObject will traver to.</param>
        /// <param name="snapPosition">If true, teh gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(int x, int y, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE)
        {
            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;
            else
                _tempAlignment = tempAlignment;

            if (IsMoving && !canCancelMovement ||!_canMove)
                return false;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(x, y, out _targetPanel, _position == new Vector2( x,y), tempAlignment))
                return false;

            _previousPanel = _currentPanel;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            float offset = 0;
            if (!_meshFilter)
                offset = transform.localScale.y / 2;
            else
                offset = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;

            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, offset, 0);
            _targetPosition = newPosition;

            SetIsMoving(true);

            MoveDirection = new Vector2(x, y) - _position;

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                _MoveRoutine = StartCoroutine(LerpPosition(newPosition));
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = !_canBeWalkedThrough;
            _position = _currentPanel.Position;
            return true;
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="targetPanel">The panel on the grid that the gameObject will travel to.</param>
        /// <param name="snapPosition">If true, teh gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(PanelBehaviour targetPanel, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE)
        {
            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;
            else
                _tempAlignment = tempAlignment;

            if (!targetPanel)
                return false;

            if (IsMoving && !canCancelMovement || targetPanel.Alignment != tempAlignment && tempAlignment != GridAlignment.ANY || !_canMove)
                return false;

            _previousPanel = _currentPanel;
            _targetPanel = targetPanel;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            float offset = 0;
            if (!_meshFilter)
                offset = transform.localScale.y / 2;
            else
                offset = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;

            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, offset, 0);
            _targetPosition = newPosition;


            SetIsMoving(true);

            MoveDirection = targetPanel.Position - _position;

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                _MoveRoutine = StartCoroutine(LerpPosition(newPosition));
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = !_canBeWalkedThrough;
            _position = _currentPanel.Position;

            return true;
        }

        /// <summary>
        /// Moves this object to the panel it should be resting on
        /// </summary>
        private void MoveToCurrentPanel()
        {

            if (IsMoving && !canCancelMovement || !_canMove)
                return;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(_position, out _targetPanel))
                return;

            _previousPanel = _currentPanel;

            //Sets the new position to be the position of the panel added to half the gameObjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            float offset = 0;
            if (!_meshFilter)
                offset = transform.localScale.y / 2;
            else
                offset = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;

            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, offset, 0);
            _targetPosition = newPosition;


            _MoveRoutine = StartCoroutine(LerpPosition(newPosition));

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = !_canBeWalkedThrough;
            _position = _currentPanel.Position;
        }

        /// <summary>
        /// Finds the closest panel that matchers this objects alignment and moves towards it
        /// </summary>
        public void MoveToClosestAlignedPanelOnRow()
        {

            if (!_moveToAlignedSideIfStuck || _currentPanel.Alignment == Alignment || !CanMove || Alignment == GridAlignment.ANY || Alignment != _tempAlignment)
                return;

            //NEEDS BETTER IMPLEMENTATION

            int panelsEvaluated = 0;
            int offSet = 0;

            PanelBehaviour panel = null;

            while (panelsEvaluated <= BlackBoardBehaviour.Instance.Grid.Dimensions.x * BlackBoardBehaviour.Instance.Grid.Dimensions.y)
            {
                if (BlackBoardBehaviour.Instance.Grid.GetPanel(Position + Vector2.left * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                else if (BlackBoardBehaviour.Instance.Grid.GetPanel(Position + Vector2.right * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                else if (BlackBoardBehaviour.Instance.Grid.GetPanel(Position + Vector2.up * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                else if (BlackBoardBehaviour.Instance.Grid.GetPanel(Position + Vector2.down * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                panelsEvaluated += 4;
                offSet++;
            }
        }

        /// <summary>
        /// Immediately stops movement and returns to previous panel
        /// </summary>
        public void CancelMovement()
        {
            StopCoroutine(_MoveRoutine);
            _currentPanel.Occupied = false;
            _currentPanel = PreviousPanel;
            _currentPanel.Occupied = !_canBeWalkedThrough;
            _isMoving = false;
        }

        private void OnDestroy()
        {
            if (_currentPanel)
                _currentPanel.Occupied = false;
        }

        private void Update()
        {
            if (_canMove)
            {
                MoveToCurrentPanel();
                SetIsMoving(Vector3.Distance(transform.position, _targetPosition) >= _targetTolerance);

                if (_alwaysLookAtOpposingSide && _defaultAlignment == GridAlignment.RIGHT)
                    transform.rotation = Quaternion.Euler(0, -90, 0);
                else if (_alwaysLookAtOpposingSide && _defaultAlignment == GridAlignment.LEFT)
                    transform.rotation = Quaternion.Euler(0, 90, 0);
            }
            else if (_movementEnableCheck != null)
            {
                if (_movementEnableCheck.Invoke())
                {
                    _movementEnableCheck = null;
                    _canMove = true;
                    _isMoving = false;
                    _moveEnabledEventListener.Invoke(gameObject);
                }
            }

            
            MoveToClosestAlignedPanelOnRow();
        }
    }
}

