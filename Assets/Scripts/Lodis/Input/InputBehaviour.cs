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
using Lodis.ScriptableObjects;

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
            if (_action == null)
                return false;

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

    public class InputBehaviour : MonoBehaviour, IControllable
    {
        private Movement.GridMovementBehaviour _gridMovement;
        private CharacterDefenseBehaviour _defense;
        private MovesetBehaviour _moveset;
        private Condition _moveInputEnableCondition;
        public static UnityAction OnApplicationQuit;
        [SerializeField]
        private GameObject _character;
        [SerializeField]
        private bool _canMove = true;
        [SerializeField]
        private bool _holdToMove;
        [SerializeField]
        private float _holdSpeed;
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
        [SerializeField]
        private IntVariable _playerID;
        private Condition _inputEnableCondition = null;
        [SerializeField]
        private bool _inputDisabled = false;
        private BufferedInput _bufferedAction;
        private Ability _lastAbilityUsed = null;
        private bool _attackButtonDown;
        [SerializeField]
        private bool _abilityBuffered;
        [SerializeField]
        private GridGame.Event _onChargeStarted;
        [SerializeField]
        private GridGame.Event _onChargeEnded;
        private bool _isPaused;
        private List<InputDevice> _devices = new List<InputDevice>();
        private bool _canBufferDefense;
        private KnockbackBehaviour _knockbackBehaviour;
        private float _defaultSpeed;
        private CharacterStateMachineBehaviour _stateMachineBehaviour;
        private bool _canBufferAbility;
        private TimedAction _chargeAction;

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
        public IntVariable PlayerID
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

        public GameObject Character { get => _character; set => _character = value; }

        private void Awake()
        {
            _playerControls = new PlayerControls();
            //Initialize action delegates
            //Movement input
            if (!_holdToMove)
            {
                _playerControls.Player.MoveUp.started += context => BufferMovement(Vector2.up);
                _playerControls.Player.MoveDown.started += context => BufferMovement(Vector2.down);
                _playerControls.Player.MoveLeft.started += context => BufferMovement(Vector2.left);
                _playerControls.Player.MoveRight.started += context => BufferMovement(Vector2.right);
            }

            //Ability input
            _playerControls.Player.Attack.started += context => { _attackButtonDown = true; TryChargeAttack(); };
            _playerControls.Player.Attack.canceled += context => _attackButtonDown = false;
            _playerControls.Player.Attack.performed += context => { BufferNormalAbility(context, new object[2]); _onChargeEnded?.Raise(Character); _chargeAction?.Disable();};
            _playerControls.Player.Special1.started += context => { BufferSpecialAbility(context, new object[2] { 0, 0 }); };
            _playerControls.Player.Special2.started += context => { BufferSpecialAbility(context, new object[2] { 1, 0 }); };
            _playerControls.Player.UnblockableAttack.started += BufferUnblockableAbility;
            _playerControls.Player.Burst.started += BufferBurst;
            _playerControls.Player.Shuffle.started += BufferShuffle;

            //Defense input
            _playerControls.Player.Parry.started += context => { BufferShield(); _defense.Brace();};
            _playerControls.Player.Parry.performed += context => {RemoveShieldFromBuffer(); };
            _playerControls.Player.PhaseShiftUp.started += context => BufferPhaseShift(context, Vector2.up);
            _playerControls.Player.PhaseShiftDown.started += context => BufferPhaseShift(context, Vector2.down);
            _playerControls.Player.PhaseShiftRight.started += context => BufferPhaseShift(context, Vector2.right);
            _playerControls.Player.PhaseShiftLeft.started += context => BufferPhaseShift(context, Vector2.left);

            _playerControls.Player.Pause.started += context => GameManagerBehaviour.Instance.TogglePause();
        }

        // Start is called before the first frame update
        void Start()
        {
            _stateMachineBehaviour = GetComponentInChildren<CharacterStateMachineBehaviour>();
            _gridMovement = Character.GetComponent<Movement.GridMovementBehaviour>();
            _moveset = Character.GetComponent<MovesetBehaviour>();
            _defense = Character.GetComponent<CharacterDefenseBehaviour>();
            _gridMovement.AddOnMoveDisabledAction(() => _storedMoveInput = Vector3.zero);
            _knockbackBehaviour = Character.GetComponent<KnockbackBehaviour>();
            _knockbackBehaviour.AddOnTakeDamageAction(DisableCharge);
            _defaultSpeed = _gridMovement.Speed;
            GameManagerBehaviour.Instance.AddOnMatchPauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate);
            GameManagerBehaviour.Instance.AddOnMatchUnpauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate);
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

        private void TryChargeAttack()
        {
            if (_stateMachineBehaviour.StateMachine.CurrentState != "Idle" && _stateMachineBehaviour.StateMachine.CurrentState != "Moving" && _stateMachineBehaviour.StateMachine.CurrentState != "Attacking")
            {
                _canBufferAbility = false;
                return;
            }
            _canBufferAbility = true;
            _chargeAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _onChargeStarted?.Raise(Character), TimedActionCountType.SCALEDTIME, _minChargeLimit);
        }

        private void DisableCharge()
        {
            _canBufferAbility = false;
            _onChargeEnded?.Raise(Character);
            _chargeAction?.Disable();
            _attackButtonDown = false;
            _abilityBuffered = false;
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
            if (!_canBufferAbility)
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
                _bufferedAction = new BufferedInput(action => { _abilityBuffered = false; UseAbility(abilityType, args); _onChargeEnded?.Raise(Character); }, condition => _moveset.GetCanUseAbility() && (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Attacking" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving"), 0.2f);
                _abilityBuffered = true;
                return;
            }

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => { _abilityBuffered = false; UseAbility(abilityType, args); _onChargeEnded?.Raise(Character); }, 
                condition =>
                { return _moveset.GetCanUseAbility() && (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Attacking" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving"); }, 0.2f);
            _abilityBuffered = true;
        }

        public void BufferUnblockableAbility(InputAction.CallbackContext context)
        {
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(AbilityType.UNBLOCKABLE, null), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        public void BufferBurst(InputAction.CallbackContext context)
        {
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(AbilityType.BURST, null), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferSpecialAbility(InputAction.CallbackContext context, params object[] args)
        {
            AbilityType abilityType = AbilityType.SPECIAL;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Assign the arguments for the ability
            args[1] = _attackDirection;

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(abilityType, args), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        private void BufferShuffle(InputAction.CallbackContext context)
        {
            if (_moveset.LoadingShuffle || _moveset.DeckReloading)
                return;

            _bufferedAction = new BufferedInput(action => _moveset.ManualShuffle(), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving", 0.2f);
        }

        private void BufferPhaseShift(InputAction.CallbackContext context, params object[] args)
        {
            if (_defense.IsResting)
                return;

            Vector2 direction = (Vector2)args[0];
            _bufferedAction = new BufferedInput(action => _defense.ActivatePhaseShift(_attackDirection), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving", 0.2f);
        }

        private void RemoveShieldFromBuffer()
        {
            _defense.DeactivateShield();
            _bufferedAction = null;
        }

        /// <summary>
        /// Buffers a parry only if the attack button is not being pressed
        /// </summary>
        /// <param name="context"></param>
        public void BufferShield()
        {
            if (_attackButtonDown || _defense.IsPhaseShifting || _playerControls.Player.Move.ReadValue<Vector2>().magnitude != 0)
                return;
            else if (_bufferedAction == null && (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving"))
                _bufferedAction = new BufferedInput(action => _defense.BeginParry(), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle", 0.2f);
            else if (_bufferedAction == null)
                return;
            else if (!_bufferedAction.HasAction() && (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving"))
                _bufferedAction = new BufferedInput(action => _defense.BeginParry(), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle", 0.2f);
        }

        /// <summary>
        /// Buffers input on the y axis
        /// </summary>
        /// <param name="y"></param>
        public void BufferMovement(Vector2 direction)
        {
            if (_abilityBuffered)
                return;

            if (_canMove)
                _storedMoveInput = direction;

            _bufferedAction = new BufferedInput(action => _gridMovement.MoveToPanel(_storedMoveInput + _gridMovement.Position),condition => _storedMoveInput.magnitude > 0 && !_gridMovement.IsMoving && _canMove && _gridMovement.CanMove, 0.2f);
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


                _gridMovement.DisableMovement(condition => _moveset.GetCanUseAbility());
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

        /// <summary>
        /// Enable player movement
        /// </summary>
        public bool EnableMovement()
        {
            //Don't enable if player is in knockback or in free fall
            if (_stateMachineBehaviour.StateMachine.CurrentState == "Tumbling" || _stateMachineBehaviour.StateMachine.CurrentState == "FreeFall")
            {
                _moveInputEnableCondition = condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle";
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

        private void CheckMoveInput()
        {
            Vector2 newMoveInput = _playerControls.Player.Move.ReadValue<Vector2>();
            if (newMoveInput == Vector2.zero)
                return;

            if (_holdToMove && _storedMoveInput == newMoveInput)
                _gridMovement.Speed = _holdSpeed;
            else if (_stateMachineBehaviour.StateMachine.CurrentState != "Moving")
                _gridMovement.Speed = _defaultSpeed;

            _storedMoveInput = newMoveInput;
            if (_storedMoveInput.magnitude == 1 && _canMove)
                _gridMovement.MoveToPanel(_gridMovement.Position + _storedMoveInput);

            //Debug.Log(_gridMovement.Speed);
        }

        // Update is called once per frame
        void Update()
        {

            //Checks to see if input can be enabled 
            if (_inputEnableCondition != null)
                if (_inputEnableCondition.Invoke())
                {
                    _playerControls.Player.Enable();
                    _inputDisabled = false;
                    _inputEnableCondition = null;
                }

            if (_holdToMove && !_abilityBuffered)
                CheckMoveInput();

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

            if (_bufferedAction?.HasAction() == true)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            if (Keyboard.current.tabKey.isPressed)
                DecisionDisplayBehaviour.DisplayText = !DecisionDisplayBehaviour.DisplayText;
        }
    }
}

