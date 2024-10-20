﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.GridScripts;
using Lodis.Gameplay;
using UnityEditor;
using UnityEngine.Events;
using System.Diagnostics;
using System.Reflection;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using DG.Tweening;
using Lodis.Sound;
using CustomEventSystem;

namespace Lodis.Movement
{

    [RequireComponent(typeof(CustomEventSystem.GameEventListener))]
    public class GridMovementBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The position of the object on the grid.")]
        private Vector2 _position;
        [SerializeField]
        [Tooltip("The current direction the object is moving in.")]
        private Vector2 _moveDirection;
        [SerializeField]
        [Tooltip("This is how close the object has to be to the panel it's moving towards to say its reached it.")]
        private float _targetTolerance = 0.05f;
        [SerializeField]
        [Tooltip("How fast the object can move towards a panel.")]
        private float _speed;
        [SerializeField]
        [Tooltip("The amount speed will be reduced when moving from an opponent panel.")]
        private float _opponentPanelSpeedReduction;
        [Tooltip("Whether or not this object is moving towards a panel")]
        [SerializeField]
        private bool _isMoving;
        [Tooltip("Whether or not this object can move to other panels")]
        [SerializeField]
        private bool _canMove = true;
        [Tooltip("If true, the object can cancel its movement in one direction, and start moving in another direction.")]
        [SerializeField]
        private bool _canCancelMovement;
        [Tooltip("The side of the grid that this object can move on by default.")]
        [SerializeField]
        private GridAlignment _defaultAlignment = GridAlignment.ANY;

        [Tooltip("Whether or not to move to the default aligned side if this object is on a panel belonging to the opposite side")]
        [SerializeField]
        private bool _moveToAlignedSideIfStuck = true;
        [SerializeField]
        private AudioClip _moveSound;
        [SerializeField]
        private CustomEventSystem.Event _onTeleportStart;
        [SerializeField]
        private CustomEventSystem.Event _onTeleportEnd;

        [Tooltip("Whether or not this object should always rotate to face the opposite side")]
        [SerializeField]
        private bool _alwaysLookAtOpposingSide = true;
        [SerializeField]
        private bool _canBeWalkedThrough = false;
        [SerializeField]
        [Tooltip("If true the object will be allowed to move diagonally on the grid.")]
        private bool _canMoveDiagonally;

        [SerializeField]
        [Tooltip("If true, the object will instantly move to its current position when the start function is called.")]
        private bool _moveOnStart = true;
        [SerializeField]
        [Tooltip("If true, the object will cast a ray to check if it is currently behind a barrier.")]
        private bool _checkIfBehindBarrier;
        [SerializeField]
        [Tooltip("If true, the object is behind a barrier. Only updated if check if behind barrier is true")]
        private bool _isBehindBarrier;

