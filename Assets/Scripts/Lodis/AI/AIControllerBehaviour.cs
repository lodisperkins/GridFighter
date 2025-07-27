using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BBUnity;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.ScriptableObjects;
using Lodis.FX;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Runtime.Remoting.Messaging;
using Lodis.Utility;
using Assets.Scripts.Lodis.AI;
using FixedPoints;
using Types;
using System.IO;
using static PixelCrushers.DialogueSystem.ActOnDialogueEvent;
using System.Threading.Tasks;
using PixelCrushers;

namespace Lodis.AI
{
    public class AIControllerBehaviour : SimulationBehaviour, IControllable
    {
        public enum AIState
        {
            Idle,
            Attacking,
            Defending,
            None
        }

        [SerializeField]
        private GameObject _character;
        [Tooltip("Sets the value that amplifies the power of strong attacks when doing them randomly.")]
        [SerializeField]
        private float _attackStrength;
        [SerializeField]
        private float _maxRange;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private FVector2 _attackDirection;
        [SerializeField]
        private bool _enableRandomBehaviour;
        [SerializeField]
        private Collider _senseCollider;
        [SerializeField]
        private bool _canAttack = true;

        [SerializeField]
        private int _maxDecisionCount;
        [Tooltip("The amount of time the dummy has to be in knock back to consider using a burst.")]
        [SerializeField]
        private float _timeNeededToBurst;
        //private DefenseNode _lastDefenseDecision;

        [SerializeField]
        private bool _copyAttacks;
        [SerializeField]
        [Tooltip("The amount of time the AI will wait before saving information about the current situation.")]
        private float _saveStateDelay;
        [SerializeField]
        [Tooltip("How much to subtract from the win count for every decision made.")]
        private int _losePenalty;
        [SerializeField]
        [Tooltip("How much to add to the win count for every decision made.")]
        private int _winReward;
        [SerializeField]
        private string _recordingName = "AI";
        [SerializeField]
        [Tooltip("Whether or not to use recording data for decision making.")]
        private bool _useRecording;
        [SerializeField]
        private int _randomDecisionConstant = 1;
        [SerializeField]
        [Tooltip("Recorded actions must have a score that is at least this value in order to be used.")]
        private float _accuracyMinimum;
        [SerializeField]
        [Tooltip("Recorded actions that have a score above this value when compared  cannot be used.")]
        private float _actionScoreMax;
        [SerializeField]
        [Tooltip("The last score found after comparing the current action situation to the current game state.")]
        private float _lastScore;
        [SerializeField]
        private AIState _currentState = AIState.Idle;

        [SerializeField] private Fixed32 _distanceAccuracyMinimum;
        [SerializeField] private Fixed32 _directionAccuracyMinimum;

        //[Header("Weights")]
        //[SerializeField]
        //[Tooltip("How important the direction the enemy is relative to the AI.")]
        //private float _directionWeight = 0.5f;
        //[SerializeField]
        //[Tooltip("How important the velocity the enemy is.")]
        //private float _opponentVelocityWeight = 0.8f;
        //[SerializeField]
        //[Tooltip("How important the distance between the enemy and the AI is")]
        //private float _distanceWeight = 0.7f;
        //[SerializeField]
        //[Tooltip("How important the direction and distance of enemy hit boxes are relative to the AI")]
        //private float _avgHitBoxOffsetWeight = 1.5f;
        //[SerializeField]
        //[Tooltip("How important the velocity of enemy hit boxes are relative to the AI")]
        //private float _avgVelocityWeight = 1.5f;
        //[SerializeField]
        //[Tooltip("How important the time remaining in the match is")]
        //private float _matchTimeRemainingWeight = 1;
        //[SerializeField]
        //[Tooltip("How important the opponent's current state is")]
        //private float _opponentStateWeight = 1;
        //[SerializeField]
        //[Tooltip("How important the opponent's current health is")]
        //private float _opponentHealthWeight = 1;

        //---
        private GridPhysicsBehaviour _opponentGridPhysics;
        private bool _isPaused;
        private FixedTimeAction _playbackRoutine;
        private bool _abilityBuffered;

        private MovesetBehaviour _opponentMoveset;
        private GridMovementBehaviour _movementBehaviour;
        private ActionPlaybackInfo[] _playbackInfo;
        private ActionPlaybackInfo _currentRecording;
        private int _currentActionIndex;
        private int _currentRecordingIndex;
        private ActionNode _currentSituation = new ActionNode(null, null);
        private GridPhysicsBehaviour _gridPhysics;
        private IntVariable _playerID;
        private BufferedInput _bufferedAction;
        private BehaviorExecutor _executor;

        private GameObject _opponent;
        private GridMovementBehaviour _opponentMove;
        private KnockbackBehaviour _opponentKnocback;
        private CharacterDefenseBehaviour _opponentDefense;

        private CharacterDefenseBehaviour _defense;
        private AIDummyMovementBehaviour _aiMovementBehaviour;
        private AttackDecisionTree _attackDecisions;
        private DefenseDecisionTree _defenseDecisions;
        private RingBarrierBehaviour _ownerBarrier;
        private RingBarrierBehaviour _opponentBarrier;

