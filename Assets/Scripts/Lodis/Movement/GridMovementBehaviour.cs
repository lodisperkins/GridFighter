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
        [SerializeField]
        [Tooltip("This is how close the object has to be to the panel it's moving towards to say its reached it.")]
        private float _targetTolerance;
        [SerializeField]
        [Tooltip("The current direction the object is moving in.")]
        private Vector2 _velocity;
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
        private GridGame.GameEventListener _onMoveBegin;
        private GridGame.GameEventListener _onMoveBeginTemp;
        private GridGame.GameEventListener _onMoveEnd;
        private GridGame.GameEventListener _onMoveEndTemp;
        [SerializeField]
        private bool _moveToAlignedSideIfStuck = true;

        /// <summary>
        /// How much time it takes to move between panels
        /// </summary>
        public float Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        /// <summary>
        /// The current velocity of the object moving on the grid.
        /// </summary>
        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
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

        private void Awake()
        {
            //initialize events
            _moveEnabledEventListener = GetComponent<GridGame.GameEventListener>();
            _onMoveBegin = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveBeginTemp = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveEnd = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
            _onMoveEndTemp = new GridGame.GameEventListener(new GridGame.Event(), gameObject);
           // _moveEnabledEventListener.AddAction(MoveToClosestAlignedPanelOnRow);

            //Set the starting position
            _targetPosition = transform.position;
        }

        private void Start()
        {
            //Set the starting panel to be occupied
            if (BlackBoardBehaviour.Grid.GetPanel(_position, out _currentPanel, true, Alignment))
                _currentPanel.Occupied = true;
            else
                Debug.LogError(name + " could not find starting panel");
        }

        public void AddOnMoveEnabledAction(UnityAction action)
        {
            if ((object)_moveEnabledEventListener != null)
                _moveEnabledEventListener.AddAction(action);
        }

        public void AddOnMoveBeginAction(UnityAction action)
        {
            if ((object)_onMoveBegin != null)
                _onMoveBegin.AddAction(action);
        }

        public void AddOnMoveBeginTempAction(UnityAction action)
        {
            if ((object)_onMoveBeginTemp != null)
                _onMoveBeginTemp.AddAction(action);
        }

        public void AddOnMoveEndAction(UnityAction action)
        {
            if ((object)_onMoveEnd != null)
                _onMoveEnd.AddAction(action);
        }

        public void AddOnMoveEndTempAction(UnityAction action)
        {
            if ((object)_onMoveEndTemp != null)
                _onMoveEndTemp.AddAction(action);
        }

        public void DisableMovement(Condition enableCondition)
        {
            if (!_canMove)
                return;

            _canMove = false;
            _movementEnableCheck = enableCondition;
            StopAllCoroutines();
        }

        public void DisableMovement(GridGame.Event moveEvent, GameObject intendedSender = null)
        {
            if (!_canMove)
                return;

            _canMove = false;
            _moveEnabledEventListener.Event = moveEvent;
            _moveEnabledEventListener.intendedSender = intendedSender;
            _moveEnabledEventListener.AddAction(() => { _canMove = true; });
            StopAllCoroutines();
        }

        /// <summary>
        /// Invokes events based on what the is moving is being set to and updates the value.
        /// </summary>
        /// <param name="value"></param>
        private void SetIsMoving(bool value)
        {
            //If is moving is being set to true, invoke onMoveBegin
            if (!_isMoving && value != _isMoving)
            {
                _onMoveBegin.Invoke(gameObject);
                _onMoveBeginTemp.Invoke(gameObject);
                _onMoveBeginTemp.ClearActions();
            }
            //If is moving is being set to false, invoke onMoveEnd
            else if (_isMoving && value != _isMoving)
            {
                _onMoveEnd.Invoke(gameObject);
                _onMoveEndTemp.Invoke(gameObject);
                _onMoveEndTemp.ClearActions();
            }

            _isMoving = value;
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
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="panelPosition">The position of the panel on the grid that the gameObject will traver to.</param>
        /// <param name="snapPosition">If true, teh gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(Vector2 panelPosition, bool snapPosition = false, GridAlignment tempAlignment = GridAlignment.NONE)
        {
            if (tempAlignment == GridAlignment.NONE)
                tempAlignment = _defaultAlignment;

            if (IsMoving && !canCancelMovement || !_canMove)
                return false;

            PanelBehaviour targetPanel;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Grid.GetPanel(panelPosition, out targetPanel, _position == panelPosition, tempAlignment))
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

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
            _currentPanel = targetPanel;
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

            PanelBehaviour targetPanel;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Grid.GetPanel(x, y, out targetPanel, _position == new Vector2( x,y), tempAlignment))
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

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
            _currentPanel = targetPanel;
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

            if (IsMoving && !canCancelMovement || targetPanel.Alignment != tempAlignment && tempAlignment != GridAlignment.ANY || !_canMove)
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

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
            _currentPanel = targetPanel;
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

            while (panelsEvaluated <= BlackBoardBehaviour.Grid.Dimensions.x * BlackBoardBehaviour.Grid.Dimensions.y)
            {
                if (BlackBoardBehaviour.Grid.GetPanel(Position + Vector2.left * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                else if (BlackBoardBehaviour.Grid.GetPanel(Position + Vector2.right * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                else if (BlackBoardBehaviour.Grid.GetPanel(Position + Vector2.up * offSet, out panel, false, Alignment))
                {
                    MoveToPanel(panel);
                    return;
                }
                else if (BlackBoardBehaviour.Grid.GetPanel(Position + Vector2.down * offSet, out panel, false, Alignment))
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
                MoveToPanel(_position + Velocity);
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

