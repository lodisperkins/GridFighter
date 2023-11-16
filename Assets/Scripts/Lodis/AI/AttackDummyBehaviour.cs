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

namespace Lodis.AI
{
    public class AttackDummyBehaviour : MonoBehaviour, IControllable
    {
        [SerializeField]
        private GameObject _character;
        private Gameplay.MovesetBehaviour _moveset;
        [Tooltip("Pick the attack this test dummy should perform")]
        [SerializeField]
        private Gameplay.AbilityType _attackType;
        [Tooltip("Sets the amount of time the dummy will wait before attacking again")]
        [SerializeField]
        private float _attackDelay;
        private float _timeOfLastAttack;
        [Tooltip("Sets the value that amplifies the power of strong attacks")]
        [SerializeField]
        private float _attackStrength;
        [SerializeField]
        private float _maxRange;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private Vector2 _attackDirection;
        private StateMachine _stateMachine;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private int _lastSlot;
        [SerializeField]
        private bool _enableRandomBehaviour;
        private bool _chargingAttack;
        private List<HitColliderBehaviour> _attacksInRange = new List<HitColliderBehaviour>();
        [SerializeField]
        private Collider _senseCollider;
        [SerializeField]
        private bool _canAttack = true;
        private BehaviorExecutor _executor;
        private Coroutine _moveRoutine;
        private PanelBehaviour _moveTarget;
        private bool _needPath;
        private List<PanelBehaviour> _currentPath;
        private int _currentPathIndex;

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

        [SerializeField]
        private int _maxDecisionCount;
        [Tooltip("The amount of time the dummy has to be in knock back to consider using a burst.")]
        [SerializeField]
        private float _timeNeededToBurst;
        private GridPhysicsBehaviour _gridPhysics;
        private IntVariable _playerID;
        private BufferedInput _bufferedAction;
        public DefenseNode LastDefenseDecision;

        public Vector2 MovePosition;
        public bool EnableBehaviourTree;
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
        private bool _abilityBuffered;

        private Vector3 _opponentVelocity;
        private bool _hasUpdatedGameState;
        private Vector3 _opponentDisplacement;
        private float _opponentHealth;
        private List<HitColliderBehaviour> _lastAttacksInRange;
        private float _targetY;
        private TimedAction _saveStateTimer;
        private PredictionDecisionTree _predictionDecisions;
        private PredictionNode _currentPrediction;
        private PredictionNode _lastPrediction;
        private TimedAction _checkStateTimer;
        private MovesetBehaviour _opponentMoveset;
        private GridMovementBehaviour _movementBehaviour;
        private RecordingPlaybackBehaviour _playbackBehaviour;
        private float _decisionDelay = 0.5f;
        private List<ActionNode>[] _recordings;
        private List<ActionNode> _currentRecording;
        private int _currentActionIndex;
        private ActionNode _currentSituation;

        [SerializeField]
        private string _recordingName;
        [SerializeField]
        private bool _useRecording;
        private DecisionTree _actionTree;

        public StateMachine StateMachine { get => _stateMachine; }
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

        private GridPhysicsBehaviour _opponentGridPhysics;
        private bool _canMakeDecision = true;
        private TimedAction _decisionTimer;
        private bool _isPaused;
        private TimedAction _playbackRoutine;

        public bool CanAttack { get => _canAttack; private set => _canAttack = value; }

        public Vector2 AttackDirection
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
        public PredictionNode CurrentPrediction { get => _currentPrediction; private set => _currentPrediction = value; }

        public float TargetY
        {
            get
            {
                if (CurrentPrediction == null)
                    return OpponentMove.Position.y;

                return CurrentPrediction.TargetY;
            }
        }

