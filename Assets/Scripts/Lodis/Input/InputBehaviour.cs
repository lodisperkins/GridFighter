using UnityEngine;
using UnityEngine.InputSystem;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.Movement;
using Lodis.Utility;
using Lodis.ScriptableObjects;
using Lodis.FX;
using FixedPoints;
using System;
using UnityGGPO;
using Types;
using System.IO;

namespace Lodis.Input
{
    [Flags]
    public enum InputFlag
    {
        NONE = 0,
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Weak = 1 << 4,
        Strong = 1 << 5,
        Special1 = 1 << 6,
        Special2 = 1 << 7,
        Burst = 1 << 8,
        Shuffle = 1 << 9
    }

    /// <summary>
    /// Stores an action that will take place once some condition is met.
    /// </summary>
    public class BufferedInput
    {
        private Fixed32 _bufferClearTime;
        private Fixed32 _bufferStartTime;
        private Condition _useCondition;

        public delegate void InputBufferAction();
        public event InputBufferAction OnPerformAction;
        public event InputBufferAction OnClearAction;


        public BufferedInput(InputBufferAction action, Condition useCondition, float bufferClearTime)
        {
            OnPerformAction = action;
            _useCondition = useCondition;
            _bufferClearTime = bufferClearTime;
            _bufferStartTime = Utils.TimeGetTime();
        }


        public bool UseAction()
        {
            if (OnPerformAction == null)
                return false;

            if (_useCondition.Invoke())
            {
                OnPerformAction?.Invoke();
                OnPerformAction = null;
                return true;
            }
            else if (Utils.TimeGetTime() - _bufferStartTime >= _bufferClearTime)
            {
                OnPerformAction = null;
                OnClearAction?.Invoke();
                return false;
            }

            return false;
        }

        //Serialize functions so that the buffer can be reset during rollback.
        public void Serialize(BinaryWriter bw)
        {
            bw.Write(_bufferStartTime);
        }
        public void Deserialize(BinaryReader br)
        {
            _bufferStartTime = br.ReadUInt64();
        }

        public bool HasAction()
        {
            return OnPerformAction != null;
        }
    }

    public class InputBehaviour : SimulationBehaviour, IControllable
    {
        [Header("References")]
        [SerializeField] private IntVariable _playerID;
        [SerializeField] private GameObject _character;

        [Header("Input Parameters")]
        [SerializeField] private float _holdSpeed;
        [Tooltip("The minimum amount of time needed to hold the button down to change it to the charge variation.")]
        [SerializeField] private Fixed32 _minChargeLimit = new Fixed32(32768);
        [Tooltip("The maximum amount of time needed before an attack is fully charged.")]
        [SerializeField] private Fixed32 _maxChargeTime = 5;
        [Tooltip("The amount of time needed to clear the buffer when a direciotn is pressed.")]
        [SerializeField] private float _attackDirectionBufferClearTime;

        [Header("Toggles")]
        [SerializeField] private bool _canMove = true;
        [SerializeField] private bool _holdToMove;
        [SerializeField] private bool _inputEnabled = true;
        [SerializeField] private bool _abilityBuffered;

        [Header("Events")]
        [SerializeField] private CustomEventSystem.Event _onChargeStarted;
        [SerializeField] private CustomEventSystem.Event _onChargeEnded;

        //---
        private Movement.GridMovementBehaviour _gridMovement;
        private CharacterDefenseBehaviour _defense;
        private MovesetBehaviour _moveset;
        private KnockbackBehaviour _knockbackBehaviour;
        private CharacterStateMachineBehaviour _stateMachineBehaviour;

        private Condition _moveInputEnableCondition;
        private Condition _inputEnableCondition = null;
        private static UnityAction _onActionButtonDown;
        private PlayerControls _playerControls;
        private BufferedInput _bufferedAction;
        private Ability _lastAbilityUsed = null;

        private InputDevice[] _devices;
        private FixedTimeAction _chargeAction;

