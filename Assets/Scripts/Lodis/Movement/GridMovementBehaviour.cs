﻿using System.Collections;
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
        private bool _isMoving;
        [SerializeField]
        private bool _canMove = true;
        [Tooltip("If true, the object can cancel its movement in one direction, and start moving in another direction.")]
        public bool canCancelMovement;
        [Tooltip("The side of the grid that this object can move on by default.")]
        [SerializeField]
        private GridAlignment _defaultAlignment = GridAlignment.ANY;
        private PanelBehaviour _currentPanel;
        private Condition _movementEnableCheck;
        private GridGame.GameEventListener _moveEnabledEventListener;
        private GridGame.GameEventListener _moveDisabledEventListener;
        private GridGame.GameEventListener _onMoveBegin;
        private GridGame.GameEventListener _onMoveBeginTemp;
        private GridGame.GameEventListener _onMoveEnd;
        private GridGame.GameEventListener _onMoveEndTemp;
        [SerializeField]
        private bool _moveToAlignedSideIfStuck = true;
        private KnockbackBehaviour _knockbackBehaviour;

        /// <summary>
        /// How much time it takes to move between panels
        /// </summary>
        public float Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        public PanelBehaviour CurrentPanel
        {
            get
            {
                return _currentPanel;
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
        }

        private void Start()
        {
            //Set the starting panel to be occupied
            if (BlackBoardBehaviour.Instance.Grid.GetPanel(_position, out _currentPanel, true, Alignment))
                _currentPanel.Occupied = true;
            else
                Debug.LogError(name + " could not find starting panel");

            if (_knockbackBehaviour)
                _knockbackBehaviour.AddOnKnockBackAction(() => SetIsMoving(false));
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
        public void DisableMovement(Condition enableCondition, bool waitForEndOfMovement = true)
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

            _movementEnableCheck = enableCondition;
            _moveDisabledEventListener.Invoke(gameObject);
        }

        /// <summary>
        /// Disables movement on the grid.
        /// </summary>
        /// <param name="moveEvent">When this event is raised, movement will be enabled.</param>
        /// <param name="intendedSender">Thhe game object that will send the event. If null, movement will be 
        /// enabled regardless of what raises the event.</param>
        /// <param name="waitForEndOfMovement">If the object is moving, its movement will be disabled once its reached
        /// its destination. If false, movement is stopped immediately.</param>
        public void DisableMovement(GridGame.Event moveEvent, GameObject intendedSender = null, bool waitForEndOfMovement = true)
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
            _moveEnabledEventListener.intendedSender = intendedSender;
            _moveEnabledEventListener.AddAction(() => { _canMove = true; });
            _moveDisabledEventListener.Invoke(gameObject);
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

            MoveDirection = Vector2.zero;
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="panelPosition">The position of the panel on the grid that the gameObject will travel to.</param>
        /// <param name="snapPosition">If true, the gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(Vector2 panelPosition, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE)
        {
            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;

            if (IsMoving && !canCancelMovement || !_canMove)
                return false;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out _targetPanel, _position == panelPosition, tempAlignment))
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

            SetIsMoving(true);

            MoveDirection = panelPosition - _position;

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
            }
            else
            {
                StartCoroutine(LerpPosition(newPosition));
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = true;
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

            if (IsMoving && !canCancelMovement ||!_canMove)
                return false;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(x, y, out _targetPanel, _position == new Vector2( x,y), tempAlignment))
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

            SetIsMoving(true);

            MoveDirection = new Vector2(x, y) - _position;

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
            }
            else
            {
                StartCoroutine(LerpPosition(newPosition));
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = true;
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

            if (!targetPanel)
                return false;

            if (IsMoving && !canCancelMovement || targetPanel.Alignment != tempAlignment && tempAlignment != GridAlignment.ANY || !_canMove)
                return false;

            _targetPanel = targetPanel;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

            SetIsMoving(true);

            MoveDirection = targetPanel.Position - _position;

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                
                transform.position = newPosition;
            }
            else
            {
                StartCoroutine(LerpPosition(newPosition));
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = true;
            _position = _currentPanel.Position;

            return true;
        }

        public void MoveToClosestAlignedPanelOnRow()
        {

            if (!_moveToAlignedSideIfStuck || _currentPanel.Alignment == Alignment || !CanMove)
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

        private void OnDestroy()
        {
            if (_currentPanel)
                _currentPanel.Occupied = false;
        }

        private void Update()
        {
            if (_canMove)
            {
                MoveToPanel(_position);
                SetIsMoving(Vector3.Distance(transform.position, _targetPosition) >= _targetTolerance);
                _movementEnableCheck = null;
            }
            else if (_movementEnableCheck != null)
            {
                if (_movementEnableCheck.Invoke())
                { 
                    _canMove = true;
                    _isMoving = false;
                    _moveEnabledEventListener.Invoke(gameObject);
                }
            }

            MoveToClosestAlignedPanelOnRow();
        }
    }
}