        public void LoadDecisions()
        {
            
            if (_useRecording)
            {
                _actionTree = new DecisionTree(0.5f);
                _actionTree.SaveLoadPath = Application.persistentDataPath + "/RecordedDecisionData";
                _actionTree.Load("_" + _recordingName);
                _executor.enabled = false;
                EnableBehaviourTree = false;
                _recordings = AIRecorderBehaviour.Load(_recordingName);
            }
            if (!EnableBehaviourTree)
                return;

            _attackDecisions = new AttackDecisionTree();
            _attackDecisions.MaxDecisionsCount = _maxDecisionCount;
            _attackDecisions.Load(Character.name);
            _defenseDecisions = new DefenseDecisionTree();
            _defenseDecisions.MaxDecisionsCount = _maxDecisionCount;
            _defenseDecisions.Load(Character.name);
            //_predictionDecisions.Load(Character.name);
            if (Application.isEditor) return;

            MatchManagerBehaviour.Instance.AddOnApplicationQuitAction(() => _attackDecisions?.Save(Character.name));
            MatchManagerBehaviour.Instance.AddOnApplicationQuitAction(() => _defenseDecisions?.Save(Character.name));
        }

        private void Awake()
        {
            _executor = GetComponent<BehaviorExecutor>();
            _aiMovementBehaviour = GetComponent<AIDummyMovementBehaviour>();
            _predictionDecisions = new PredictionDecisionTree();
            _playbackBehaviour = GetComponent<RecordingPlaybackBehaviour>();
        }

        private void Start()
        {
            Defense = Character.GetComponent<CharacterDefenseBehaviour>();
            Moveset = Character.GetComponent<Gameplay.MovesetBehaviour>();
            _stateMachine = Character.GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            Knockback = Character.GetComponent<Movement.KnockbackBehaviour>();
            _gridPhysics = Character.GetComponent<GridPhysicsBehaviour>();
            _movementBehaviour = Character.GetComponent<GridMovementBehaviour>();

            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(PlayerID);
            OpponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            OpponentKnockback = _opponent.GetComponent<KnockbackBehaviour>();
            OpponentDefense = _opponent.GetComponent<CharacterDefenseBehaviour>();
            _opponentGridPhysics = _opponent.GetComponent<GridPhysicsBehaviour>();
            _opponentMoveset = _opponent.GetComponent<MovesetBehaviour>();

            _senseCollider.transform.SetParent(Character.transform);
            _senseCollider.transform.localPosition = Vector3.zero;

            _knockbackBehaviour.AddOnTakeDamageAction(() => CreateNewPredictNode(false));
            _opponentKnocback.AddOnTakeDamageAction(() => CreateNewPredictNode(true));

            //MatchManagerBehaviour.Instance.AddOnMatchOverAction(AddMatchReward);
            //MatchManagerBehaviour.Instance.AddOnMatchOverAction(() =>
            //{
            //    if (MatchManagerBehaviour.Instance.LastMatchResult != MatchResult.DRAW)
            //        MatchManagerBehaviour.Instance.Restart();
            //});
            _opponentBarrier = _movementBehaviour.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;

            _currentRecording = _recordings[0];
        }

        private void OnEnable()
        {
            if (_useRecording)
                return;

            if (_executor && EnableBehaviourTree)
                _executor.enabled = true;

            if (_aiMovementBehaviour)
                _aiMovementBehaviour.enabled = true;
        }

        private void OnDisable()
        {
            _executor.enabled = false;
            _aiMovementBehaviour.enabled = false;
        }

        private void OnDestroy()
        {
            if (!Application.isEditor) return;

            _attackDecisions?.Save(Character.name);
            _defenseDecisions?.Save(Character.name);
            //_predictionDecisions?.Save(Character.name);
        }