        private Vector2 _storedMoveInput;
        private FVector2 _attackDirection;
        private float _timeOfLastDirectionInput;
        private float _defaultSpeed;
        private Fixed32 _chargeHoldTime;

        private static bool _playerActionButtonDown;
        private bool _isPaused;
        private bool _canBufferDefense;
        private bool _canBufferAbility = true;
        private bool _movementBuffered;
        private bool _attackButtonDown;
        private bool _chargingAttack;
        private bool _special1Down;
        private bool _special2Down;

        public static UnityAction OnApplicationQuit;

        public InputDevice[] Devices 
        {
            get { return _devices; }
            set
            {
                _devices = value;
                PlayerControls.devices = _devices;
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
        public FVector2 AttackDirection
        {
            get
            {
                return _attackDirection;
            }
        }

        public GameObject Character { get => _character; set => _character = value; }
        public bool Enabled { get => _inputEnabled; set => _inputEnabled = value; }
        public bool NormalAttackButtonDown { get => _attackButtonDown; private set => _attackButtonDown = value; }
        public static bool PlayerActionButtonDown { get => _playerActionButtonDown; private set => _playerActionButtonDown = value; }
        public PlayerControls PlayerControls { get => _playerControls; private set => _playerControls = value; }

        protected override void Awake()
        {
            PlayerControls = new PlayerControls();
            ////Initialize action delegates
            ////Movement input
            //if (!_holdToMove)
            //{
            //    PlayerControls.Player.MoveUp.started += context => BufferMovement(Vector2.up);
            //    PlayerControls.Player.MoveDown.started += context => BufferMovement(Vector2.down);
            //    PlayerControls.Player.MoveLeft.started += context => BufferMovement(Vector2.left);
            //    PlayerControls.Player.MoveRight.started += context => BufferMovement(Vector2.right);
            //}

            ////Ability input
            //PlayerControls.Player.Attack.started += context => { NormalAttackButtonDown = true; };
            //PlayerControls.Player.Attack.canceled += context => NormalAttackButtonDown = false;
            //PlayerControls.Player.Attack.performed += context => { BufferNormalAbility(context, new object[2]);};
            //PlayerControls.Player.ChargeAttack.started += context => { NormalAttackButtonDown = true; TryChargeAttack(); };
            //PlayerControls.Player.ChargeAttack.performed += context => { BufferChargeNormalAbility(context, new object[2]); _onChargeEnded?.Raise(Character); _chargeAction?.Disable(); };
            //PlayerControls.Player.Special1.started += context => { BufferSpecialAbility(context, new object[2] { 0, 0 });  _special1Down = true; };
            //PlayerControls.Player.Special1.canceled += context => { _special1Down = false; };

            //PlayerControls.Player.Special2.started += context => { BufferSpecialAbility(context, new object[2] { 1, 0 });  _special2Down = true; };
            //PlayerControls.Player.Special2.canceled += context => { _special2Down = false; };
            //PlayerControls.Player.Burst.started += BufferBurst;
            //PlayerControls.Player.Shuffle.started += BufferShuffle;

            //PlayerControls.Player.Pause.started += context => { MatchManagerBehaviour.Instance.TogglePauseMenu(); ClearBuffer(); };
            _defaultSpeed = _holdSpeed;
            //Instead of listening to input events from unity we will instead listen to custom GGPO input events.
            GridGame.OnPollInput += GridGame_PollInput;
            GridGame.OnProcessInput += GridGame_ProcessInput;
        }
        /// <summary>
        /// Called every GGPO frame and is used to parse the current inputs. Inputs could be changed during rollback. This function should catch that and update the buffered action.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inputs"></param>
        private void GridGame_ProcessInput(int id, long inputs)
        {
            if (id != PlayerID)
                return;

            if ((inputs & (long)InputFlag.Up) != 0)
            {
                _attackDirection = new FVector2(0, 1);
                // Call the function related to Up input
                BufferMovement(new Vector2(0, 1));
            }
            if ((inputs & (long)InputFlag.Down) != 0)
            {
                _attackDirection = new FVector2(0, -1);
                // Call the function related to Down input
                BufferMovement(new Vector2(0, -1));
            }
            if ((inputs & (long)InputFlag.Left) != 0)
            {
                _attackDirection = new FVector2(-1, 0);
                // Call the function related to Left input
                BufferMovement(new Vector2(-1, 0));
            }
            if ((inputs & (long)InputFlag.Right) != 0)
            {
                _attackDirection = new FVector2(1, 0);
                // Call the function related to Right input
                BufferMovement(new Vector2(1, 0));
            }
            if ((inputs & (long)InputFlag.Weak) != 0)
            {
                // Call the function related to Weak attack
                BufferNormalAbility();
            }
            if ((inputs & (long)InputFlag.Strong) != 0)
            {
                // Call the function related to Strong attack
                BufferChargeNormalAbility();
            }
            if ((inputs & (long)InputFlag.Special1) != 0)
            {
                // Call the function related to Special1
                BufferSpecialAbility(0);
            }
            if ((inputs & (long)InputFlag.Special2) != 0)
            {
                // Call the function related to Special2
                BufferSpecialAbility(1);
            }
            if ((inputs & (long)InputFlag.Burst) != 0)
            {
                // Call the function related to Burst
                BufferBurst();
            }
            if ((inputs & (long)InputFlag.Shuffle) != 0)
            {
                // Call the function related to Shuffle
                BufferShuffle();
            }
        }

        /// <summary>
        /// Sets the current input flags for this player.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void GridGame_PollInput(int id)
        {
            if (id == PlayerID)
                GetInputFlags();
        }

        private void GetInputFlags()
        {
            InputFlag flags = InputFlag.NONE;

            if (_playerControls.Player.MoveUp.IsPressed())
                flags |= InputFlag.Up;
            if (_playerControls.Player.MoveDown.IsPressed())
                flags |= InputFlag.Down;
            if (_playerControls.Player.MoveLeft.IsPressed())
                flags |= InputFlag.Left;
            if (_playerControls.Player.MoveRight.IsPressed())
                flags |= InputFlag.Right;
            if (_playerControls.Player.Attack.IsPressed())
                flags |= InputFlag.Weak;
            if (_playerControls.Player.Special1.IsPressed())
                flags |= InputFlag.Special1;
            if (_playerControls.Player.Special2.IsPressed())
                flags |= InputFlag.Special2;
            if (_playerControls.Player.Burst.IsPressed())
                flags |= InputFlag.Burst;
            if (_playerControls.Player.Shuffle.IsPressed())
                flags |= InputFlag.Shuffle;


            if (_playerControls.Player.ChargeAttack.IsPressed())
            {
                TryChargeAttack();
            }
            else if (_chargingAttack)
            {
                _chargingAttack = false;
                _onChargeEnded?.Raise(Character);
                _chargeAction?.Stop();
                flags |= InputFlag.Strong;
            }


            GridGame.SetPlayerInput(PlayerID, (long)flags);

            // Check each input and log which ones are being pressed
            //if (_playerControls.Player.MoveUp.IsPressed())
            //{
            //    Debug.Log("Move Up button is being pressed");
            //}

            //if (_playerControls.Player.MoveDown.IsPressed())
            //{
            //    Debug.Log("Move Down button is being pressed");
            //}

            //if (_playerControls.Player.MoveLeft.IsPressed())
            //{
            //    Debug.Log("Move Left button is being pressed");
            //}

            //if (_playerControls.Player.MoveRight.IsPressed())
            //{
            //    Debug.Log("Move Right button is being pressed");
            //}

            //if (_playerControls.Player.Attack.IsPressed())
            //{
            //    Debug.Log("Attack button is being pressed");
            //}

            //if (_playerControls.Player.Special1.IsPressed())
            //{
            //    Debug.Log("Special1 button is being pressed");
            //}

            //if (_playerControls.Player.Special2.IsPressed())
            //{
            //    Debug.Log("Special2 button is being pressed");
            //}

            //if (_playerControls.Player.Burst.IsPressed())
            //{
            //    Debug.Log("Burst button is being pressed");
            //}

            //if (_playerControls.Player.Shuffle.IsPressed())
            //{
            //    Debug.Log("Shuffle button is being pressed");
            //}
        }

        public override void Serialize(BinaryWriter bw)
        {
        }

        public override void Deserialize(BinaryReader br)
        {
        }

        // Start is called before the first frame update
        void Start()
        {
            Entity = GetComponentInChildren<EntityDataBehaviour>();
            Entity.Data.AddComponent(this);
            _stateMachineBehaviour = GetComponentInChildren<CharacterStateMachineBehaviour>();
            _gridMovement = Character.GetComponent<Movement.GridMovementBehaviour>();
            _moveset = Character.GetComponent<MovesetBehaviour>();
            _defense = Character.GetComponent<CharacterDefenseBehaviour>();
            _gridMovement.AddOnMoveDisabledAction(() => _storedMoveInput = Vector3.zero);
            _knockbackBehaviour = Character.GetComponent<KnockbackBehaviour>();
            _knockbackBehaviour.AddOnTakeDamageAction(DisableCharge);
            _defaultSpeed = _gridMovement.Speed;
            MatchManagerBehaviour.Instance.AddOnMatchPauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate);
            MatchManagerBehaviour.Instance.AddOnMatchUnpauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually);
        }

