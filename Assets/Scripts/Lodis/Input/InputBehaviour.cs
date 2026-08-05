using UnityEngine;
using UnityEngine.InputSystem;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.FX;
using FixedPoints;
using System;
using Types;
using System.IO;
using System.Collections.Generic;

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
        Shuffle = 1 << 9,
        Pause = 1 << 10
    }

    /// <summary>
    /// Stores an action that will take place once some condition is met.
    /// </summary>
    public class BufferedInput
    {
        private int _bufferClearFrame;
        private int _bufferStartFrame;
        private InputFlag _storedInput;

        public int BufferStartFrame { get => _bufferStartFrame; set => _bufferStartFrame = value; }
        public int BufferClearFrame { get => _bufferClearFrame; private set => _bufferClearFrame = value; }
        public InputFlag StoredInput { get => _storedInput; }  
        public bool HasInput => _storedInput != InputFlag.NONE;
        public int FramesLeft
        {
            get
            {
                if (!HasInput)
                    return 0;

                int elapsedTime = GridGameManager.FrameNumber - BufferStartFrame;
                return Fixed32.Max(0, BufferClearFrame - elapsedTime);
            }
        }


        public BufferedInput(InputFlag input, int bufferClearTime)
        {
            BufferClearFrame = bufferClearTime;
            Init(input);
        }

        public void Init(InputFlag input)
        {
            _storedInput = input;

            BufferStartFrame = input == InputFlag.NONE ? -1 : GridGameManager.FrameNumber;
        }

        public void Update()
        {
            if (!HasInput)
                return;

            if (GridGameManager.FrameNumber - BufferStartFrame > BufferClearFrame)
            {
                _storedInput = InputFlag.NONE;
                //BufferStartTime = -2;
            }
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
        [SerializeField] private Fixed32 _maxChargeTime = 1;
        [Tooltip("The amount of time needed to clear the buffer when a direction is pressed.")]
        [SerializeField] private Fixed32 _attackDirectionBufferClearTime = new Fixed32(3276);
        [Tooltip("The amount of time to wait before clearing the last input stored in the buffer.")]
        [SerializeField] private int _bufferClearFrames = 6;

        [Header("Toggles")]
        [SerializeField] private bool _canMove = true;
        [SerializeField] private bool _holdToMove;
        [SerializeField] private bool _inputEnabled = true;
        [SerializeField] private bool _aiControlled;
        [SerializeField] private bool _snapMovement;

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
        private static UnityAction _onActionButtonUp;
        private PlayerControls _playerControls;
        private BufferedInput _bufferedAction;
        private Ability _lastAbilityUsed = null;

        private InputDevice[] _devices;
        private FixedTimeAction _chargeAction;

        private FVector2 _attackDirection;
        private Fixed32 _timeOfLastDirectionInput;
        private float _defaultSpeed;
        private Fixed32 _chargeHoldTime;
        private Fixed32 _lastBufferedActionStartTime;
        private Fixed32 _lastProcessedGridGameTime;

        private static bool _playerActionButtonDown;
        private bool _isPaused;
        private bool _canBufferDefense;
        private bool _canBufferAbility = true;
        private bool _weakAttackButtonDown;
        private bool _chargingAttack;
        private bool _special1Down;
        private bool _special2Down;
        private bool _canTogglePause;

        public delegate void OnInputReceived(InputFlag input, EntityDataBehaviour entity);
        public static event OnInputReceived OnInputReceivedEvent;

        private BufferedInput[] _bufferedInputs = new BufferedInput[7];
        private int _currentBufferInputIndex;

        private InputFlag _aiFlags;
        private InputFlag _lastActionBuffered;

        private UnityAction _onGetFlags;

        public static Queue<InputFlag> TestInputList = new Queue<InputFlag>();

        private bool MovementBuffered
        {
            get => (_bufferedAction.StoredInput == InputFlag.Up ||
             _bufferedAction.StoredInput == InputFlag.Down ||
             _bufferedAction.StoredInput == InputFlag.Left ||
             _bufferedAction.StoredInput == InputFlag.Right);
        }

        private InputFlag _abilityFlag = InputFlag.Weak | InputFlag.Strong | InputFlag.Special1 | InputFlag.Special2 | InputFlag.Burst;

        private bool AbilityBuffered { get => (_bufferedAction.StoredInput & _abilityFlag) != 0; }

        public static UnityAction OnApplicationQuit;
        private InputFlag flags;

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
            set
            {
                _attackDirection = value;
                _timeOfLastDirectionInput = GridGame.Time;
            }
        }

        public GameObject Character { get => _character; set => _character = value; }
        public bool Enabled { get => _inputEnabled; set => _inputEnabled = value; }
        public bool NormalAttackButtonDown { get => _weakAttackButtonDown; private set => _weakAttackButtonDown = value; }

        public bool AIControlled { get => _aiControlled; set => _aiControlled = value; }
        public InputFlag AIFlags { get => _aiFlags; set => _aiFlags = value; }

        public static bool PlayerActionButtonDown { get => _playerActionButtonDown; private set => _playerActionButtonDown = value; }
        public PlayerControls PlayerControls { get => _playerControls; private set => _playerControls = value; }
        public bool ChargingAttack { get => _chargingAttack; private set => _chargingAttack = value; }
        public InputFlag Flags { get => flags; set => flags = value; }
        public UnityAction OnGetFlags { get => _onGetFlags; set => _onGetFlags = value; }

        public override string LogName => "InputBehaviour";

        protected override void Awake()
        {
            if (!AIControlled)
            {
                PlayerControls = new PlayerControls();
            }

            //--Old input initialization
            ////Initialize action delegates
            ////Movement input
            //if (!_holdToMove)
            //{
            //    PlayerControls.Player.MoveUp.started += context => TryUseMovement(Vector2.up);
            //    PlayerControls.Player.MoveDown.started += context => TryUseMovement(Vector2.down);
            //    PlayerControls.Player.MoveLeft.started += context => TryUseMovement(Vector2.left);
            //    PlayerControls.Player.MoveRight.started += context => TryUseMovement(Vector2.right);
            //}

            ////Ability input
            //PlayerControls.Player.Attack.started += context => { NormalAttackButtonDown = true; };
            //PlayerControls.Player.Attack.canceled += context => NormalAttackButtonDown = false;
            //PlayerControls.Player.Attack.performed += context => { TryUseNormalAbility(context, new object[2]);};
            //PlayerControls.Player.ChargeAttack.started += context => { NormalAttackButtonDown = true; TryChargeAttack(); };
            //PlayerControls.Player.ChargeAttack.performed += context => { TryUseChargeNormalAbility(context, new object[2]); _onChargeEnded?.Raise(Character); _chargeAction?.Disable(); };
            //PlayerControls.Player.Special1.started += context => { TryUseSpecialAbility(context, new object[2] { 0, 0 });  _special1Down = true; };
            //PlayerControls.Player.Special1.canceled += context => { _special1Down = false; };

            //PlayerControls.Player.Special2.started += context => { TryUseSpecialAbility(context, new object[2] { 1, 0 });  _special2Down = true; };
            //PlayerControls.Player.Special2.canceled += context => { _special2Down = false; };
            //PlayerControls.Player.Burst.started += TryUseBurst;
            //PlayerControls.Player.Shuffle.started += TryUseShuffle;

            //PlayerControls.Player.Pause.started += context => { MatchManagerBehaviour.Instance.TogglePauseMenu(); ClearBuffer(); };

            _defaultSpeed = _holdSpeed;

            _bufferedAction = new BufferedInput(InputFlag.NONE, _bufferClearFrames);

            if (MatchManagerBehaviour.Instance)
            {
                MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ClearBuffer);
            }
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
            _knockbackBehaviour = Character.GetComponent<KnockbackBehaviour>();
            _knockbackBehaviour.AddOnTakeDamageAction(DisableCharge);
            _defaultSpeed = _gridMovement.Speed;
            MatchManagerBehaviour.Instance.AddOnMatchPauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate);
            MatchManagerBehaviour.Instance.AddOnMatchUnpauseAction(() => InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate);
        }

        private void OnEnable()
        {
            PlayerControls.Enable();
            PlayerControls.devices = _devices;

            //Instead of listening to input events from unity we will instead listen to custom GGPO input events.
            GridGame.OnPollInput += GridGame_PollInput;
            GridGame.OnProcessInput += GridGame_ProcessInput;
        }

        private void OnDisable()
        {
            PlayerControls.Disable();
            GridGame.OnPollInput -= GridGame_PollInput;
            GridGame.OnProcessInput -= GridGame_ProcessInput;
        }

        /// <summary>
        /// Called every GGPO frame and is used to parse the current inputs. Inputs could be changed during rollback. This function should catch that and update the buffered action.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inputs"></param>
        private void GridGame_ProcessInput(int id, long inputs)
        {
            if (id != PlayerID || !MatchManagerBehaviour.Instance.MatchStarted || !_inputEnabled)
                return;


            InputFlag currentInputFlag = (InputFlag)inputs;

            _lastProcessedGridGameTime = GridGame.Time;


            // if (PlayerID == 1)
            // {
            //     Debug.Log("Player2 input processed.");
            // }

            // if (PlayerID == 0)
            // {
            //     Debug.Log("Player1 input processed.");
            // }
            bool isDirectionalInput = false;
            bool inputActionCompleted = true;

            //If we didnt get a new input here, check the buffer.
            if (_bufferedAction.StoredInput != 0)
            {
                currentInputFlag = _bufferedAction.StoredInput;
            }

            if ((currentInputFlag & InputFlag.Pause) != 0 && _canTogglePause)
            {
                MatchManagerBehaviour.Instance.TogglePauseMenu();
                _canTogglePause = false;
                return;
            }
            else if ((currentInputFlag & InputFlag.Pause) == 0)
            {
                _canTogglePause = true;
            }

            if ((currentInputFlag & InputFlag.Up) != 0)
            {
                _attackDirection = new FVector2(0, 1);
                // Call the function related to Up input
                inputActionCompleted = TryUseMovement(new FVector2(0, 1));

                if (PlayerID == 0)
                    TestInputList.Enqueue(InputFlag.Up);

                isDirectionalInput = true;
            }
            else if ((currentInputFlag & InputFlag.Down) != 0)
            {
                _attackDirection = new FVector2(0, -1);
                // Call the function related to Down input
                inputActionCompleted = TryUseMovement(new FVector2(0, -1));

                if (PlayerID == 0)
                    TestInputList.Enqueue(InputFlag.Down);

                isDirectionalInput = true;
            }
            else if ((currentInputFlag & InputFlag.Left) != 0)
            {
                _attackDirection = new FVector2(-1, 0);
                // Call the function related to Left input
                inputActionCompleted = TryUseMovement(new FVector2(-1, 0));

                if (PlayerID == 0)
                    TestInputList.Enqueue(InputFlag.Left);

                isDirectionalInput = true;
            }
            else if ((currentInputFlag & InputFlag.Right) != 0)
            {
                _attackDirection = new FVector2(1, 0);
                // Call the function related to Right input
                inputActionCompleted = TryUseMovement(new FVector2(1, 0));

                if (PlayerID == 0)
                    TestInputList.Enqueue(InputFlag.Right);

                isDirectionalInput = true;
            }

            // If no directional input was detected, enqueue InputFlag.None
            if (!isDirectionalInput)
            {
                _attackDirection = FVector2.Zero;
                if (PlayerID == 0)
                    TestInputList.Enqueue(InputFlag.NONE);
            }

            if ((currentInputFlag & InputFlag.Weak) != 0)
            {
                // Call the function related to Weak attack
                inputActionCompleted = TryUseNormalAbility();
            }
            if ((currentInputFlag & InputFlag.Strong) != 0)
            {
                TryChargeAttack();
            }
            else if (ChargingAttack)
            {
                // Call the function related to Strong attack
                inputActionCompleted = TryUseChargeNormalAbility();
            }
            if (!_special1Down && (currentInputFlag & InputFlag.Special1) != 0)
            {
                // Call the function related to Special1
                inputActionCompleted = TryUseSpecialAbility(0);
            }
            _special1Down = (currentInputFlag & InputFlag.Special1) != 0;

            if (!_special2Down && (currentInputFlag & InputFlag.Special2) != 0)
            {
                // Call the function related to Special2
                inputActionCompleted = TryUseSpecialAbility(1);
            }

            _special2Down = (currentInputFlag & InputFlag.Special2) != 0;

            if ((currentInputFlag & InputFlag.Burst) != 0)
            {
                // Call the function related to Burst
                inputActionCompleted = TryUseBurst();
            }
            if ((currentInputFlag & InputFlag.Shuffle) != 0)
            {
                // Call the function related to Shuffle
                inputActionCompleted = TryUseShuffle();
            }

            //If we couldn't complete the action this time, store the input in the buffer to try again next frame.
            if (!inputActionCompleted && _bufferedAction.StoredInput != currentInputFlag)
            {
                _bufferedAction.Init(currentInputFlag);
                _lastBufferedActionStartTime = _bufferedAction.BufferStartFrame;
            }
            //If a new input was recieved and we were able to complete the action, clear the buffer.
            else if (inputActionCompleted && inputs != 0)
            {
                ClearBuffer();
            }

            _bufferedAction.Update();

        }

        /// <summary>
        /// Sets the current input flags for this player.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void GridGame_PollInput(int id)
        {
            //Debug.Log("Poll id is " + id);
            if (id == PlayerID)
            {
                GetInputFlags();
            }
        }

        private void GetInputFlags()
        {
            // if (PlayerID == 0)
            // {
            //     Debug.Log("Player1 input polled.");
            // }
            // if (PlayerID == 1)
            // {
            //     Debug.Log("Player2 input polled.");
            // }

            Flags = InputFlag.NONE;

            OnGetFlags?.Invoke();   

            if (!AIControlled)
            {
                if (_playerControls.Player.MoveUp.IsPressed())
                    Flags |= InputFlag.Up;
                if (_playerControls.Player.MoveDown.IsPressed())
                    Flags |= InputFlag.Down;
                if (_playerControls.Player.MoveLeft.IsPressed())
                    Flags |= InputFlag.Left;
                if (_playerControls.Player.MoveRight.IsPressed())
                    Flags |= InputFlag.Right;
                if (_weakAttackButtonDown = _playerControls.Player.Attack.IsPressed())
                    Flags |= InputFlag.Weak;
                if (_playerControls.Player.Special1.IsPressed())
                    Flags |= InputFlag.Special1;
                if (_playerControls.Player.Special2.IsPressed())
                    Flags |= InputFlag.Special2;
                if (_playerControls.Player.Burst.IsPressed())
                    Flags |= InputFlag.Burst;
                if (_playerControls.Player.Shuffle.IsPressed())
                    Flags |= InputFlag.Shuffle;
                if (_playerControls.Player.ChargeAttack.IsPressed())
                    Flags |= InputFlag.Strong;
                if (_playerControls.Player.Pause.IsPressed())
                    Flags |= InputFlag.Pause;

                if (Flags != InputFlag.NONE)
                {
                    OnInputReceivedEvent?.Invoke(Flags, Entity);
                }
            }
            else
            {
                Flags = AIFlags;
            }

            GridGame.SetPlayerInput(PlayerID, (long)Flags);

            //---Debug commands

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

            //if (_playerControls.Player.Attack.IsPressed() && PlayerID == 1)
            //{
            //    Debug.Log("Attack button is being pressed " + PlayerID.Value);
            //}

            //if (_playerControls.Player.Attack.IsPressed() && PlayerID == 0)
            //{
            //    Debug.Log("Attack button is being pressed " + PlayerID.Value);
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
            bw.Write(_canMove);
            bw.Write(_inputEnabled);
            bw.Write((long)_bufferedAction.StoredInput);
            bw.Write(_bufferedAction.BufferStartFrame);
        }

        public override void Deserialize(BinaryReader br)
        {
            _canMove = br.ReadBoolean();
            _inputEnabled = br.ReadBoolean();
            InputFlag storedInput = (InputFlag)br.ReadInt64();
            int bufferStartFrame = br.ReadInt32();

            _bufferedAction.Init(storedInput);
            _bufferedAction.BufferStartFrame = bufferStartFrame;

            _lastBufferedActionStartTime = bufferStartFrame;
            _lastProcessedGridGameTime = GridGameManager.FrameNumber;
        }


        /// <summary>
        /// Hashes the serialized input buffering and directional state so input-side
        /// divergences can be isolated during rollback debugging.
        /// </summary>
        protected override string[] GetLogItems()
        {
            return new string[]
            {
                $"Attack Direction={_attackDirection}",
                $"Buffered Action Start Frame={_bufferedAction.BufferStartFrame}",
                $"Buffered Action Input={_bufferedAction.StoredInput}",
                $"Buffered Action Frames Left={_bufferedAction.FramesLeft}",
                $"Last Buffered Action Start Time={_lastBufferedActionStartTime}",
                $"Last GridGame Time={_lastProcessedGridGameTime}"
            };
        }

        private void TryChargeAttack()
        {
            if (!_stateMachineBehaviour.CompareState("Idle", "Moving", "Attacking") || ChargingAttack)
            {
                if (!ChargingAttack)
                    _canBufferAbility = false;

                return;
            }

            _canBufferAbility = true;
            ChargingAttack = true;
            _chargeAction = FixedPointTimer.StartNewTimedAction(() => _onChargeStarted?.Raise(Character), _minChargeLimit);
        }

        private void DisableCharge()
        {
            _canBufferAbility = false;
            _onChargeEnded?.Raise(Character);
            _chargeAction?.Stop();
            NormalAttackButtonDown = false;
            ChargingAttack = false;
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
        public bool TryUseNormalAbility()
        {
            //First check and see if we should buffer this ability
            if (!_moveset.GetCanUseAbility() || !_stateMachineBehaviour.CompareState("Idle", "Moving", "Attacking"))
            {
                return false;
            }

            object[] args = new object[2];

            //if (PlayerID == 1)
            //    Debug.Log("Ability buffered");

            AbilityType abilityType;
            _attackDirection.X *= Fixed32.Round(FixedTransform.Forward.X);

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

            UseAbility(abilityType, args);
            _onChargeEnded?.Raise(Character);

            return true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        /// Index 0 is always the power scale.
        /// index 1 is always the direction of input.</param>
        public bool TryUseChargeNormalAbility()
        {
            ChargingAttack = false;
            _onChargeEnded?.Raise(Character);
            _chargeAction?.Stop();

            if (!_canBufferAbility)
                return false;

            if (!_moveset.GetCanUseAbility() ||
                !_stateMachineBehaviour.CompareState("Idle", "Moving", "Attacking") ||
                FXManagerBehaviour.Instance.SuperMoveEffectActive)
            {
                return false;
            }

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
            Fixed32 powerScale = _minChargeLimit + 1;

            //Find the power scale based on the time the button was held to use a charge ability
            Fixed32 timeHeld = Fixed32.Clamp(_chargeHoldTime, 0, _maxChargeTime);
            if (timeHeld > _minChargeLimit)
            {
                powerScale = timeHeld + 1;
            }

            args[0] = powerScale;
            UseAbility(abilityType, args);
            _onChargeEnded?.Raise(Character);
            return true;
        }

        public bool TryUseUnblockableAbility(InputAction.CallbackContext context)
        {
            if (!_moveset.GetCanUseAbility() || _gridMovement.IsMoving)
            {
                return false;
            }

            UseAbility(AbilityType.UNBLOCKABLE, null);
            return true;
        }

        public bool TryUseBurst()
        {
            UseAbility(AbilityType.BURST, null);
            return true;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public bool TryUseSpecialAbility(int abilityNum)
        {
            if (!_moveset.GetCanUseAbility() || FXManagerBehaviour.Instance.SuperMoveEffectActive)
            {
                return false;
            }

            object[] args = new object[2];
            AbilityType abilityType = AbilityType.SPECIAL;
            _attackDirection.X *= Mathf.Round(transform.forward.x);

            //Assign the arguments for the ability
            args[0] = abilityNum;
            args[1] = _attackDirection;

            UseAbility(abilityType, args);

            return true;
        }

        private bool TryUseShuffle()
        {
            if (_moveset.LoadingShuffle || _moveset.DeckReloading || !_stateMachineBehaviour.CompareState("Idle", "Moving"))
                return false;

            _moveset.ManualShuffle();
            return true;
        }

        private bool TryUsePhaseShift(InputAction.CallbackContext context, params object[] args)
        {
            if (_defense.IsResting || !_stateMachineBehaviour.CompareState("Idle", "Moving"))
                return false;

            Vector2 direction = (Vector2)args[0];
            _defense.ActivatePhaseShift((FixedPoints.FVector2)_attackDirection);
            return true;
        }

        private void RemoveShieldFromBuffer()
        {
            _defense.DeactivateShield();
        }

        /// <summary>
        /// Buffers a parry only if the attack button is not being pressed
        /// </summary>
        /// <param name="context"></param>
        public bool TryUseShield()
        {
            if (NormalAttackButtonDown || _defense.IsPhaseShifting || PlayerControls.Player.Move.ReadValue<Vector2>().magnitude != 0)
                return false;

            if (!_stateMachineBehaviour.CompareState("Idle", "Moving"))
                return false;

            _defense.BeginParry();
            return true;
        }

        /// <summary>
        /// Buffers input on the y axis
        /// </summary>
        /// <param name="y"></param>
        public bool TryUseMovement(FVector2 direction)
        {
            return _canMove && _gridMovement.Move(direction, clampPosition: true, snapPosition: _snapMovement);
        }

        /// <summary>
        /// Removes the last input from the input buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _bufferedAction.Init(InputFlag.NONE);
            _bufferedAction.BufferStartFrame = -3;
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

        public static void OnActionDown(UnityAction action)
        {
            _onActionButtonDown += action;
        }

        public override void Tick(Fixed32 dt)
        {
            if (PlayerID == 0)
            {
                if (!PlayerActionButtonDown && _weakAttackButtonDown)
                {
                    _onActionButtonDown?.Invoke();
                }

                PlayerActionButtonDown = _weakAttackButtonDown;
            }

            if (_moveset.AbilityInUse)
                _canMove = CheckInputAllowedInAbilityPhase() || _stateMachineBehaviour.StateMachine.CurrentState != "Attacking";
            else if (_stateMachineBehaviour.StateMachine.CurrentState == "Idle")
                _canMove = true;

            //Checks to see if input can be enabled 
            if (_inputEnableCondition != null)
            {
                if (_inputEnableCondition.Invoke())
                {
                    PlayerControls.Player.Enable();
                    _inputEnabled = true;
                    _inputEnableCondition = null;
                }
            }

            if (!_inputEnabled)
            {
                ClearBuffer();
                return;
            }



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
                if (!_bufferedAction.HasInput)
                    EnableMovement();
            }

            //Stores the current attack direction input
            Vector2 attackDirInput = PlayerControls.Player.AttackDirection.ReadValue<Vector2>();

            //If there is a direction input, update the attack direction buffer and the time of input
            if (attackDirInput.magnitude > 0)
            {
                _attackDirection = new FVector2(Mathf.Round(attackDirInput.x), Mathf.Round(attackDirInput.y));
                _timeOfLastDirectionInput = GridGame.Time;
            }

            //Clear the buffer if its exceeded the alotted time
            if (GridGame.Time - _timeOfLastDirectionInput > _attackDirectionBufferClearTime)
                _attackDirection = FVector2.Zero;


            if (Keyboard.current.tabKey.isPressed)
                DecisionDisplayBehaviour.DisplayText = !DecisionDisplayBehaviour.DisplayText;

            if (ChargingAttack)
                _chargeHoldTime += dt;
            else
                _chargeHoldTime = 0;
        }

    }
}