        private void AddMatchReward()
        {
            GridAlignment alignment = _aiMovementBehaviour.MovementBehaviour.Alignment;

            if (MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.P1WINS && alignment == GridAlignment.LEFT 
                || MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.P2WINS && alignment == GridAlignment.RIGHT)
            {
                _attackDecisions.AddRewardToDecisions(_winReward);
                _defenseDecisions.AddRewardToDecisions(_winReward);
                _predictionDecisions.AddRewardToDecisions(_winReward);
            }
            else if (MatchManagerBehaviour.Instance.LastMatchResult != MatchResult.DRAW)
            {
                _attackDecisions.AddRewardToDecisions(_losePenalty);
                _defenseDecisions.AddRewardToDecisions(_losePenalty);
                _predictionDecisions.AddRewardToDecisions(_losePenalty);
            }
        }

        public List<HitColliderBehaviour> GetAttacksInRange()
        {
            if (_attacksInRange.Count > 0)
                _attacksInRange.RemoveAll(hitCollider =>
                { 
                    if ((object)hitCollider != null)
                        return hitCollider == null || !hitCollider.gameObject.activeInHierarchy;

                    return true;
                });

            return _attacksInRange;
        }

        public IEnumerator ChargeRoutine(float chargeTime, AbilityType type)
        {
            _chargingAttack = true;
            yield return new WaitForSeconds(chargeTime);

            if ((StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Attacking"))
            {
                Moveset.UseBasicAbility(type, new object[] { _attackStrength, _attackDirection });
            }
                _chargingAttack = false;
        }

        private void UseAbility(Ability ability, float attackStrength, Vector2 attackDirection)
        {
            //Uses the ability based on its type
            if (Moveset.GetAbilityNamesInCurrentSlots()[0] == ability.abilityData.name)
                Moveset.UseSpecialAbility(0, attackStrength, attackDirection);
            else if (Moveset.GetAbilityNamesInCurrentSlots()[1] == ability.abilityData.name)
                Moveset.UseSpecialAbility(1, attackStrength, attackDirection);
            else if (ability.abilityData.AbilityType != AbilityType.SPECIAL)
                Moveset.UseBasicAbility(ability.abilityData.abilityName, attackStrength, attackDirection);
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
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => UseAbility(ability, attackStrength, attackDirection), condition =>
            {
                _abilityBuffered = false;
                return _moveset.GetCanUseAbility() && !FXManagerBehaviour.Instance.SuperMoveEffectActive;
            }, 0.2f);
            _abilityBuffered = true;
        }


        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferMovement(Vector2 moveDirection)
        {
            moveDirection.x *= Mathf.Round(transform.forward.x);

            GridMovementBehaviour movement = _aiMovementBehaviour.MovementBehaviour;
                
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => movement.MoveToPanel(movement.Position + moveDirection), condition =>
            {
                return !movement.IsMoving && movement.CanMove;
            }, 0.2f);
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferAction(int id, float attackStrength, Vector2 attackDirection)
        {
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => _moveset.UseAbility(id, attackStrength, attackDirection), condition =>
            {
                _abilityBuffered = false;
                return _moveset.GetCanUseAbility() && !FXManagerBehaviour.Instance.SuperMoveEffectActive;
            }, 0.2f);
            _abilityBuffered = true;
        }

        private void UpdateGameState(params object[] args)
        {
            _hasUpdatedGameState = true;
            _opponentDisplacement = _opponent.transform.position - Character.transform.position;
            _opponentHealth = _opponentKnocback.Health;
            _opponentVelocity = _opponentKnocback.Physics.LastVelocity;
            _lastAttacksInRange = GetAttacksInRange();
            _targetY = _opponentMove.Position.y;
        }