        private Tweener _moveTween;
        private static FloatVariable _maxYPosition;
        private Vector3 _targetPosition;
        private PanelBehaviour _targetPanel = null;
        private PanelBehaviour _lastPanel;
        private PanelBehaviour _currentPanel;
        private GameEventListener _moveEnabledEventListener;
        private CustomEventSystem.GameEventListener _moveDisabledEventListener;
        private CustomEventSystem.GameEventListener _onMoveBegin;
        private CustomEventSystem.GameEventListener _onMoveBeginTemp;
        private CustomEventSystem.GameEventListener _onMoveEnd;
        private CustomEventSystem.GameEventListener _onMoveEndTemp;
        private PanelBehaviour _previousPanel;
        private float _heightOffset;
        private MeshFilter _meshFilter;
        private bool _searchingForSafePanel;
        private ParticleSystem _returnEffect;
        private SkinnedMeshRenderer _renderer;
        private DelayedAction _moveEnabledAction;
        private TimedAction _teleportAction;
        private HealthBehaviour _health;

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
            set 
            {
                _speed = value;
            }
        }

        /// <summary>
        /// How long it would take this object to travel to a panel based on its speed
        /// </summary>
        public float TravelTime { get => 1 / Speed; }

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
            get { return _position; }
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

        public bool IsBehindBarrier { get => _isBehindBarrier; private set => _isBehindBarrier = value; }
        public bool CanBeWalkedThrough { get => _canBeWalkedThrough; set => _canBeWalkedThrough = value; }
        public float HeightOffset { get => _heightOffset; private set => _heightOffset = value; }
        public bool CanMoveDiagonally { get => _canMoveDiagonally; set => _canMoveDiagonally = value; }
        public bool CanCancelMovement { get => _canCancelMovement; set => _canCancelMovement = value; }
        public static FloatVariable MaxYPosition { get => _maxYPosition; private set => _maxYPosition = value; }

        private void Awake()
        {
            MaxYPosition = (FloatVariable)Resources.Load("ScriptableObjects/MaxYPosition");
            _returnEffect = ((GameObject)Resources.Load("Effects/Teleport")).GetComponent<ParticleSystem>();
            //initialize events
            _moveEnabledEventListener = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            _moveEnabledEventListener.Init(ScriptableObject.CreateInstance<CustomEventSystem.Event>(), gameObject);
            _moveDisabledEventListener = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            _moveDisabledEventListener.Init(ScriptableObject.CreateInstance<CustomEventSystem.Event>(), gameObject);

            _onMoveBegin = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            _onMoveBegin.Init(ScriptableObject.CreateInstance<CustomEventSystem.Event>(), gameObject);
            _onMoveBegin.AddAction(() => SoundManagerBehaviour.Instance.PlaySound(_moveSound, 0.2f));

            _onMoveBeginTemp = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            _onMoveBeginTemp.Init(ScriptableObject.CreateInstance<CustomEventSystem.Event>(), gameObject);

            _onMoveEnd = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            _onMoveEnd.Init(ScriptableObject.CreateInstance<CustomEventSystem.Event>(), gameObject);

            _onMoveEndTemp = gameObject.AddComponent<CustomEventSystem.GameEventListener>();
            _onMoveEndTemp.Init(ScriptableObject.CreateInstance<CustomEventSystem.Event>(), gameObject);

            _renderer = GetComponentInChildren<SkinnedMeshRenderer>();

            //Set the starting position
            _targetPosition = transform.position;
            _meshFilter = GetComponent<MeshFilter>();

            _health = GetComponent<HealthBehaviour>();

            //Adding the height ensures the gameObject is not placed inside the panel.
            if (!_meshFilter)
                _heightOffset = transform.localScale.y / 2;
            else
                _heightOffset = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;
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
            //else
            //    Debug.LogError(name + " could not find starting panel");


            if (CompareTag("Player") || CompareTag("Entity"))
            {
                BlackBoardBehaviour.Instance.AddEntityToList(this);

                
            }
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
            //Used to see what object disabled movement
            //StackFrame stackFrame = new StackFrame(1);

            //if (stackFrame != null)
            //{
            //    MethodBase method = stackFrame.GetMethod();
            //    if (method != null)
            //        UnityEngine.Debug.Log(method.DeclaringType.Name);
            //}

            if (!_canMove && !overridesMoveCondition)
                return;

            bool removed = false;

            if (_moveEnabledAction?.GetEnabled() == true)
                removed = RoutineBehaviour.Instance.StopAction(_moveEnabledAction);

            if (IsMoving && waitForEndOfMovement)
            { 
                AddOnMoveEndTempAction(() => { _canMove = false; _moveTween.Kill(); _isMoving = false; });
            }
            else
            {
                _canMove = false;
                _moveTween.Kill();
                _isMoving = false;
            }

            _moveEnabledAction = RoutineBehaviour.Instance.StartNewConditionAction(args => EnableMovement(), enableCondition);
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
        /// <param name="overridesMoveCondition">This condition to enable movement will override the move condition from an earlier call.</param>
        public void DisableMovement(CustomEventSystem.Event moveEvent, GameObject intendedSender = null, bool waitForEndOfMovement = true)
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
        }

        /// <summary>
        /// Allows this object to move on the grid again and raises the move enabled event.
        /// </summary>
        public void EnableMovement()
        {
            _canMove = true;
            _isMoving = false;
            _moveEnabledEventListener.Invoke(gameObject);
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

        private void ResetMovementValues()
        {
            if (CurrentPanel)
                CurrentPanel.Occupied = !CanBeWalkedThrough;

            MoveDirection = Vector2.zero;
        }

        /// <summary>
        /// Gradually moves the gameObject from its current position to the position given.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        private void LerpPosition(Vector3 newPosition)
        {
            if (_moveTween?.active == true)
                _moveTween.ChangeEndValue(newPosition, true);
            else
            {
                _moveTween = transform.DOMove(newPosition, TravelTime).SetUpdate(UpdateType.Fixed).SetEase(Ease.Linear);
                _moveTween.onComplete = ResetMovementValues;
            }
        }


        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="panelPosition">The position of the panel on the grid that the gameObject will travel to.</param>
        /// <param name="snapPosition">If true, the gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <param name="tempAlignment">The alignment of this object during this move,</param>
        /// <param name="canBeOccupied">Whether or not the target panel can be occupied</param>
        /// <param name="reservePanel">If true, the target panel is marked occupied before the object reaches it.</param>
        /// <param name="clampPosition">If true, will travel to the nearest available panel if the target can't be reached.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool Move(Vector2 direction, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE, bool canBeOccupied = false, bool reservePanel = true, bool clampPosition = false)
        {
            Vector2 panelPosition = Position + direction;

            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;

            if (IsMoving && !CanCancelMovement || !_canMove || _health?.Stunned == true)
                return false;
            else if (CanCancelMovement && IsMoving)
                CancelMovement();

            if (!CanMoveDiagonally && panelPosition.x != _position.x && panelPosition.y != _position.y)
                panelPosition.y = _position.y;

            if (clampPosition)
            {
                panelPosition = BlackBoardBehaviour.Instance.Grid.ClampPanelPosition(panelPosition, tempAlignment);

                if (panelPosition == Position)
                    return false;
            }

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out _targetPanel, _position == panelPosition || canBeOccupied, tempAlignment))
                return false;

            _previousPanel = _currentPanel;
            //Sets the new position to be the position of the panel added to half the gameObjects height.


            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, _heightOffset, 0);
            _targetPosition = newPosition;

            MoveDirection = (panelPosition - _position).normalized;

            SetIsMoving(true);

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                LerpPosition(newPosition);
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _lastPanel = _currentPanel;
            _currentPanel = _targetPanel;
            if (reservePanel)
                _currentPanel.Occupied = !CanBeWalkedThrough;
            _position = _currentPanel.Position;

            return true;
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="panelPosition">The position of the panel on the grid that the gameObject will travel to.</param>
        /// <param name="snapPosition">If true, the gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <param name="tempAlignment">The alignment of this object during this move,</param>
        /// <param name="canBeOccupied">Whether or not the target panel can be occupied</param>
        /// <param name="reservePanel">If true, the target panel is marked occupied before the object reaches it.</param>
        /// <param name="clampPosition">If true, will travel to the nearest available panel if the target can't be reached.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(Vector2 panelPosition, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE, bool canBeOccupied = false, bool reservePanel = true, bool clampPosition = false)
        {
            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;

            if (IsMoving && !CanCancelMovement || !_canMove || _health?.Stunned == true)
                return false;
            else if (CanCancelMovement && IsMoving)
                CancelMovement();

            if (!CanMoveDiagonally && panelPosition.x != _position.x && panelPosition.y != _position.y)
                panelPosition.y = _position.y;

            if (clampPosition)
            {
                panelPosition = BlackBoardBehaviour.Instance.Grid.ClampPanelPosition(panelPosition, tempAlignment);

                if (panelPosition == Position)
                    return false;
            }

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(panelPosition, out _targetPanel, _position == panelPosition || canBeOccupied, tempAlignment))
                return false;

            _previousPanel = _currentPanel;
            //Sets the new position to be the position of the panel added to half the gameObjects height.


            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, _heightOffset, 0);
            _targetPosition = newPosition;

            MoveDirection = (panelPosition - _position).normalized;

            SetIsMoving(true);

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                LerpPosition(newPosition);
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _lastPanel = _currentPanel;
            _currentPanel = _targetPanel;
            if (reservePanel)
                _currentPanel.Occupied = !CanBeWalkedThrough;
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

            if (IsMoving && !CanCancelMovement ||!_canMove || _health?.Stunned == true)
                return false;

            if (!CanMoveDiagonally && x != _position.x && y != _position.y)
                y = (int)_position.y;

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

            MoveDirection = new Vector2(x, y) - _position;

            SetIsMoving(true);

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                LerpPosition(newPosition);
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _lastPanel = _currentPanel;
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = !CanBeWalkedThrough;
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

            if (IsMoving && !CanCancelMovement || targetPanel.Alignment != tempAlignment && tempAlignment != GridAlignment.ANY || !_canMove || _health?.Stunned == true)
                return false;

            //To Do: This section should make this function prevent diagonal movement based on the "_canMoveDiagonally" boolean
            Vector2 targetPosition = targetPanel.Position;
            if (!CanMoveDiagonally && targetPosition.x != _position.x && targetPosition.y != _position.y && CurrentPanel)
            {
                targetPosition.y = _position.y;
                //If it's not possible to move to the panel at the given position, return false.
                if (!BlackBoardBehaviour.Instance.Grid.GetPanel((int)targetPosition.x, (int)targetPosition.y, out targetPanel, _position == new Vector2(targetPosition.x, targetPosition.y), tempAlignment))
                    return false;
            }
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


            MoveDirection = (targetPanel.Position - _position).normalized;

            SetIsMoving(true);

            //If snap position is true, hard set the position to the destination. Otherwise smoothly slide to destination.
            if (snapPosition)
            {
                
                transform.position = newPosition;
                SetIsMoving(false);
            }
            else
            {
                LerpPosition(newPosition);
            }

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _lastPanel = _currentPanel;
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = !CanBeWalkedThrough;
            _position = _currentPanel.Position;

            return true;
        }

        /// <summary>
        /// Moves this object to another location and plays the teleportation effect. 
        /// Will move regardless of movement rules like like being unable to move onto occupied panels.
        /// </summary>
        /// <param name="panel">The panel to teleport to. Spawns the character on top of the panel using its height offset.</param>
        /// <param name="travelTime">The amount of time it will take for the object to appear again.</param>
        /// <returns>Returns false if the panel is null.</returns>
        public bool TeleportToPanel(PanelBehaviour panel, float travelTime = 0.05f)
        {
            if (!panel || _health?.Stunned == true)
                return false;

            RoutineBehaviour.Instance.StopAction(_teleportAction);

            _onTeleportStart?.Raise(gameObject);
            SpawnTeleportEffect();
            gameObject.SetActive(false);

            _teleportAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                gameObject.transform.position = panel.transform.position + Vector3.up * HeightOffset;
                gameObject.SetActive(true);
                _onTeleportEnd?.Raise(gameObject);
                SpawnTeleportEffect();
            },TimedActionCountType.SCALEDTIME, travelTime);

            return true;
        }

        /// <summary>
        /// Moves this object to another location and plays the teleportation effect. 
        /// Will move regardless of movement rules like like being unable to move onto occupied panels.
        /// </summary>
        /// <param name="position">Spawns the character at the exact position given ignoring the height offset.</param>
        /// <param name="travelTime">The amount of time it will take for the object to appear again.</param>
        public void TeleportToLocation(Vector3 position, float travelTime = 0.05f, bool setInactive = true)
        {
            if (_health?.Stunned == true)
                return;

            RoutineBehaviour.Instance.StopAction(_teleportAction);

            _onTeleportStart?.Raise(gameObject);
            SpawnTeleportEffect();

            if (setInactive)
                gameObject.SetActive(false);

            _teleportAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                gameObject.transform.position = position;
                gameObject.SetActive(true);
                _onTeleportEnd?.Raise(gameObject);
                SpawnTeleportEffect();
            },TimedActionCountType.SCALEDTIME, travelTime);
        }

        /// <summary>
        /// Moves this object to the panel it should be resting on
        /// </summary>
        private void MoveToCurrentPanel()
        {

            if (IsMoving && !CanCancelMovement || !_canMove)
                return;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(_position, out _targetPanel))
                return;

            _previousPanel = _currentPanel;

            //Sets the new position to be the position of the panel added to half the gameObjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            float _heightOffset = 0;
            if (!_meshFilter)
                _heightOffset = transform.localScale.y / 2;
            else
                _heightOffset = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;

            Vector3 newPosition = _targetPanel.transform.position + new Vector3(0, _heightOffset, 0);
            _targetPosition = newPosition;

            if (_moveTween?.active == true)
                _moveTween.ChangeEndValue(newPosition, true);
            else
            {
                _moveTween = transform.DOMove(newPosition, TravelTime).SetUpdate(UpdateType.Fixed);
                _moveTween.onComplete = ResetMovementValues;
            }

            MoveDirection = (_currentPanel.Position - _position).normalized;

            //Sets the current panel to be unoccupied if it isn't null
            if (_currentPanel)
                _currentPanel.Occupied = false;

            //Updates the current panel
            _currentPanel = _targetPanel;
            _currentPanel.Occupied = !CanBeWalkedThrough;
            _position = _currentPanel.Position;
        }

        private void SpawnTeleportEffect()
        {
            //Offset the particles so they spawn at the players location
            Vector3 offset = (Vector3.right * _targetTolerance * transform.forward.x) + Vector3.back * 0.2f + Vector3.up;

            //Sets the y position so that the effect is at the players center
            if (!_meshFilter)
                offset.y = transform.localScale.y / 2;
            else
                offset.y = (_meshFilter.mesh.bounds.size.y * transform.localScale.y) / 2;

            //Spawns the effect and makes it face the camera
            Instantiate(_returnEffect, transform.position + offset, Camera.main.transform.rotation);
        }

        /// <summary>
        /// Snaps the character to the current panel they are moving towards.
        /// </summary>
        public void SnapToTarget()
        {
            if (!IsMoving || _lastPanel == null || _targetPanel == null)
                return;


            float currentDistance = Vector3.Distance(_lastPanel.transform.position, transform.position);
            float targetDistance = Vector3.Distance(_targetPanel.transform.position, transform.position);

            CanCancelMovement = true;

            if (currentDistance < targetDistance)
                MoveToPanel(CurrentPanel, true);
            else
                MoveToPanel(TargetPanel, true);


            CanCancelMovement = false;
        }


        /// <summary>
        /// Finds the closest panel that matchers this objects alignment and moves towards it
        /// </summary>
        public void MoveToClosestAlignedPanelOnRow()
        {

            if (!_moveToAlignedSideIfStuck || _currentPanel?.Alignment == Alignment || TargetPanel?.Alignment == Alignment 
                || !CanMove || Alignment == GridAlignment.ANY || _searchingForSafePanel || IsMoving)
                return;


            _searchingForSafePanel = true;
            int offSet = 0;
            float defaultMoveSpeed = _speed;
            PanelBehaviour panel = null;

            CancelMovement();
            _speed = _opponentPanelSpeedReduction;

            AddOnMoveBeginTempAction(() => 
            { 
                if (offSet <= 1) 
                    return;

                _onTeleportStart?.Raise(gameObject);
                SpawnTeleportEffect();
                _renderer.enabled = false;
            });

            RoutineBehaviour.Instance.StartNewConditionAction(args =>
            {
                _speed = defaultMoveSpeed;
                _searchingForSafePanel = false;
                if (offSet > 1)
                {
                    _onTeleportEnd?.Raise(gameObject);
                    SpawnTeleportEffect();
                }
                _renderer.enabled = true;
            },
            condition => !IsMoving
            );

            for (int i = 0; i < BlackBoardBehaviour.Instance.Grid.Dimensions.x * BlackBoardBehaviour.Instance.Grid.Dimensions.y; i ++)
            {
                BlackBoardBehaviour.Instance.Grid.GetPanel(args =>
                {
                    return Vector2.Distance(((PanelBehaviour)args[0]).Position, _currentPanel.Position) <= offSet;
                }, 
                out panel, gameObject
                );

                if (panel != null)
                {
                    MoveToPanel(panel);
                    _searchingForSafePanel = false;
                    return;
                }
                offSet++;
            }

        }

        /// <summary>
        /// Immediately stops movement and returns to previous panel
        /// </summary>
        public void CancelMovement()
        {
            if (!IsMoving || _moveTween == null)
                return;

            _moveTween.Kill();
            _currentPanel.Occupied = false;
            _currentPanel = PreviousPanel;
            _targetPanel = null;

            if (_currentPanel)
                _currentPanel.Occupied = !CanBeWalkedThrough;

            _isMoving = false;
            _targetPosition = Vector3.zero;
        }

        public float GetAlignmentX()
        {
            switch (Alignment)
            {
                case GridAlignment.LEFT:
                    return 1;
                case GridAlignment.RIGHT:
                    return -1;
                case GridAlignment.ANY:
                    return 2;
                default:
                    return 0;
            }
        }

        private void OnDestroy()
        {
            if (_currentPanel)
                _currentPanel.Occupied = false;
        }

        private void FixedUpdate()
        {
            //If the character is above the max y position...
            if (transform.position.y > MaxYPosition.Value)
            {
                //...clamp their height.
                Vector3 newPostion = new Vector3(transform.position.x, MaxYPosition.Value, transform.position.z);
                transform.position = newPostion;
            }


            if (!_checkIfBehindBarrier) return;

            IsBehindBarrier = Physics.Raycast(transform.position, transform.forward, BlackBoardBehaviour.Instance.Grid.PanelSpacingX, LayerMask.GetMask("Structure"));
        }

        private void Update()
        {
            if (!_canMove || _health?.Stunned == true)
                return;

            MoveToClosestAlignedPanelOnRow();

            if (!_currentPanel)
                return;

            if (Vector3.Distance(transform.position, _currentPanel.transform.position + Vector3.up  * _heightOffset) >= _targetTolerance || _searchingForSafePanel)
                MoveToCurrentPanel();

            SetIsMoving(Vector3.Distance(transform.position, _targetPosition) >= _targetTolerance);

            string state = BlackBoardBehaviour.Instance.GetPlayerState(gameObject);

            if (state != "Idle")
                return;

            if (_alwaysLookAtOpposingSide && _defaultAlignment == GridAlignment.RIGHT)
                transform.rotation = Quaternion.Euler(0, -90, 0);
            else if (_alwaysLookAtOpposingSide && _defaultAlignment == GridAlignment.LEFT)
                transform.rotation = Quaternion.Euler(0, 90, 0);
        }

    }
}

