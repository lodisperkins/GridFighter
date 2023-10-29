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
using Lodis.FX;
using Lodis.AI;

namespace Lodis.Input
{
    public class AIInputBehaviour : MonoBehaviour, IControllable
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
        private bool _inputEnabled = true;
        private BufferedInput _bufferedAction;
        private Ability _lastAbilityUsed = null;

        private bool _attackButtonDown;
        private bool _special1Down;
        private bool _special2Down;

        [SerializeField]
        private bool _abilityBuffered;
        [SerializeField]
        private CustomEventSystem.Event _onChargeStarted;
        [SerializeField]
        private CustomEventSystem.Event _onChargeEnded;
        private bool _isPaused;
        private InputDevice[] _devices;
        private bool _canBufferDefense;
        private KnockbackBehaviour _knockbackBehaviour;
        private float _defaultSpeed;
        private CharacterStateMachineBehaviour _stateMachineBehaviour;
        private bool _canBufferAbility = true;
        private TimedAction _chargeAction;
        private AIAgentBehaviour _agent;
        private bool _bufferedMovement;

        public InputDevice[] Devices 
        {
            get { return _devices; }
            set
            {
                _devices = value;
                _playerControls.devices = _devices;
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
        public bool Enabled { get => _inputEnabled; set => _inputEnabled = value; }
        public bool NormalAttackButtonDown { get => _attackButtonDown; private set => _attackButtonDown = value; }

        private void Awake()
        {
            _agent = GetComponent<AIAgentBehaviour>();
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
            _playerControls.Player.Attack.started += context => { NormalAttackButtonDown = true; };
            _playerControls.Player.Attack.canceled += context => NormalAttackButtonDown = false;
            _playerControls.Player.Attack.performed += context => { BufferNormalAbility(context, new object[2]);};
            _playerControls.Player.ChargeAttack.started += context => { NormalAttackButtonDown = true; TryChargeAttack(); };
            _playerControls.Player.ChargeAttack.performed += context => { BufferChargeNormalAbility(context, new object[2]); _onChargeEnded?.Raise(Character); _chargeAction?.Disable(); };
            _playerControls.Player.Special1.started += context => { BufferSpecialAbility(context, new object[2] { 0, 0 });  _special1Down = true; };
            _playerControls.Player.Special1.canceled += context => { _special1Down = false; };

            _playerControls.Player.Special2.started += context => { BufferSpecialAbility(context, new object[2] { 1, 0 });  _special2Down = true; };
            _playerControls.Player.Special2.canceled += context => { _special2Down = false; };
            _playerControls.Player.Burst.started += BufferBurst;
            _playerControls.Player.Shuffle.started += BufferShuffle;

            _playerControls.Player.Pause.started += context => { MatchManagerBehaviour.Instance.TogglePauseMenu(); ClearBuffer(); };
        }

        // Start is called before the first frame update
        void Start()
        {
           
            MatchManagerBehaviour.Instance.AddOnMatchPauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate);
            MatchManagerBehaviour.Instance.AddOnMatchUnpauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate);
        }

        private void OnEnable()
        {
            _playerControls.Enable();
            _playerControls.devices = _devices;
        }

        private void OnDisable()
        {
            _playerControls.Disable();
        }