        private bool _touchingBarrier;
        private bool _touchingOpponentBarrier;
        private bool _lastActionWasCharge;
        private List<HitColliderBehaviour> _attacksInRange = new List<HitColliderBehaviour>();
        private CharacterStateMachineBehaviour _stateMachine;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private int _lastSlot;
        private Gameplay.MovesetBehaviour _moveset;
        private InputBehaviour _inputBehaviour;
        private FixedConditionAction changePlanAction;
        private bool _waitingToChangePlans;
        private bool _cantFindRecording;

        public CharacterStateMachineBehaviour StateMachine { get => _stateMachine; }
        public GameObject Opponent { get => _opponent; }
        public MovesetBehaviour Moveset { get => _moveset; set => _moveset = value; }
        public AIDummyMovementBehaviour AIMovement { get => _aiMovementBehaviour; }
        public AttackDecisionTree AttackDecisions { get => _attackDecisions; }
        public GridPhysicsBehaviour GridPhysics { get => _gridPhysics; }
        public BehaviorExecutor Executor { get => _executor; }
        public DefenseDecisionTree DefenseDecisions { get => _defenseDecisions; }
        public float TimeNeededToBurst { get => _timeNeededToBurst; }
        public IntVariable PlayerID { get => _playerID; set => _playerID = value; }
        public GameObject Character { get => _character; set => _character = value; }
        public float MaxRange { get => _maxRange; set => _maxRange = value; }
        public bool Enabled { get => enabled; set => enabled = value; }
        public GridMovementBehaviour OpponentMove { get => _opponentMove; private set => _opponentMove = value; }
        public KnockbackBehaviour OpponentKnockback { get => _opponentKnocback; private set => _opponentKnocback = value; }
        public KnockbackBehaviour Knockback { get => _knockbackBehaviour; private set => _knockbackBehaviour = value; }
        public CharacterDefenseBehaviour Defense { get => _defense; private set => _defense = value; }
        public CharacterDefenseBehaviour OpponentDefense { get => _opponentDefense; private set => _opponentDefense = value; }


        public bool CanAttack { get => _canAttack; private set => _canAttack = value; }

        public FVector2 AttackDirection
        {
            get
            {
                return _attackDirection;
            }
            set
            {
                _attackDirection = value;
            }
        }

        public bool TouchingBarrier { get => _touchingBarrier; set => _touchingBarrier = value; }
        public bool TouchingOpponentBarrier { get => _touchingOpponentBarrier; set => _touchingOpponentBarrier = value; }
        public bool CopyAttacks { get => _copyAttacks; set => _copyAttacks = value; }

        public bool HasBuffered { get => _bufferedAction?.HasAction() == true; }
        //public DefenseNode LastDefenseDecision { get => _lastDefenseDecision; set => _lastDefenseDecision = value; }

        public void LoadDecisions()
        {
            //_useRecording = PlayerID == 1;
            //EnableBehaviourTree = PlayerID != 1;
            //_executor.enabled = PlayerID != 1;
            //_aiMovementBehaviour.enabled = PlayerID != 1;
            Moveset = Character.GetComponent<Gameplay.MovesetBehaviour>();
            if (_useRecording)
            {
                _executor.enabled = false;

                _playbackInfo = SceneManagerBehaviour.Instance.GetRecordings(PlayerID);

                return;
            }

            _attackDecisions = new AttackDecisionTree();
            _attackDecisions.MaxDecisionsCount = _maxDecisionCount;
            _attackDecisions.Load(Character.name);
            _defenseDecisions = new DefenseDecisionTree();
            _defenseDecisions.MaxDecisionsCount = _maxDecisionCount;
            _defenseDecisions.Load(Character.name);

            if (Application.isEditor) return;

            MatchManagerBehaviour.Instance.AddOnApplicationQuitAction(() => _attackDecisions?.Save(Character.name));
            MatchManagerBehaviour.Instance.AddOnApplicationQuitAction(() => _defenseDecisions?.Save(Character.name));
        }

        protected override void Awake()
        {
            _executor = GetComponent<BehaviorExecutor>();
            _aiMovementBehaviour = GetComponent<AIDummyMovementBehaviour>();
            _inputBehaviour = GetComponent<InputBehaviour>();

            GridGame.OnProcessInput += (a,b) => _inputBehaviour.AIFlags = InputFlag.NONE;
        }

        private void Start()
        {
            Defense = Character.GetComponent<CharacterDefenseBehaviour>();
            _stateMachine = Character.GetComponent<Gameplay.CharacterStateMachineBehaviour>();
            Knockback = Character.GetComponent<Movement.KnockbackBehaviour>();
            _gridPhysics = Character.GetComponent<GridPhysicsBehaviour>();
            _movementBehaviour = Character.GetComponent<GridMovementBehaviour>();
            _moveset = Character.GetComponent<MovesetBehaviour>();

            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Character);
            OpponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            OpponentKnockback = _opponent.GetComponent<KnockbackBehaviour>();
            OpponentDefense = _opponent.GetComponent<CharacterDefenseBehaviour>();
            _opponentGridPhysics = _opponent.GetComponent<GridPhysicsBehaviour>();
            _opponentMoveset = _opponent.GetComponent<MovesetBehaviour>();

            _senseCollider.transform.SetParent(Character.transform);
            _senseCollider.transform.localPosition = Vector3.zero;

            _opponentBarrier = _movementBehaviour.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;


            CleanRecordings();

