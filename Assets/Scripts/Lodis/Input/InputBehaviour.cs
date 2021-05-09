using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Controls;
using Lodis.Gameplay;
using UnityEngine.Events;

namespace Lodis.Input
{
    public delegate void InputBufferAction(object[] args = null);

    public class BufferedInput
    {
        public BufferedInput(InputBufferAction action, Movement.Condition useCondition, float bufferClearTime)
        {
            _action = action;
            _useCondition = useCondition;
            _bufferClearTime = bufferClearTime;
            _bufferStartTime = Time.time;
        }

        public bool UseAction()
        {
            if (_useCondition.Invoke())
            {
                _action?.Invoke();
                _action = null;
                return true;
            }
            else if (Time.time - _bufferStartTime >= _bufferClearTime)
            {
                _action = null;
                return false;
            }

            return false;
        }

        private InputBufferAction _action;
        private float _bufferClearTime;
        private float _bufferStartTime;
        private Movement.Condition _useCondition;
    }


    [RequireComponent(typeof(Movement.GridMovementBehaviour))]
    public class InputBehaviour : MonoBehaviour
    {
        private Movement.GridMovementBehaviour _gridMovement;
        private PlayerDefenseBehaviour _defense;
        private MovesetBehaviour _moveset;
        [SerializeField]
        private bool _canMove = true;
        private Vector2 _storedMoveInput;
        private Vector2 _previousMoveInput;
        private Vector2 _attackDirection;
        [Tooltip("The minimum amount of time needed to hold the button down to change it to the charge variation.")]
        [SerializeField]
        private float _minChargeLimit = 0.5f;
        [Tooltip("The maximum amount of time needed before an attack is fully charged.")]
        [SerializeField]
        private float _maxChargeTime = 5;
        [Tooltip("The amount of time needed to clear the buffer when a direciotn is pressed.")]
        [SerializeField]
        private float _attackDirectionBufferClearTime;
        private float _timeOfLastDirectionInput;
        private InputActionAsset _actions;
        private int _playerID;
        private Movement.Condition _inputEnableCondition = null;
        private bool _inputDisabled;
        private BufferedInput _bufferedAction;
        private PlayerState _playerState;

        public int PlayerID
        {
            get
            {
                return _playerID;
            }
            set
            {
                _playerID = value;
            }
        }

        private void Awake()
        {
            //Initialize action delegates
            _actions = GetComponent<PlayerInput>().actions;
            _actions.actionMaps[0].actions[0].started += context => UpdateInputY(1);
            _actions.actionMaps[0].actions[1].started += context => UpdateInputY(-1);
            _actions.actionMaps[0].actions[2].started += context => UpdateInputX(-1);
            _actions.actionMaps[0].actions[3].started += context => UpdateInputX(1);
            _actions.actionMaps[0].actions[4].started += context => DisableMovement();
            _actions.actionMaps[0].actions[4].performed += context => UseAbility(context, new object[2]);
            _actions.actionMaps[0].actions[4].performed += context => EnableMovement();
            _actions.actionMaps[0].actions[6].performed += context => _bufferedAction = new BufferedInput(action => _defense.ActivateParry(), condition => !_gridMovement.IsMoving, 0.2f);
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _defense = GetComponent<PlayerDefenseBehaviour>();
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        /// Index 0 is always the power scale.
        /// index 1 is always the direction of input.</param>
        public void UseAbility(InputAction.CallbackContext context, params object[] args)
        {
            //Ignore player input if they are in knockback
            if (_playerState == PlayerState.KNOCKBACK || _playerState == PlayerState.FREEFALL)
                return;

            AbilityType abilityType = AbilityType.NONE;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Decide which ability type to use based on the input
            if (_attackDirection.y != 0)
                abilityType = AbilityType.WEAKSIDE;
            else if (_attackDirection.x < 0)
                abilityType = AbilityType.WEAKBACKWARD;
            else if (_attackDirection.x > 0)
                abilityType = AbilityType.WEAKFORWARD;
            else
                abilityType = AbilityType.WEAKNEUTRAL;

            //Assign the arguments for the ability
            args[1] = _attackDirection;
            //Find the power scale based on the time the button was held to use a charge ability
            float timeHeld = Mathf.Clamp((float)context.duration, 0, _maxChargeTime);
            if (timeHeld > _minChargeLimit && (int)abilityType < 4)
            {
                abilityType += 4;
                float powerScale = 0;
                powerScale = timeHeld * 0.1f + 1;
                args[0] = powerScale;
                _bufferedAction = new BufferedInput(action =>_moveset.UseBasicAbility(abilityType, args), condition => !_moveset.AbilityInUse && !_gridMovement.IsMoving, 0.2f);
                return;
            }

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => _moveset.UseBasicAbility(abilityType, args), condition => !_moveset.AbilityInUse && !_gridMovement.IsMoving, 0.2f);
        }

        /// <summary>
        /// Doesn't work because button is pressed before attack direction is updated in update func
        /// </summary>
        private void AirDodge()
        {
            _defense.ActivateAirDodge(_attackDirection);
        }

        /// <summary>
        /// Disable player movement on grid
        /// </summary>
        public void DisableMovement()
        {
            _canMove = false;
            _gridMovement.DisableMovement(condition =>  _canMove == true );
        }

        /// <summary>
        /// Enable player movement
        /// </summary>
        public void EnableMovement()
        {
            //Don't enable if player is in knockback or in free fall
            if (_playerState == PlayerState.KNOCKBACK || _playerState == PlayerState.FREEFALL)
                return;

            _canMove = true;
        }

        public void DisableInput(Movement.Condition condition)
        {
            _inputDisabled = true;
            _actions.Disable();
            _inputEnableCondition = condition;
        }

        public void UpdateInputX(int x)
        {
            _storedMoveInput = new Vector2(x, 0);
        }

        public void UpdateInputY(int y)
        {
            _storedMoveInput = new Vector2(0, y);
        }

        // Update is called once per frame
        void Update()
        {
            _playerState = BlackBoardBehaviour.GetPlayerStateFromID(PlayerID);

            //Checks to see if input can be enabled 
            if (_inputEnableCondition != null)
                if (_inputEnableCondition.Invoke())
                {
                    _actions.Enable();
                    _inputDisabled = false;
                    _inputEnableCondition = null;
                }

            //Move if the is a movement stored and movement is allowed
            if (_storedMoveInput.magnitude > 0 && !_gridMovement.IsMoving)
            {
                _gridMovement.MoveToPanel(_storedMoveInput + _gridMovement.Position);
                _gridMovement.Velocity = Vector2.zero;
                _storedMoveInput = Vector2.zero;
            }

            //Stores the current attack direction input
            Vector3 attackDirInput = _actions.actionMaps[0].actions[5].ReadValue<Vector2>();

            //If there is a direction input, update the attack direction buffer and the time of input
            if (attackDirInput.magnitude > 0)
            {
                _attackDirection = attackDirInput;
                _timeOfLastDirectionInput = Time.time;
            }

            //Clear the buffer if its exceeded the alotted time
            if (Time.time - _timeOfLastDirectionInput > _attackDirectionBufferClearTime)
                _attackDirection = Vector2.zero;

            _bufferedAction?.UseAction();
        }
    }
}