        private void OnEnable()
        {
            PlayerControls.Enable();
            PlayerControls.devices = _devices;
        }

        private void OnDisable()
        {
            PlayerControls.Disable();
        }

        private void TryChargeAttack()
        {
            if (!_stateMachineBehaviour.CompareState("Idle", "Moving", "Attacking") || _chargingAttack)
            {
                if (!_chargingAttack)
                    _canBufferAbility = false;

                return;
            }

            _canBufferAbility = true;
            _chargingAttack = true;
            _chargeAction = FixedPointTimer.StartNewTimedAction(() => _onChargeStarted?.Raise(Character), _minChargeLimit);
        }

        private void DisableCharge()
        {
            _canBufferAbility = false;
            _onChargeEnded?.Raise(Character);
            _chargeAction?.Stop();
            NormalAttackButtonDown = false;
            _abilityBuffered = false;
            _chargingAttack = false;
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
        public void BufferNormalAbility()
        {
            if (!_stateMachineBehaviour.CompareState("Idle", "Moving", "Attacking"))
                return;

            object[] args = new object[2];

            AbilityType abilityType;
            _attackDirection.X *= Mathf.Round(transform.forward.x);

            //Decide which ability type to use based on the input
            if (_attackDirection.Y != 0)
                abilityType = AbilityType.WEAKSIDE;
            else if (_attackDirection.X < 0)
                abilityType = AbilityType.WEAKBACKWARD;
            else if (_attackDirection.X > 0)
                abilityType = AbilityType.WEAKFORWARD;
            else
                abilityType = AbilityType.WEAKNEUTRAL;

            //Assign the arguments for the ability
            args[1] = _attackDirection;
            args[0] = new Fixed32(0);

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(() => { _abilityBuffered = false; UseAbility(abilityType, args); _onChargeEnded?.Raise(Character); }, 
                condition =>
                {
                    return _moveset.GetCanUseAbility() && _stateMachineBehaviour.CompareState("Idle", "Moving", "Attacking");
                }, 0.2f);

            _abilityBuffered = true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        /// Index 0 is always the power scale.
        /// index 1 is always the direction of input.</param>
        public void BufferChargeNormalAbility()
        {
            if (!_canBufferAbility)
                return;

            object[] args = new object[2];

            AbilityType abilityType;
            _attackDirection.X *= Mathf.Round(transform.forward.x);

            //Decide which ability type to use based on the input
            if (_attackDirection.Y != 0)
                abilityType = AbilityType.WEAKSIDE;
            else if (_attackDirection.X < 0)
                abilityType = AbilityType.WEAKBACKWARD;
            else if (_attackDirection.X > 0)
                abilityType = AbilityType.WEAKFORWARD;
            else
                abilityType = AbilityType.WEAKNEUTRAL;

            //Assign the arguments for the ability
            args[1] = _attackDirection;
            args[0] = 0.0f;
            abilityType += 4;
            Fixed32 powerScale = _minChargeLimit * 0.1f + 1;

            //Find the power scale based on the time the button was held to use a charge ability
            Fixed32 timeHeld = Fixed32.Clamp(_chargeHoldTime, 0, _maxChargeTime);
            if (timeHeld > _minChargeLimit)
            {
                powerScale = timeHeld * 0.1f + 1;
            }

            args[0] = powerScale;

            _bufferedAction = new BufferedInput(() => { _abilityBuffered = false; UseAbility(abilityType, args); _onChargeEnded?.Raise(Character); },
            condition =>
            _moveset.GetCanUseAbility() &&
            (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" ||
            _stateMachineBehaviour.StateMachine.CurrentState == "Attacking" ||
            _stateMachineBehaviour.StateMachine.CurrentState == "Moving")
            && !FXManagerBehaviour.Instance.SuperMoveEffectActive, 0.2f);

            _abilityBuffered = true;
        }

        public void BufferUnblockableAbility(InputAction.CallbackContext context)
        {
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(() => UseAbility(AbilityType.UNBLOCKABLE, null), condition => { _abilityBuffered = false; return _moveset.GetCanUseAbility() && !_gridMovement.IsMoving; }, 0.2f);
            _abilityBuffered = true;
        }

        public void BufferBurst()
        {
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(() => UseAbility(AbilityType.BURST, null), condition => { _abilityBuffered = false; return true; }, 0.2f);
            _abilityBuffered = true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferSpecialAbility(int abilityNum)
        {
            object[] args = new object[2];
            AbilityType abilityType = AbilityType.SPECIAL;
            _attackDirection.X *= Mathf.Round(transform.forward.x);

            //Assign the arguments for the ability
            args[0] = abilityNum;
            args[1] = _attackDirection;

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(() => UseAbility(abilityType, args), condition =>
            { 
                _abilityBuffered = false;
                return _moveset.GetCanUseAbility() && !FXManagerBehaviour.Instance.SuperMoveEffectActive;
            }, 0.2f);
            _abilityBuffered = true;
        }

        private void BufferShuffle()
        {
            if (_moveset.LoadingShuffle || _moveset.DeckReloading)
                return;

            _bufferedAction = new BufferedInput(() => _moveset.ManualShuffle(), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving", 0.2f);
        }

        private void BufferPhaseShift(InputAction.CallbackContext context, params object[] args)
        {
            if (_defense.IsResting)
                return;

            Vector2 direction = (Vector2)args[0];
            _bufferedAction = new BufferedInput(() => _defense.ActivatePhaseShift((FixedPoints.FVector2)_attackDirection), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving", 0.2f);
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
            if (NormalAttackButtonDown || _defense.IsPhaseShifting || PlayerControls.Player.Move.ReadValue<Vector2>().magnitude != 0)
                return;
            else if (_bufferedAction == null && (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving"))
                _bufferedAction = new BufferedInput(() => _defense.BeginParry(), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle", 0.2f);
            else if (_bufferedAction == null)
                return;
            else if (!_bufferedAction.HasAction() && (_stateMachineBehaviour.StateMachine.CurrentState == "Idle" || _stateMachineBehaviour.StateMachine.CurrentState == "Moving"))
                _bufferedAction = new BufferedInput(() => _defense.BeginParry(), condition => _stateMachineBehaviour.StateMachine.CurrentState == "Idle", 0.2f);
        }

        /// <summary>
        /// Buffers input on the y axis
        /// </summary>
        /// <param name="y"></param>
        public void BufferMovement(Vector2 direction)
        {
            //Don't allow current movement buffer to be overwritten.
            if (_movementBuffered || !_canMove)
                return;
            
            _storedMoveInput = direction;

            _movementBuffered = true;
            
            _bufferedAction = new BufferedInput(Move, condition => _storedMoveInput.magnitude > 0 && !_gridMovement.IsMoving && _canMove && _gridMovement.CanMove, 0.2f);
            _bufferedAction.OnClearAction += () => _movementBuffered = false;
        }

        private void Move()
        {
            _gridMovement.Move((FVector2)_storedMoveInput, clampPosition: true);
            _movementBuffered = false;
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
            if (_attackDirection.GetNormalized() == _gridMovement.MoveDirection.GetNormalized() || _gridMovement.MoveDirection == FVector2.Zero)
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
            PlayerControls.Disable();
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
            Vector2 newMoveInput = PlayerControls.Player.Move.ReadValue<Vector2>();
            if (newMoveInput == Vector2.zero)
                return;

            if (_holdToMove && _storedMoveInput == newMoveInput)
                _gridMovement.Speed = _holdSpeed;
            else if (_stateMachineBehaviour.StateMachine.CurrentState != "Moving")
                _gridMovement.Speed = _defaultSpeed;

            _storedMoveInput = newMoveInput;
            if (_storedMoveInput.magnitude == 1 && _canMove)
                _gridMovement.MoveToPanel(_gridMovement.Position + (FVector2)_storedMoveInput);

            //Debug.Log(_gridMovement.Speed);
        }

        public static void OnActionDown(UnityAction action)
        {
            _onActionButtonDown += action;
        }

        // Update is called once per frame
        public override void Tick(Fixed32 dt)
        {
            if (!PlayerActionButtonDown && _attackButtonDown)
            {
                _onActionButtonDown?.Invoke();
            }

            PlayerActionButtonDown = _attackButtonDown;

            if (_moveset.AbilityInUse)
                _canMove = CheckInputAllowedInAbilityPhase() || _stateMachineBehaviour.StateMachine.CurrentState != "Attacking";
            else if (_stateMachineBehaviour.StateMachine.CurrentState == "Idle")
                _canMove = true;

            //Checks to see if input can be enabled 
            if (_inputEnableCondition != null)
                if (_inputEnableCondition.Invoke())
                {
                    PlayerControls.Player.Enable();
                    _inputEnabled = true;
                    _inputEnableCondition = null;
                }

            if (!_inputEnabled)
            {
                ClearBuffer();
                return;
            }

            if (_abilityBuffered)
                _movementBuffered = false;

            //if (_holdToMove && !_abilityBuffered)
            //    CheckMoveInput();

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
            Vector2 attackDirInput = PlayerControls.Player.AttackDirection.ReadValue<Vector2>();

            //If there is a direction input, update the attack direction buffer and the time of input
            if (attackDirInput.magnitude > 0)
            {
                _attackDirection = new FVector2(Mathf.Round(attackDirInput.x), Mathf.Round(attackDirInput.y));
                _timeOfLastDirectionInput = Time.time;
            }

            //Clear the buffer if its exceeded the alotted time
            if (Time.time - _timeOfLastDirectionInput > _attackDirectionBufferClearTime)
                _attackDirection = FVector2.Zero;

            if (_bufferedAction?.HasAction() == true)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            if (Keyboard.current.tabKey.isPressed)
                DecisionDisplayBehaviour.DisplayText = !DecisionDisplayBehaviour.DisplayText;

            if (_chargingAttack)
                _chargeHoldTime += dt;
            else
                _chargeHoldTime = 0;
        }

    }
}