        private void CreateNewPredictNode(bool isAttackNode)
        {
            if (!_hasUpdatedGameState)
                return;

            TreeNode node = null;

            if (isAttackNode)
            {
                if (_opponentKnocback.LastCollider != null && _opponentKnocback.LastCollider.CompareTag("Structure"))
                    return;

                node = new AttackNode(_opponent.transform.position - Character.transform.position, _opponentKnocback.Health, 0, 0, "", 0, _opponentKnocback.Physics.LastVelocity, null, null);
            }
            else
            {
                if (_knockbackBehaviour.LastCollider != null && _knockbackBehaviour.LastCollider.CompareTag("Structure"))
                    return;

                List<HitColliderBehaviour> defenseAttacks = new List<HitColliderBehaviour>(_attacksInRange);
                node = new DefenseNode(defenseAttacks, null, null);
            }

            List<HitColliderBehaviour> attacks = new List<HitColliderBehaviour>(_lastAttacksInRange);
            PredictionNode predictNode = new PredictionNode(null, null, _opponentVelocity, _opponentDisplacement, _opponentHealth, attacks, node);
            predictNode.TargetY = _targetY;
            _predictionDecisions.AddDecision(predictNode);
        }

        private void CheckState()
        {
            PredictionNode predictNode = new PredictionNode(null, null, _opponentKnocback.Physics.LastVelocity, _opponent.transform.position - Character.transform.position, _opponentKnocback.Health, _attacksInRange, null);
            if (_lastPrediction.Compare(predictNode) < 0.95f)
            {
                _lastPrediction.Wins--;
            }
            else
                _lastPrediction.Wins++;

            _currentPrediction = (PredictionNode)_predictionDecisions.GetDecision(predictNode);
        }

        private Vector3 GetAverageVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                if (_attacksInRange[i].RB)
                    averageVelocity += _attacksInRange[i].RB.velocity;

            return averageVelocity /= _attacksInRange.Count;
        }

        private Vector3 GetAveragePosition()
        {
            Vector3 averagePosition = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            _attacksInRange.RemoveAll(physics =>
            {
                if ((object)physics != null)
                    return physics == null;

                return true;
            });

            for (int i = 0; i < _attacksInRange.Count; i++)
                averagePosition += _attacksInRange[i].gameObject.transform.position;

            return averagePosition /= _attacksInRange.Count;
        }

        private void PerformAction(ActionNode action)
        {
            if (action.CurrentAbilityID == -1)
            {
                Vector2 direction = action.MoveDirection;
                direction.x *= _movementBehaviour.GetAlignmentX();
                _movementBehaviour.Move(direction);
                return;
            }

            _moveset.UseAbility(action.CurrentAbilityID, 1.6f, action.AttackDirection);
        }

