using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Controls;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.Movement;
using System.Windows.Input;
using Lodis.Utility;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace Lodis.Input
{
    public delegate void InputBufferAction(object[] args = null);

    public class BufferedInput
    {
        public BufferedInput(InputBufferAction action, Condition useCondition, float bufferClearTime)
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

        public bool HasAction()
        {
            return _action != null;
        }

        private InputBufferAction _action;
        private float _bufferClearTime;
        private float _bufferStartTime;
        private Condition _useCondition;
    }


    [RequireComponent(typeof(Movement.GridMovementBehaviour))]
    public class InputBehaviour : MonoBehaviour
    {
        private Movement.GridMovementBehaviour _gridMovement;
        private CharacterDefenseBehaviour _defense;
        private MovesetBehaviour _moveset;
        private Condition _moveInputEnableCondition;
        [SerializeField]
        private bool _canMove = true;
        private Vector2 _storedMoveInput;
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
        private PlayerControls _playerControls;
        private int _playerID;
        private Condition _inputEnableCondition = null;
        [SerializeField]
        private bool _inputDisabled;
        private BufferedInput _bufferedAction;
        private string _playerState;
        private Ability _lastAbilityUsed = null;
        private bool _attackButtonDown;
        [SerializeField]
        private bool _abilityBuffered;
        [SerializeField]
        private UnityAction _onPlayerMove;
        private UnityAction _onPlayeMoveTemp;
        private bool _isPaused;
        private List<InputDevice> _devices = new List<InputDevice>();

        public List<InputDevice> Devices 
        {
            get { return _devices; }
            set
            {
                _devices = value;
                _playerControls.devices = _devices.ToArray();
            }
        }

        /// <summary>
        /// The ID number of the player using this component
        /// </summary>
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

        /// <summary>
        /// The direction the player is currently holding for an attack
        /// </summary>
        public Vector2 AttackDirection
        {
            get
            {
                return _attackDirection;
            }
        }

        private void Awake()
        {
            _playerControls = new PlayerControls();
            //Initialize action delegates
            _playerControls.Player.MoveUp.started += context => UpdateInputY(1);
            _playerControls.Player.MoveDown.started += context => UpdateInputY(-1);
            _playerControls.Player.MoveLeft.started += context => UpdateInputX(-1);
            _playerControls.Player.MoveRight.started += context => UpdateInputX(1);
            _playerControls.Player.Attack.started += context => { DisableMovement(); _attackButtonDown = true; };
            _playerControls.Player.Attack.canceled += context => _attackButtonDown = false;
            _playerControls.Player.Attack.performed += context => { BufferNormalAbility(context, new object[2]);};
            _playerControls.Player.Parry.started += context => { BufferParry(context); _defense.Brace(); };
            _playerControls.Player.Special1.started += context => { BufferSpecialAbility(context, new object[2] { 0, 0 }); };
            _playerControls.Player.Special2.started += context => { BufferSpecialAbility(context, new object[2] { 1, 0 }); };
        }

        // Start is called before the first frame update
        void Start()
        {
            _gridMovement = GetComponent<Movement.GridMovementBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _defense = GetComponent<CharacterDefenseBehaviour>();
        }

        private void OnEnable()
        {
            _playerControls.Enable();
            _playerControls.devices = _devices.ToArray();
        }

        private void OnDisable()
        {
            _playerControls.Disable();
        }

        public void AddDevice(InputDevice device)
        {
            _devices.Add(device);
            _playerControls.devices = _devices.ToArray();
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        /// Index 0 is always the power scale.
        /// index 1 is always the direction of input.</param>
        public void BufferNormalAbility(InputAction.CallbackContext context, params object[] args)
        {
            //Ignore player input if they are in knockback
            if (_playerState != "Idle" && _playerState != "Attacking")
                return;

            AbilityType abilityType;
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
                _bufferedAction = new BufferedInput(action => UseAbility(abilityType, args), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
                _abilityBuffered = true;
                return;
            }

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(abilityType, args), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferSpecialAbility(InputAction.CallbackContext context, params object[] args)
        {
            //Ignore player input if they aren't in a state that can attack
            if (_playerState != "Idle" && _playerState != "Attacking")
                return;

            AbilityType abilityType = AbilityType.SPECIAL;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Assign the arguments for the ability
            args[1] = _attackDirection;

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(abilityType, args), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        /// <summary>
        /// Buffers a parry only if the attack button is not being pressed
        /// </summary>
        /// <param name="context"></param>
        public void BufferParry(InputAction.CallbackContext context)
        {
            if (_attackButtonDown)
                return;
            else if (_bufferedAction == null && (_playerState == "Tumbling" || _playerState == "FreeFall" || _playerState == "Idle" || _playerState == "Moving"))
                _bufferedAction = new BufferedInput(action => UseDefensiveAction(), condition => !_gridMovement.IsMoving, 0.2f);
            else if (_bufferedAction == null)
                return;
            else if (!_bufferedAction.HasAction() && (_playerState == "Tumbling"
                || _playerState == "FreeFall" || _playerState == "Idle" || _playerState == "Moving"))
                _bufferedAction = new BufferedInput(action => UseDefensiveAction(), condition => !_gridMovement.IsMoving, 0.2f);
        }

        /// <summary>
        /// Uses the basic moveset ability given and updates the move input enabled condition
        /// </summary>
        /// <param name="abilityType">The basic ability type to use</param>
        /// <param name="args">Additional ability arguments like direction and attack strength</param>
        private void UseAbility(AbilityType abilityType, object[] args)
        {
            if (abilityType == AbilityType.SPECIAL)
            {
                if ((int)args[0] == 0)
                    _lastAbilityUsed = _moveset.UseSpecialAbility(0, args);
                else if ((int)args[0] == 1)
                    _lastAbilityUsed = _moveset.UseSpecialAbility(1, args);

            }
            else
            {
                _lastAbilityUsed = _moveset.UseBasicAbility(abilityType, args);
            }

            if (_lastAbilityUsed == null)
                return;

            if (!_lastAbilityUsed.abilityData.CanInputMovementWhileActive)
            {
                DisableMovementBasedOnCondition(condition => !_moveset.AbilityInUse);
            }
            else if (_lastAbilityUsed.abilityData.CanCancelOnMove)
            {
                UnityAction action = () => _lastAbilityUsed.TryCancel();
                AddOnPlayerMoveAction(action);
                _lastAbilityUsed.onEnd += () => { _onPlayerMove -= action; };
                DisableMovementBasedOnCondition(condition => _moveset.GetCanUseAbility());
            }
        }

        private void UseDefensiveAction()
        {
            _defense.ActivateParry();
            DisableMovementBasedOnCondition(args => !_defense.IsParrying);
        }

        /// <summary>
        /// Disable player movement on grid. Mainly used to prevent player from moving while attacking.
        /// USE CAREFULLY
        /// </summary>
        private void DisableMovement()
        {
            if (_playerState == "Tumbling")
                return;

            if (_attackDirection.normalized == _gridMovement.MoveDirection.normalized || _gridMovement.MoveDirection == Vector2.zero)
            {
                _canMove = false;
                _storedMoveInput = Vector2.zero;
            }
        }

        /// <summary>
        /// Disable player movement on grid
        /// </summary>
        public void DisableMovementBasedOnCondition(Condition condition)
        {
            if (_attackDirection.normalized == _gridMovement.MoveDirection.normalized || _gridMovement.MoveDirection == Vector2.zero)
            {
                _moveInputEnableCondition = condition;
                _canMove = false;
                _storedMoveInput = Vector2.zero;
            }
        }

        public void AddOnPlayerMoveAction(UnityAction action)
        {
            _onPlayerMove += action;
        }

        public void AddOnPlayerMoveTempAction(UnityAction action)
        {
            _onPlayeMoveTemp += action;
        }

        /// <summary>
        /// Enable player movement
        /// </summary>
        public bool EnableMovement()
        {
            //Don't enable if player is in knockback or in free fall
            if (_playerState == "Tumbling" || _playerState == "FreeFall")
            {
                _moveInputEnableCondition = condition => _playerState == "Idle";
                return false;
            }
            _canMove = true;
            return true;
        }

        /// <summary>
        /// Disables input until the given condition is true
        /// </summary>
        /// <param name="condition">Delegate that is checked each update</param>
        public void DisableInput(Condition condition)
        {
            _inputDisabled = true;
            _playerControls.Disable();
            _inputEnableCondition = condition;
        }

        /// <summary>
        /// BUffers input on the x axis
        /// </summary>
        /// <param name="x"></param>
        public void UpdateInputX(int x)
        {
            if (_canMove)
                _storedMoveInput = new Vector2(x, 0);
        }

        /// <summary>
        /// Buffers input on the y axis
        /// </summary>
        /// <param name="y"></param>
        public void UpdateInputY(int y)
        {
            if (_canMove)
                _storedMoveInput = new Vector2(0, y);
        }

        // Update is called once per frame
        void Update()
        {
            _playerState = BlackBoardBehaviour.Instance.GetPlayerStateFromID(PlayerID);

            //Checks to see if input can be enabled 
            if (_inputEnableCondition != null)
                if (_inputEnableCondition.Invoke())
                {
                    _playerControls.Player.Enable();
                    _inputDisabled = false;
                    _inputEnableCondition = null;
                }


            //Move if there is a movement stored and movement is allowed
            if (_storedMoveInput.magnitude > 0 && !_gridMovement.IsMoving && _canMove && _gridMovement.CanMove)
            {
                _gridMovement.MoveToPanel(_storedMoveInput + _gridMovement.Position);
                _onPlayerMove?.Invoke();
                _onPlayeMoveTemp?.Invoke();
                _onPlayeMoveTemp = null;
                _storedMoveInput = Vector2.zero;
            }
            //Checks to see if move input can be enabled 

            if (_moveInputEnableCondition != null)
            {
                if (_moveInputEnableCondition.Invoke())
                {
                    if (EnableMovement())
                        _moveInputEnableCondition = null;
                }
            }
            //If player isn't doing anything, enable movement
            else if (!_attackButtonDown && !_canMove && !_moveset.AbilityInUse && _bufferedAction != null)
            {
                if (!_bufferedAction.HasAction())
                    EnableMovement();
            }

            //Stores the current attack direction input
            Vector3 attackDirInput = _playerControls.Player.AttackDirection.ReadValue<Vector2>();

            //If there is a direction input, update the attack direction buffer and the time of input
            if (attackDirInput.magnitude > 0)
            {
                _attackDirection = attackDirInput;
                _timeOfLastDirectionInput = Time.time;
            }

            //Clear the buffer if its exceeded the alotted time
            if (Time.time - _timeOfLastDirectionInput > _attackDirectionBufferClearTime)
                _attackDirection = Vector2.zero;

            if (_bufferedAction != null)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            //Temp quit buttom for first prototype build
            if (Keyboard.current.escapeKey.isPressed)
                Application.Quit();

            if (Keyboard.current.enterKey.wasPressedThisFrame)
                EditorApplication.isPaused = true;
        }
    }
}