        private void TryChargeAttack()
        {
            _chargeAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _onChargeStarted?.Raise(Character), TimedActionCountType.SCALEDTIME, _minChargeLimit);
        }

        private void DisableCharge()
        {
            _canBufferAbility = false;
            _onChargeEnded?.Raise(Character);
            _chargeAction?.Disable();
            NormalAttackButtonDown = false;
            _abilityBuffered = false;
        }

        public bool GetSpecialButton(int buttonNum)
        {
            if (buttonNum == 1)
                return _special1Down;
            else if (buttonNum == 2)
                return _special2Down;

            return false;
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
            _agent.Actions[0] = 1;
            //Decide which ability type to use based on the input
            if (_attackDirection.y > 0)
                _agent.Actions[3] = 1;
            else if (_attackDirection.y < 0)
                _agent.Actions[3] = 2;
            else if (_attackDirection.x < 0)
                _agent.Actions[3] = 3;
            else if (_attackDirection.x > 0)
                _agent.Actions[3] = 4;
            else
                _agent.Actions[3] = 0;

            _abilityBuffered = true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        /// Index 0 is always the power scale.
        /// index 1 is always the direction of input.</param>
        public void BufferChargeNormalAbility(InputAction.CallbackContext context, params object[] args)
        {
            _agent.Actions[0] = 2;

            //Decide which ability type to use based on the input
            if (_agent.AttackDirection.y > 0)
                _agent.Actions[3] = 1;
            else if (_agent.AttackDirection.y < 0)
                _agent.Actions[3] = 2;
            else if (_agent.AttackDirection.x < 0)
                _agent.Actions[3] = 3;
            else if (_agent.AttackDirection.x > 0)
                _agent.Actions[3] = 4;
            else
                _agent.Actions[3] = 0;
        }

        public void BufferUnblockableAbility(InputAction.CallbackContext context)
        {
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(AbilityType.UNBLOCKABLE, null), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        public void BufferBurst(InputAction.CallbackContext context)
        {
            _agent.Actions[4] = 1;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferSpecialAbility(InputAction.CallbackContext context, params object[] args)
        {
            if ((int)args[0] == 0)
                _agent.Actions[0] = 3;
            else if ((int)args[0] == 1)
                _agent.Actions[0] = 4;
        }

        private void BufferShuffle(InputAction.CallbackContext context)
        {
            _agent.Actions[0] = 5;
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
            if (NormalAttackButtonDown || _defense.IsPhaseShifting || _playerControls.Player.Move.ReadValue<Vector2>().magnitude != 0)
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
            if (direction == Vector2.up)
                _agent.Actions[1] = 1;
            else if (direction == Vector2.down)
                _agent.Actions[1] = 2;
            else if (direction == Vector2.left)
                _agent.Actions[1] = 3;
            else if (direction == Vector2.right)
                _agent.Actions[1] = 4;

            _bufferedMovement = true;
        }

        /// <summary>
        /// Removes the last input from the input buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _bufferedAction = null;
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
        }

        private bool CheckInputAllowedInAbilityPhase()
        {
            if (_lastAbilityUsed == null)
                return true;

            if (_lastAbilityUsed.CurrentAbilityPhase == AbilityPhase.STARTUP && _lastAbilityUsed.abilityData.CanInputMovementDuringStartUp)
                return true;
            else if (_lastAbilityUsed.CurrentAbilityPhase == AbilityPhase.ACTIVE && _lastAbilityUsed.abilityData.CanInputMovementWhileActive)
                return true;
            else if (_lastAbilityUsed.CurrentAbilityPhase == AbilityPhase.RECOVER && _lastAbilityUsed.abilityData.CanInputMovementWhileRecovering)
                return true;

            return false;

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
            _inputEnabled = false;
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
            if (!_bufferedMovement)
                _agent.Actions[1] = 0;

            //Checks to see if input can be enabled 
            if (_inputEnableCondition != null)
                if (_inputEnableCondition.Invoke())
                {
                    _playerControls.Player.Enable();
                    _inputEnabled = true;
                    _inputEnableCondition = null;
                }

            if (!_inputEnabled)
            {
                ClearBuffer();
                return;
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
            else if (!NormalAttackButtonDown && !_canMove && !_moveset.AbilityInUse && _bufferedAction != null)
            {
                if (!_bufferedAction.HasAction())
                    EnableMovement();
            }

            //Stores the current attack direction input
            Vector2 attackDirInput = _playerControls.Player.AttackDirection.ReadValue<Vector2>();

            //If there is a direction input, update the attack direction buffer and the time of input
            if (attackDirInput.magnitude > 0)
            {
                _agent.AttackDirection = new Vector2(Mathf.Round(attackDirInput.x) * _agent.AIMovement.GetAlignmentX(), Mathf.Round(attackDirInput.y));
                _timeOfLastDirectionInput = Time.time;
            }

            //Clear the buffer if its exceeded the alotted time
            if (Time.time - _timeOfLastDirectionInput > _attackDirectionBufferClearTime)
                _agent.AttackDirection = Vector2.zero;

            if (_bufferedAction?.HasAction() == true)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            if (Keyboard.current.tabKey.isPressed)
                DecisionDisplayBehaviour.DisplayText = !DecisionDisplayBehaviour.DisplayText;
        }
    }
}