        private void StartPlayback(int index)
        {
            _playbackRoutine = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                PerformAction(_currentRecording[index]);

            }, TimedActionCountType.SCALEDTIME, _currentRecording[_currentActionIndex].TimeDelay);

        }

        public void PausePlayback()
        {
            RoutineBehaviour.Instance.StopAction(_playbackRoutine);
            _isPaused = true;
        }

        public void UnpausePlayback()
        {
            _isPaused = false;
        }

        private void UpdateSituationNode()
        {
            _currentSituation = new ActionNode(null, null);

            _currentSituation.CurrentState = _stateMachine.CurrentState;

            _currentSituation.AlignmentX = (int)_movementBehaviour.GetAlignmentX();
            _currentSituation.AveragePosition = GetAveragePosition();
            _currentSituation.AverageVelocity = GetAverageVelocity();
            _currentSituation.MoveDirection = _movementBehaviour.MoveDirection;
            _currentSituation.IsGrounded = _gridPhysics.IsGrounded;

            if (_moveset.AbilityInUse)
            {
                _currentSituation.Energy = _moveset.Energy;
                _currentSituation.CurrentAbilityID = _moveset.LastAbilityInUse.abilityData.ID;
            }
            else
            {
                _currentSituation.CurrentAbilityID = -1;
            }

            _currentSituation.IsAttacking = _moveset.AbilityInUse;

            _currentSituation.Health = _knockbackBehaviour.Health;
            _currentSituation.BarrierHealth = _ownerBarrier.Health;

            _currentSituation.OwnerToTarget = _opponent.transform.position - _character.transform.position;

            _currentSituation.OpponentVelocity = _opponentGridPhysics.LastVelocity;
            _currentSituation.OpponentEnergy = _opponentMoveset.Energy;
            _currentSituation.OpponentMoveDirection = _opponentMove.MoveDirection;
            _currentSituation.OpponentHealth = _opponentKnocback.Health;
            _currentSituation.OpponentBarrierHealth = _opponentBarrier.Health;

        }

        private void StartNewAction()
        {
            foreach (List<ActionNode> recording in _recordings)
            {
                for (int i = 0; i < recording.Count; i++)
                {
                    if (recording[i].Compare(_currentSituation) >= 0.7f)
                    {
                        _currentRecording = recording;
                        _currentActionIndex = i;
                        RoutineBehaviour.Instance.StopAction(_playbackRoutine);
                        StartPlayback(_currentActionIndex);
                        return;
                    }
                }
            }

        }

        public void Update()
        {

            if (_bufferedAction?.HasAction() == true)
            {
                _bufferedAction.UseAction();
            }
            else
                _abilityBuffered = false;

            if (_useRecording)
            {

                if (_isPaused)
                    return;

               


                if (_playbackRoutine == null || !_playbackRoutine.GetEnabled())
                {
                    UpdateSituationNode();

                    float score = _currentRecording[_currentActionIndex].Compare(_currentSituation);

                    Debug.Log(score);

                    if (score < 0.6f)
                        StartNewAction();
                    else
                        StartPlayback(_currentActionIndex);

                    _currentActionIndex++;

                    if (_currentActionIndex >= _currentRecording.Count)
                        _currentActionIndex = 0;
                }
                //else
                //{
                //    _executor.enabled = true;
                //    _playbackBehaviour.PausePlayback();
                //}

            }

            _executor.blackboard.boolParams[1] = GridPhysics.IsGrounded;

            //if ( (_checkStateTimer == null || _checkStateTimer.GetEnabled() == false))
            //{
            //    _lastPrediction = _currentPrediction;
            //    _checkStateTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => CheckState(), TimedActionCountType.SCALEDTIME, _saveStateDelay);
            //    return;
            //}

            //if (_saveStateTimer == null || _saveStateTimer.GetEnabled() == false)
            //    _saveStateTimer = RoutineBehaviour.Instance.StartNewTimedAction(UpdateGameState, TimedActionCountType.SCALEDTIME, _saveStateDelay);

            if (_executor.enabled || _useRecording) return;

            //Only attack if the dummy is grounded and delay timer is up
            if ((StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Attacking") && Time.time - _timeOfLastAttack >= _attackDelay && !_knockbackBehaviour.LandingScript.RecoveringFromFall && !_chargingAttack)
            {
                //Clamps z direction in case its abs value becomes larger than one at runtime
                _attackDirection.Normalize();

                if (_enableRandomBehaviour)
                {
                    _attackType = (Gameplay.AbilityType)UnityEngine.Random.Range(0, 9);

                    _attackDirection = new Vector2(UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2));
                    _attackStrength = 1.09f;

                    if (((int)_attackType) > 3 && ((int)_attackType) < 8)
                    {
                        StartCoroutine(ChargeRoutine((_attackStrength - 1) / 0.1f, _attackType));
                        return;
                    }
                }

                if (StateMachine.CurrentState == "Stunned")
                    return;

                //Attack based on the ability type selected
                if (_attackType == Gameplay.AbilityType.SPECIAL)
                {
                    if (_lastSlot == 0)
                        _lastSlot = 1;
                    else
                        _lastSlot = 0;

                    Moveset.UseSpecialAbility(_lastSlot, new object[] { _attackStrength, _attackDirection });
                }
                else
                    Moveset.UseBasicAbility(_attackType, new object[]{_attackStrength, _attackDirection});

                _timeOfLastAttack = Time.time;
            }

            
        }
    }
}