            if (_useRecording && _playbackInfo != null && _playbackInfo.Length > 0)
            {
                _inputBehaviour.OnGetFlags += HandleActionPlayback;
                MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ResetRecordingActions);
            }
            else
            {
                MatchManagerBehaviour.Instance.AddOnMatchOverAction(AddMatchReward);
            }

            //---Auto restart match for training
            //MatchManagerBehaviour.Instance.AddOnMatchOverAction(() =>
            //{
            //    if (MatchManagerBehaviour.Instance.LastMatchResult != MatchResult.DRAW)
            //        MatchManagerBehaviour.Instance.Restart();
            //});

            Entity = GetComponentInChildren<EntityDataBehaviour>();
            Entity.Data.AddComponent(this);
        }

        private void CleanRecordings()
        {
            if (_playbackInfo == null || _playbackInfo.Length == 0)
                return;

            // Remove empty recordings from the playback info array
            List<ActionPlaybackInfo> newRecordingArray = new();

            for (int i = 0;  i < _playbackInfo.Length; i++)
            {
                if (_playbackInfo[i].Recording.Count > 0)
                {
                   newRecordingArray.Add(_playbackInfo[i]);
                }
            }

            _playbackInfo = newRecordingArray.ToArray();
        }

        private void OnEnable()
        {
            if (_useRecording)
            {
                UnpausePlayback();
                return;
            }

            if (_executor)
                _executor.enabled = true;

            if (_aiMovementBehaviour)
                _aiMovementBehaviour.enabled = true;

        }

        private void OnDisable()
        {
            if (_useRecording)
            {
                PausePlayback();
                _playbackRoutine?.Stop();
                return;
            }

            _executor.enabled = false;
            _aiMovementBehaviour.enabled = false;
        }

        private void OnDestroy()
        {
            if (!Application.isEditor) return;

            _attackDecisions?.Save(Character.name);
            _defenseDecisions?.Save(Character.name);
        }

        private void AddMatchReward()
        {
            if (_useRecording)
                return;

            GridAlignment alignment = _aiMovementBehaviour.MovementBehaviour.Alignment;

            if (MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.P1WINS && alignment == GridAlignment.LEFT 
                || MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.P2WINS && alignment == GridAlignment.RIGHT)
            {
                _attackDecisions.AddRewardToDecisions(_winReward);
                _defenseDecisions.AddRewardToDecisions(_winReward);
            }
            else if (MatchManagerBehaviour.Instance.LastMatchResult != MatchResult.DRAW)
            {
                _attackDecisions.AddRewardToDecisions(_losePenalty);
                _defenseDecisions.AddRewardToDecisions(_losePenalty);
            }
        }

        public List<HitColliderBehaviour> GetAttacksInRange()
        {
            if (_attacksInRange.Count > 0)
            {
                _attacksInRange.RemoveAll(hitCollider =>
                {
                    if ((object)hitCollider != null)
                        return hitCollider == null || !hitCollider.gameObject.activeInHierarchy || hitCollider.Spawner == Entity.Data;

                    return true;
                });
            }
            return _attacksInRange;
        }

        public IEnumerator ChargeRoutine(float chargeTime, AbilityType type)
        {
            _lastActionWasCharge = true;
            yield return new WaitForSeconds(chargeTime);

            if ((StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Attacking"))
            {
                Moveset.UseBasicAbility(type, new object[] { _attackStrength, _attackDirection });
            }
            _lastActionWasCharge = false;
        }

        private void UseAbility(Ability ability, float attackStrength, Vector2 attackDirection)
        {
            //Uses the ability based on its type
            if (Moveset.GetAbilityNamesInCurrentSlots()[0] == ability.abilityData.name)
                Moveset.UseSpecialAbility(0, attackStrength, attackDirection);
            else if (Moveset.GetAbilityNamesInCurrentSlots()[1] == ability.abilityData.name)
                Moveset.UseSpecialAbility(1, attackStrength, attackDirection);
            else if (ability.abilityData.AbilityType != AbilityType.SPECIAL)
                Moveset.UseBasicAbility(ability, attackStrength, attackDirection);
            else return;
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferAction(Ability ability, float attackStrength, Vector2 attackDirection)
        {
            AbilityType abilityType = AbilityType.SPECIAL;
            _attackDirection.X *= Mathf.Round(transform.forward.x);

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(() => UseAbility(ability, attackStrength, attackDirection), condition =>
            {
                _abilityBuffered = false;
                return _moveset.GetCanUseAbility() && !FXManagerBehaviour.Instance.SuperMoveEffectActive;
            }, 0.2f);
            _abilityBuffered = true;
        }

        private FVector3 GetAverageVelocity()
        {
            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);
            FVector3 averageVelocity = FVector3.Zero;

            if (_attacksInRange == null) return FVector3.Zero;

            _attacksInRange.RemoveAll(physics =>
            {
                return (object)physics == null || !physics.gameObject.activeInHierarchy;
            });

            if (_attacksInRange.Count == 0)
                return FVector3.Zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                if (_attacksInRange[i].GridPhysics)
                    averageVelocity += _attacksInRange[i].GridPhysics.Velocity;

            return averageVelocity;
        }

        private Vector3 GetAveragePosition()
        {
            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);
            Vector3 averageDirection = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            _attacksInRange.RemoveAll(physics =>
            {
                return (object)physics == null || !physics.gameObject.activeInHierarchy;
            });

            for (int i = 0; i < _attacksInRange.Count; i++)
                averageDirection += _attacksInRange[i].gameObject.transform.position - Character.transform.position;

            return averageDirection;
        }

        private void PerformChargeAttack(ActionNode action)
        {

            if (action.InputAction.HasFlag(InputFlag.Up))
            {
                _inputBehaviour.AttackDirection = FVector2.Up;
            }
            else if (action.InputAction.HasFlag(InputFlag.Down))
            {
                _inputBehaviour.AttackDirection = FVector2.Down;
            }
            else if (action.InputAction.HasFlag(InputFlag.Left))
            {
                _inputBehaviour.AttackDirection = FVector2.Left;
            }
            else if (action.InputAction.HasFlag(InputFlag.Right))
            {
                _inputBehaviour.AttackDirection = FVector2.Right;
            }
            else
            {
                _inputBehaviour.AttackDirection = FVector2.Zero;
            }

            _inputBehaviour.BufferChargeNormalAbility();
        }

        private void PerformAction(ActionNode action)
        {
            InputFlag flag = action.InputAction;

            if (_gridPhysics.MovementBehaviour.Alignment == GridAlignment.RIGHT)
            {
                // Check if the action's input contains the flag for left and the grid alignment is right
                if (flag.HasFlag(InputFlag.Left))
                {
                    flag &= ~InputFlag.Left; // Remove the Left flag
                    flag |= InputFlag.Right; // Add the Right flag
                }
                // Check if the action's input contains the flag for right and the grid alignment is left
                else if (flag.HasFlag(InputFlag.Right))
                {
                    flag &= ~InputFlag.Right; // Remove the Right flag
                    flag |= InputFlag.Left; // Add the Left flag
                }
            }

            //If we have the special in one slot but the other special button was pressed in the recording swap it here.
            if (action.InputAction.HasFlag(InputFlag.Special1) && _moveset.SpecialAbilitySlots[1]?.abilityData.ID == action.CurrentAbilityID)
            {
                flag &= ~InputFlag.Special1; // Remove the Special1 flag
                flag |= InputFlag.Special2; // Add the Special2 flag
            }
            else if (action.InputAction.HasFlag(InputFlag.Special2) && _moveset.SpecialAbilitySlots[0]?.abilityData.ID == action.CurrentAbilityID)
            {
                flag &= ~InputFlag.Special2; // Remove the Special2 flag
                flag |= InputFlag.Special1; // Add the Special1 flag
            }

            //_lastActionWasCharge = flag.HasFlag(InputFlag.Strong);

            ////If the action is a charge attack, perform it.
            //if (_lastActionWasCharge)
            //{
            //    PerformChargeAttack(action);
            //    return;
            //}


            _inputBehaviour.AIFlags = flag;
            return;

            FVector2 direction = action.CurrentAbilityID == -1 ? (FVector2)action.MoveDirection : (FVector2)action.AttackDirection;

            direction.X *= _movementBehaviour.GetAlignmentX();

            //Set movement flags.
            if (direction != FVector2.Zero)
            {
                if(direction == FVector2.Up)
                {
                    _inputBehaviour.AIFlags |= InputFlag.Up;
                }
                else if (direction == FVector2.Down)
                {
                    _inputBehaviour.AIFlags |= InputFlag.Down;
                }
                else if (direction == FVector2.Left)
                {
                    _inputBehaviour.AIFlags |= InputFlag.Left;
                }
                else if (direction == FVector2.Right)
                {
                    _inputBehaviour.AIFlags |= InputFlag.Right;
                }
            }
            //Set shuffle flag.
            else if (action.CurrentAbilityID == -2)
            {
                _inputBehaviour.AIFlags |= InputFlag.Shuffle;
                return;
            }

            //Store the ability so the flag can be set based on its type.
            Ability ability = _moveset.GetAbility(args =>
            {
                Ability possibleAbility = (Ability)args[0];

                return possibleAbility.abilityData.ID == action.CurrentAbilityID;
            });

            if (ability == null)
                return;

            //Set normal ability flag.
            if ((int)ability.abilityData.AbilityType < 4)
            {
                _inputBehaviour.AIFlags |= InputFlag.Weak;
            }
            //Set strong ability flag.
            else if ((int)ability.abilityData.AbilityType < 8)
            {
                _inputBehaviour.AIFlags |= InputFlag.Strong;
            }
            //Set special ability flag.
            else if ((int)ability.abilityData.AbilityType == 8)
            {
                //Set flag based on which slot the ability is in.

                int index = _moveset.GetSpecialAbilityIndex(ability);

                if (index == 0)
                {
                    _inputBehaviour.AIFlags |= InputFlag.Special1;
                }
                else if (index == 1)
                {
                    _inputBehaviour.AIFlags |= InputFlag.Special2;
                }
            }
            //Set burst ability flag.
            else if ((int)ability.abilityData.AbilityType == 10)
            {
                _inputBehaviour.AIFlags |= InputFlag.Burst;
            }

            //Old action code
            //if (action.CurrentAbilityID == -1 && !_movementBehaviour.IsMoving && _movementBehaviour.CanMove && (StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Moving"))
            //{
            //    direction.X *= _movementBehaviour.GetAlignmentX();
            //    _movementBehaviour.Move((FVector2)direction);
            //    return;
            //}
            //else if (action.CurrentAbilityID == -2)
            //{
            //    _moveset.ManualShuffle();
            //    return;
            //}

            //direction = (FVector2)action.AttackDirection;

            //_moveset.UseAbility(action.CurrentAbilityID, new Fixed32(104857), direction);

        }

        public void PerformAction(InputFlag action)
        {
            _inputBehaviour.AIFlags = action;
        }

        private void StartPlayback(float delayOffset = 0)
        {
            ActionNode nextAction = _currentRecording.Recording[_currentActionIndex];

            //if (_lastActionWasCharge && (nextAction.InputAction == InputFlag.Left || 
            //    nextAction.InputAction == InputFlag.Right || 
            //    nextAction.InputAction == InputFlag.Up || 
            //    nextAction.InputAction == InputFlag.Down))
            //{
            //    PerformChargeAttack(nextAction);
            //    _currentActionIndex++;
            //    return;
            //}

            //if (nextAction.TimeDelay <= GridGame.FixedTimeStep)
            //{
            //    PerformAction(_currentRecording.Recording[_currentActionIndex]);

            //    _currentActionIndex++;
            //    return;
            //}

            _playbackRoutine = FixedPointTimer.StartNewTimedAction(() =>
            {
                if (_currentActionIndex >= _currentRecording.Recording.Count)
                {
                    _playbackRoutine.Stop();
                    return;
                }
                PerformAction(_currentRecording.Recording[_currentActionIndex]);

                _currentActionIndex++;

            }, _currentRecording.Recording[_currentActionIndex].TimeDelay - delayOffset);

        }


        /// <summary>
        /// Stops whatever action the AI is doing and prevents playing a new action.
        /// </summary>
        public void PausePlayback()
        {
            _playbackRoutine?.Pause();
            _isPaused = true;
        }

        /// <summary>
        /// Restarts the playback at the last action performed.
        /// </summary>
        public void UnpausePlayback()
        {
            _playbackRoutine?.Resume();
            _isPaused = false;
        }

        private void UpdateSituationNode()
        {
            _currentSituation.CurrentState = _stateMachine.CurrentState;

            //Update grid state
            _currentSituation.AlignmentX = (int)_movementBehaviour.GetAlignmentX();
            _currentSituation.AverageHitBoxOffset = GetAveragePosition();
            _currentSituation.AverageVelocity = (Vector3)GetAverageVelocity();
            _currentSituation.MoveDirection = (Vector2)_movementBehaviour.MoveDirection;
            _currentSituation.IsGrounded = _gridPhysics.IsGrounded;

            //Update current actions
            if (_moveset.AbilityInUse)
            {
                _currentSituation.Energy = _moveset.Energy;
                _currentSituation.CurrentAbilityID = _moveset.LastAbilityInUse.abilityData.ID;
            }
            else
            {
                _currentSituation.CurrentAbilityID = -1;
            }

            //Update health
            _currentSituation.Health = _knockbackBehaviour.Health;
            _currentSituation.BarrierHealth = _ownerBarrier.Health;

            //Update opponent values
            _currentSituation.OwnerToTarget = _opponent.transform.position - _character.transform.position;
            _currentSituation.OpponentState = BlackBoardBehaviour.Instance.GetPlayerState(Opponent);
            _currentSituation.OpponentVelocity = (Vector3)_opponentGridPhysics.Velocity;
            _currentSituation.OpponentEnergy = _opponentMoveset.Energy;
            _currentSituation.OpponentMoveDirection = (Vector2)_opponentMove.MoveDirection;
            _currentSituation.OpponentHealth = _opponentKnocback.Health;
            _currentSituation.OpponentBarrierHealth = _opponentBarrier.Health;
            _currentSituation.PanelPosition = (Vector2)_movementBehaviour.Position;
            _currentSituation.OpponentPanelPosition = (Vector2)_opponentMove.Position;

        }

        /// <summary>
        /// Checks to see if the action is in the ability decks if it isn't movement or shuffling.
        /// </summary>
        /// <param name="ID">The ID of the action. -1 if movement, -2 if reshuffle. Anything else is assumed to be an ability.</param>
        /// <returns>Whether or not the action can be performed.</returns>
        private bool ValidateAction(ActionPlaybackInfo playbackInfo, int actionIndex)
        {
            bool specialsOkay = playbackInfo.CheckCanPerformSpecials(Moveset);

            return specialsOkay;
        }

        /// <summary>
        /// Iterates through the list of recordings to find an action that was performed when the game state was similar.
        /// </summary>
        private void StartNewAction()
        {
            float currentLowest = _actionScoreMax;

            //Iterate through recording list.
            for (int i = 0; i < _playbackInfo.Length; i++)
            {
                ActionPlaybackInfo recording = _playbackInfo[i];

                //Iterate through current recording actions.
                for (int j = 0; j < recording.Recording.Count; j++)
                {
                    float compareVal = recording.Recording[j].Compare(_currentSituation);

                    //If the current action is valid and matches our situation more closely than the last action...
                    if (compareVal + UnityEngine.Random.Range(0, TreeNode.RandomDecisionConstant + _randomDecisionConstant) < currentLowest && ValidateAction(recording, j))
                    {
                        //...update the current action.
                        _currentRecording = recording;
                        _currentActionIndex = j;
                        _currentRecordingIndex = i;
                        currentLowest = compareVal;
                    }

                    if (currentLowest <= _accuracyMinimum)
                        break;
                }
            }

            //Play the the action at the current index after storing  the amount of time it took to act in the previous action.
            //This is to be sure the last actions delay doesn't effect the next.
            float time = _playbackRoutine == null ? 0 : _playbackRoutine.GetTimeLeft();

            _playbackRoutine?.Stop();
            StartPlayback();
        }


        /// <summary>
        /// Returns all items in the playbackInfo array that match the current situation's panel position and opponent panel position.
        /// </summary>
        /// <returns>A list of ActionPlaybackInfo objects that match the current situation.</returns>
        private List<ActionPlaybackInfo> GetMatchingPlaybackInfos(bool needSpecial = false)
        {
            List<ActionPlaybackInfo> matchingInfos = new List<ActionPlaybackInfo>();

            foreach (ActionPlaybackInfo playbackInfo in _playbackInfo)
            {
                ActionNode action = playbackInfo.Recording[0];

                Vector2 panelPosition = action.PanelPosition;
                Vector2 opponentPanelPosition = action.OpponentPanelPosition;

                if (action.AlignmentX != _currentSituation.AlignmentX)
                {
                    int xPosClamped = (int)Math.Clamp(panelPosition.x, 0, GridBehaviour.Instance.Dimensions.x - 1);
                    int yPosClamped = (int)Math.Clamp(panelPosition.y, 0, GridBehaviour.Instance.Dimensions.y - 1);

                    int opponentXPosClamped = (int)Math.Clamp(opponentPanelPosition.x, 0, GridBehaviour.Instance.Dimensions.x - 1);
                    int opponentYPosClamped = (int)Math.Clamp(opponentPanelPosition.y, 0, GridBehaviour.Instance.Dimensions.y - 1);

                    panelPosition = (Vector2)GridBehaviour.Instance.GetMirroredPanelAcrossX(xPosClamped, yPosClamped).Position;
                    opponentPanelPosition = (Vector2)GridBehaviour.Instance.GetMirroredPanelAcrossX(opponentXPosClamped, opponentYPosClamped).Position;
                }

                if (panelPosition == _currentSituation.PanelPosition &&
                    opponentPanelPosition == _currentSituation.OpponentPanelPosition)
                {
                    if (needSpecial && playbackInfo.SpecialAttackNodes.Count == 0)
                        continue;

                    matchingInfos.Add(playbackInfo);
                }
            }

            matchingInfos.Shuffle();

            return matchingInfos;
        }

        private void ResetRecordingActions()
        {
            _currentActionIndex = 0;
            _currentRecordingIndex = 0;
            _currentRecording = null;
            _currentState = AIState.Idle;
            changePlanAction?.Stop();
            _waitingToChangePlans = false;
        }

        private void StartNewRecording()
        {
            UpdateSituationNode();

            float currentLowest = _actionScoreMax;
            _currentActionIndex = 0; // Always the first action
            bool foundRecording = false;
            _waitingToChangePlans = false;
            _currentRecording = null;
            _cantFindRecording = true;

            List<ActionPlaybackInfo> matchingInfos = null;

            if (Moveset.Energy == 5)
            {
                matchingInfos = GetMatchingPlaybackInfos(true);

                if (matchingInfos.Count == 0)
                    matchingInfos = GetMatchingPlaybackInfos();
            }
            else
            {
                matchingInfos = GetMatchingPlaybackInfos();
            }

            if (matchingInfos.Count == 0)
            {
                Debug.LogError("No recordings found that match the current panel positions.");
                return;
            }

            // Iterate through recording list.
            for (int i = 0; i < matchingInfos.Count; i++)
            {
                ActionPlaybackInfo recording = matchingInfos[i];

                // Only compare the first action of the recording.
                if (recording.Recording.Count > 0)
                {
                    
                    // If the current action is valid and matches our situation more closely than the last action...
                    if (ValidateAction(recording, 0) && CheckSituationSimilar(recording))
                    {
                        foundRecording = true; // Found a valid recording
                                               // ...update the current action.
                        _currentRecording = recording;

                        _currentRecordingIndex = i;

                        _cantFindRecording = false;

                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of physics components from all attacks in range
        /// </summary>
        /// <returns></returns>
        private bool CheckIfProjectilesWillHit()
        {
            List<HitColliderBehaviour> attacksInRange = GetAttacksInRange();

            for (int i = 0; i < attacksInRange.Count; i++)
            {
                GridPhysicsBehaviour physics = attacksInRange[i].GetComponentInParent<GridPhysicsBehaviour>();

                if (physics == null) continue;

                FVector3 direction = (physics.FixedTransform.WorldPosition - FixedTransform.WorldPosition).GetNormalized();
                Fixed32 dotProduct = FVector3.Dot(direction, physics.Velocity.GetNormalized());

                //0.8
                if (Fixed32.Abs(dotProduct) >= new Fixed32(52428) || physics.GetGridPosition() == AIMovement.MovementBehaviour.Position || attacksInRange[i].CheckInCollisionRange(AIMovement.MovementBehaviour.Position))
                    return true;
            }

            return false;
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            if (MatchManagerBehaviour.Instance.IsPaused || !MatchManagerBehaviour.Instance.MatchStarted || MatchManagerBehaviour.Instance.PlayerOutOfRing
                || _stateMachine.CompareState("Idle"))
            {
                _currentState = AIState.Idle;
            }

            switch (_currentState)
            {
                case AIState.Idle:

                    if (MatchManagerBehaviour.Instance.IsPaused || !MatchManagerBehaviour.Instance.MatchStarted || MatchManagerBehaviour.Instance.PlayerOutOfRing
                || !_stateMachine.CompareState("Idle", "HardLanding", "Tumbling", "Flinching"))
                    {
                        break;
                    }

                    if (CheckIfProjectilesWillHit() || _stateMachine.CompareState("Tumbling", "Flinching", "HardLanding"))
                    {
                        _currentState = AIState.Defending;
                    }
                    else
                    {
                        _currentState = AIState.Attacking;
                        if (_currentRecording == null)
                        {
                            StartNewRecording();
                        }
                    }
                    break;
                case AIState.Attacking:

                    if (CheckIfProjectilesWillHit() || _stateMachine.CompareState("Tumbling", "Flinching"))
                    {
                        ResetRecordingActions();
                        _currentState = AIState.Defending;
                        return;
                    }
                    break;
                case AIState.Defending:
                    HandleDefense();

                    if (!CheckIfProjectilesWillHit())
                    {
                        _currentState = AIState.Attacking;
                    }
                    break;
            }
        }

        private bool CheckSituationSimilar(ActionPlaybackInfo info = null)
        {
            UpdateSituationNode();

            if (info == null)
                info = _currentRecording;

            Fixed32 distancePercentage = info.Recording[_currentActionIndex].GetDistanceAccuracy(_currentSituation);

            if (distancePercentage < _distanceAccuracyMinimum)
            {
                //Debug.Log("Distance accuracy too low: " + distancePercentage + " for action: " + info.Recording[_currentActionIndex].CurrentAbilityID + " at index: " + _currentActionIndex + " in recording: " + _currentRecordingIndex + ". Starting new recording.)");
                return false;
            }

            Fixed32 directionPercentage = info.Recording[_currentActionIndex].GetDirectionAccuracy(_currentSituation);

            if (directionPercentage < _directionAccuracyMinimum)
            {
                //Debug.Log("Direction accuracy too low: " + directionPercentage + " for action: " + info.Recording[_currentActionIndex].CurrentAbilityID + " at index: " + _currentActionIndex + " in recording: " + _currentRecordingIndex + ". Starting new recording.)");
                return false;
            }

            return true;
        }

        private bool CheckOpponentState()
        {
            if (_currentRecording == null || _currentRecording.Recording.Count == 0)
                return false;

            string recordedOpponentState = _currentRecording.Recording[_currentActionIndex].OpponentState;

            if (recordedOpponentState != "Flinching" && recordedOpponentState != "Tumbling" && recordedOpponentState != "HardLanding")
                return true;

            string currentOpponentState = BlackBoardBehaviour.Instance.GetPlayerState(_opponent);

            return currentOpponentState == "Flinching" || currentOpponentState == "Tumbling" || currentOpponentState == "HardLanding";
        }

        /// <summary>
        /// Calculate a rating for safety for each panel on the AI side. The rating is based on the projectiles on the row and how long it would take the projectile to touch the panel.
        /// After that find the lowest rating and use A star to get a safe path. A star will need to know all the ratings and go to the best one.
        /// </summary>
        private void HandleDefense()
        {
            if (_stateMachine.CompareState("Tumbling", "Flinching") && _knockbackBehaviour.Health >= _ownerBarrier.Health)
            {
                _moveset.UseBasicAbility(AbilityType.BURST);
                return;
            }

            List<HitColliderBehaviour> hitColliders = GetAttacksInRange();

            if (hitColliders.Count == 0)
                return;

            List<PanelBehaviour> panels = GridBehaviour.Instance.GetPanelsForAlignment(_movementBehaviour.Alignment);

            // Variables to track the safest and closest panel
            PanelBehaviour safestPanel = null;
            Fixed32 highestSafetyRating = 0;
            Fixed32 shortestDistance = 0;

            // Get the AI's current position
            FVector2 currentPosition = _movementBehaviour.Position;

            // Clean safety ratings for a fresh search and calculate safety ratings
            foreach (PanelBehaviour panel in panels)
            {
                if (panel == null || !panel.gameObject.activeInHierarchy)
                    continue;

                // Reset the safety rating for this panel
                panel.SafetyRating = 0;

                foreach (HitColliderBehaviour hitCollider in hitColliders)
                {
                    if (hitCollider == null || !hitCollider.gameObject.activeInHierarchy)
                        continue;

                    if (hitCollider.EntityCollider.GetPanelPosition() == panel.Position)
                    {
                        panel.SafetyRating = -1; // Immediate danger
                        break;
                    }

                    // Find how much the velocity direction lines up with the direction of the hit collider and the panel
                    FVector3 direction = (panel.FixedWorldPosition.GetWithoutY() - hitCollider.FixedTransform.WorldPosition.GetWithoutY()).GetNormalized();
                    Fixed32 dot = FVector3.Dot(direction, hitCollider.GridPhysics.Velocity.GetNormalized());

                    // If the dot product is negative, the hit collider is moving away from the panel, so it is safe
                    if (dot < 1)
                    {
                        panel.SafetyRating = 10;
                        continue;
                    }

                    Fixed32 speed = hitCollider.GridPhysics.Velocity.Magnitude;

                    Fixed32 distance = (panel.FixedWorldPosition - hitCollider.FixedTransform.WorldPosition).Magnitude;
                    Fixed32 time = speed == 0 ? 0 : distance / speed;

                    if (time > panel.SafetyRating || panel.SafetyRating == 0)
                    {
                        panel.SafetyRating = time;
                    }
                }

                //Check the danger for the opponent themselves in case their down a melee attack.
                if (BlackBoardBehaviour.Instance.GetPlayerState(_opponent) == "Attacking")
                {
                    if (_opponentMove.Position == panel.Position)
                    {
                        panel.SafetyRating = -1; // Immediate danger
                        break;
                    }

                    // Find how much the velocity direction lines up with the direction of the hit collider and the panel
                    FVector3 direction = (panel.FixedWorldPosition - _opponentMove.FixedTransform.WorldPosition).GetNormalized();
                    Fixed32 dot = FVector3.Dot(direction, _opponentGridPhysics.Velocity.GetNormalized());

                    //Only care if the opponent is coming towards us.
                    if (dot > 0)
                    {
                        Fixed32 speed = _opponentGridPhysics.Velocity.Magnitude;

                        Fixed32 distance = (panel.FixedWorldPosition - _opponentMove.FixedTransform.WorldPosition).Magnitude;
                        Fixed32 time = speed == 0 ? 0 : distance / speed;

                        if (time > panel.SafetyRating || panel.SafetyRating == 0)
                        {
                            panel.SafetyRating = time;
                        }
                    }
                }

                //TODO: Add logic for responding to panels with warnings

                // Skip panels with immediate danger
                if (panel.SafetyRating == -1)
                    continue;

                // Calculate the distance to the panel
                Fixed32 distanceToPanel = (panel.Position - currentPosition).Magnitude;

                // Check if this panel is better (lower safety rating or closer if ratings are equal)
                if (panel.SafetyRating > highestSafetyRating ||
                    (panel.SafetyRating == highestSafetyRating && distanceToPanel < shortestDistance))
                {
                    safestPanel = panel;
                    highestSafetyRating = panel.SafetyRating;
                    shortestDistance = distanceToPanel;
                }
            }

            // Move to the safest panel if one is found
            if (safestPanel != null)
            {
                _aiMovementBehaviour.MoveToLocation(safestPanel.Position, (panel, goal) => 3 - panel.SafetyRating);
            }
        }

        private void HandleActionPlayback()
        {
            if (!_useRecording || _isPaused || _waitingToChangePlans || _currentState != AIState.Attacking /*|| _opponentKnocback.IsInvincible*/ || _currentRecording == null)
            {
                if (_currentRecording != null)
                    ResetRecordingActions();

                if (_currentRecording == null)
                {
                    //Debug.LogError("No recording found for AI: " + Character.name);
                    StartNewRecording();
                }

                return;
            }


            //Old code for updating weights in the editor
            ////If we are in the editor...
            //if (Application.isEditor)
            //{
            //    //...update weights with inspector values
            //    ActionNode.DirectionWeight = _directionWeight;
            //    ActionNode.OpponentVelocityWeight = _opponentVelocityWeight;
            //    ActionNode.DistanceWeight = _distanceWeight;
            //    ActionNode.AvgHitBoxOffsetWeight = _avgHitBoxOffsetWeight;
            //    ActionNode.AvgVelocityWeight = _avgVelocityWeight;
            //    ActionNode.MatchTimeRemainingWeight = _matchTimeRemainingWeight;
            //    ActionNode.OpponentStateWeight = _opponentStateWeight;
            //    ActionNode.OpponentHealthWeight = _opponentHealthWeight;
            //}

            //If we are done with the most recent recording...
            if (_currentActionIndex >= _currentRecording.Recording.Count)
            {
                //...find a new action in the recording list.
                _waitingToChangePlans = true;
                changePlanAction?.Stop();   
                changePlanAction = FixedPointTimer.StartNewConditionAction(StartNewRecording, c => _stateMachine.CurrentState == "Idle");
                return;
            }

            if (_currentRecording == null)
            {
                return;
            }

            //Debug.Log("Current action index: " + _currentActionIndex + " in recording: " + _currentRecordingIndex + ". Current situation: " + _currentSituation.ToString() + " with score: " + _lastScore);

            string opponentState = BlackBoardBehaviour.Instance.GetPlayerState(_opponent);

            ////If the action node's situation is too different from the current or if the action isn't possible...
            if ((!CheckOpponentState()) && _currentActionIndex >= _currentRecording.Recording.Count / 2)
            {
                //...find a new action in the recording list.

                //This is on a delay to ensure inputs aren't eaten due to the AI not being in the proper state. So we wait for Idle before getting a new recording.
                _waitingToChangePlans = true;
                changePlanAction = FixedPointTimer.StartNewConditionAction(StartNewRecording, c => _stateMachine.CurrentState == "Idle");
                return;
            }

            //If the AI is current performing an action return.
            //if (_playbackRoutine != null && _playbackRoutine.IsActive)
            //    return;

            //If the AI isn't performing an action play the next action in the recording list.
            //StartPlayback();

            if (_currentActionIndex >= _currentRecording.Recording.Count)
                _currentActionIndex = 0;

            PerformAction(_currentRecording.Recording[_currentActionIndex]);

            _currentActionIndex++;
        }

        public override void Serialize(BinaryWriter bw)
        {
            
        }

        public override void Deserialize(BinaryReader br)
        {
        }

    }
}
