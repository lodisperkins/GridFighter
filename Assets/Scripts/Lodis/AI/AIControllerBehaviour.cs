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
    public class AIControllerBehaviour : MonoBehaviour, IControllable
    {
        [SerializeField]
        private GameObject _character;
        private Gameplay.MovesetBehaviour _moveset;
        [Tooltip("Sets the value that amplifies the power of strong attacks when doing them randomly.")]
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
        private DefenseNode _lastDefenseDecision;

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

        private MovesetBehaviour _opponentMoveset;
        private GridMovementBehaviour _movementBehaviour;
        private List<ActionNode>[] _recordings;
        private List<ActionNode> _currentRecording;
        private int _currentActionIndex;
        private int _currentRecordingIndex;
        private ActionNode _currentSituation;

        private string _recordingName = "AI";
        [SerializeField]
        [Tooltip("Whether or not to use recording data for decision making.")]
        private bool _useRecording;
        private GridPhysicsBehaviour _opponentGridPhysics;
        private bool _isPaused;
        private TimedAction _playbackRoutine;
        [SerializeField]
        [Tooltip("Recorded actions that have a score above this value when compared are considered cannot be used.")]
        private float _actionScoreMax;
        [SerializeField]
        [Tooltip("The last score found after comparing the current action situation to the current game state.")]
        private float _lastScore;
        [Header("Weights")]
        [SerializeField]
        [Tooltip("How important the direction the enemy is relative to the AI.")]
        private float _directionWeight = 0.5f;
        [SerializeField]
        [Tooltip("How important the velocity the enemy is.")]
        private float _opponentVelocityWeight = 0.8f;
        [SerializeField]
        [Tooltip("How important the distance between the enemy and the AI is")]
        private float _distanceWeight = 0.7f;
        [SerializeField]
        [Tooltip("How important the direction and distance of enemy hit boxes are relative to the AI")]
        private float _avgHitBoxOffsetWeight = 1.5f;
        [SerializeField]
        [Tooltip("How important the velocity of enemy hit boxes are relative to the AI")]
        private float _avgVelocityWeight = 1.5f;
        [SerializeField]
        [Tooltip("How important the time remaining in the match is")]
        private float _matchTimeRemainingWeight = 1;
        [SerializeField]
        [Tooltip("How important the opponent's current state is")]
        private float _opponentStateWeight = 1;
        [SerializeField]
        [Tooltip("How important the opponent's current health is")]
        private float _opponentHealthWeight = 1;

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
        public DefenseNode LastDefenseDecision { get => _lastDefenseDecision; set => _lastDefenseDecision = value; }

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
                _recordings = AIRecorderBehaviour.Load(_recordingName, _moveset);
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

        private void Awake()
        {
            _executor = GetComponent<BehaviorExecutor>();
            _aiMovementBehaviour = GetComponent<AIDummyMovementBehaviour>();
        }

        private void Start()
        {
            Defense = Character.GetComponent<CharacterDefenseBehaviour>();
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

            _opponentBarrier = _movementBehaviour.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;


            if (_useRecording)
            {
                _currentRecording = _recordings[0];
                _knockbackBehaviour.AddOnTakeDamageAction(() =>
                {
                    UpdateSituationNode();
                    StartNewAction();
                });
            }
            else
                MatchManagerBehaviour.Instance.AddOnMatchOverAction(AddMatchReward);


            //MatchManagerBehaviour.Instance.AddOnMatchOverAction(() =>
            //{
            //    if (MatchManagerBehaviour.Instance.LastMatchResult != MatchResult.DRAW)
            //        MatchManagerBehaviour.Instance.Restart();
            //});
        }

        private void OnEnable()
        {
            if (_useRecording)
                return;

            if (_executor)
                _executor.enabled = true;

            if (_aiMovementBehaviour)
                _aiMovementBehaviour.enabled = true;
        }

        private void OnDisable()
        {
            _executor.enabled = false;
            _aiMovementBehaviour.enabled = false;
            RoutineBehaviour.Instance.StopAction(_playbackRoutine);
        }

        private void OnDestroy()
        {
            if (!Application.isEditor) return;

            _attackDecisions?.Save(Character.name);
            _defenseDecisions?.Save(Character.name);
        }

        private void AddMatchReward()
        {
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

        private Vector3 GetAverageVelocity()
        {
            _attacksInRange = BlackBoardBehaviour.Instance.GetActiveColliders(_opponentMove.Alignment);
            Vector3 averageVelocity = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            _attacksInRange.RemoveAll(physics =>
            {
                return (object)physics == null || !physics.gameObject.activeInHierarchy;
            });

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            for (int i = 0; i < _attacksInRange.Count; i++)
                if (_attacksInRange[i].RB)
                    averageVelocity += _attacksInRange[i].RB.velocity;

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

        private void PerformAction(ActionNode action)
        {
            Vector2 direction = action.MoveDirection;
            if (action.CurrentAbilityID == -1 && !_movementBehaviour.IsMoving && _movementBehaviour.CanMove && (StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Moving"))
            {
                direction.x *= _movementBehaviour.GetAlignmentX();
                _movementBehaviour.Move(direction);
                return;
            }
            else if (action.CurrentAbilityID == -2)
            {
                _moveset.ManualShuffle();
                return;
            }

            direction = action.AttackDirection;

            _moveset.UseAbility(action.CurrentAbilityID, 1.6f, direction);
        }

        private void StartPlayback(float delayOffset = 0)
        {
            _playbackRoutine = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                PerformAction(_currentRecording[_currentActionIndex]);

            }, TimedActionCountType.SCALEDTIME, _currentRecording[_currentActionIndex].TimeDelay - delayOffset);

        }


        /// <summary>
        /// Stops whatever action the AI is doing and prevents playing a new action.
        /// </summary>
        public void PausePlayback()
        {
            RoutineBehaviour.Instance.StopAction(_playbackRoutine);
            _isPaused = true;
        }

        /// <summary>
        /// Restarts the playback at the last action performed.
        /// </summary>
        public void UnpausePlayback()
        {
            _isPaused = false;
        }

        private void UpdateSituationNode()
        {
            _currentSituation = new ActionNode(null, null);

            _currentSituation.CurrentState = _stateMachine.CurrentState;

            //Update grid state
            _currentSituation.AlignmentX = (int)_movementBehaviour.GetAlignmentX();
            _currentSituation.AverageHitBoxOffset = GetAveragePosition();
            _currentSituation.AverageVelocity = GetAverageVelocity();
            _currentSituation.MoveDirection = _movementBehaviour.MoveDirection;
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
            _currentSituation.OpponentVelocity = _opponentGridPhysics.LastVelocity;
            _currentSituation.OpponentEnergy = _opponentMoveset.Energy;
            _currentSituation.OpponentMoveDirection = _opponentMove.MoveDirection;
            _currentSituation.OpponentHealth = _opponentKnocback.Health;
            _currentSituation.OpponentBarrierHealth = _opponentBarrier.Health;
            _currentSituation.PanelPosition = _movementBehaviour.Position;

        }

        /// <summary>
        /// Checks to see if the action is in the ability decks if it isn't movement or shuffling.
        /// </summary>
        /// <param name="ID">The ID of the action. -1 if movement, -2 if reshuffle. Anything else is assumed to be an ability.</param>
        /// <returns>Whether or not the action can be performed.</returns>
        private bool ValidateAction(int ID)
        {
            if (ID == -1 || ID == -2)
                return true;

            return _moveset.SpecialAbilitySlots.Contains<Ability>(ability => ability.abilityData.ID == ID) || _moveset.NormalDeckContains(ID);
        }

        /// <summary>
        /// Iterates through the list of recordings to find an action that was performed when the game state was similar.
        /// </summary>
        private void StartNewAction()
        {
            bool actionFound = false;
            float currentLowest = _actionScoreMax;

            //Iterate through recording list.
            for (int i = 0; i < _recordings.Length; i++)
            {
                List<ActionNode> recording = _recordings[i];

                //Iterate through current recording actions.
                for (int j = 0; j < recording.Count; j++)
                {
                    float compareVal = recording[j].Compare(_currentSituation);

                    //If the current action is valid and matches our situation more closely than the last action...
                    if (compareVal + UnityEngine.Random.Range(0, TreeNode.RandomDecisionConstant + 1) < currentLowest && ValidateAction(recording[j].CurrentAbilityID))
                    {
                        //...update the current action.
                        _currentRecording = recording;
                        _currentActionIndex = j;
                        _currentRecordingIndex = i;
                        currentLowest = compareVal;
                        actionFound = true;
                    }
                }
            }

            if (!actionFound)
                Debug.Log("Couldn't find recording that matched situation.");

            //Play the the action at the current index after storing  the amount of time it took to act in the previous action.
            //This is to be sure the last actions delay doesn't effect the next.
            float time = _playbackRoutine == null ? 0 : _playbackRoutine.TimeLeft;

            RoutineBehaviour.Instance.StopAction(_playbackRoutine);
            StartPlayback(time);
        }

        public void Update()
        {
            if (_bufferedAction?.HasAction() == true)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            if (!_useRecording || _isPaused)
                return;

            //If we are in the editor...
            if (Application.isEditor)
            {
                //...update weights with inspector values
                ActionNode.DirectionWeight = _directionWeight;
                ActionNode.OpponentVelocityWeight = _opponentVelocityWeight;
                ActionNode.DistanceWeight = _distanceWeight;
                ActionNode.AvgHitBoxOffsetWeight = _avgHitBoxOffsetWeight;
                ActionNode.AvgVelocityWeight = _avgVelocityWeight;
                ActionNode.MatchTimeRemainingWeight = _matchTimeRemainingWeight;
                ActionNode.OpponentStateWeight = _opponentStateWeight;
                ActionNode.OpponentHealthWeight = _opponentHealthWeight;
            }

            //Update the current situation node to reflect the current game state.
            UpdateSituationNode();

            //Compare the situation recorded for this action to the current situation.
            _lastScore = _currentRecording[_currentActionIndex].Compare(_currentSituation);

            //Debug.Log(score);

            //If the action node's situation is too different from the current or if the action isn't possible...
            if (_lastScore >= _actionScoreMax || !ValidateAction(_currentRecording[_currentActionIndex].CurrentAbilityID))
            {
                //...find a new action in the recording list.
                StartNewAction();
                return;
            }

            //If the AI is current performing an action return.
            if (_playbackRoutine != null && _playbackRoutine.GetEnabled())
                return;

            //If the AI isn't performing an action play the next action in the recording list.
            StartPlayback();

            _currentActionIndex++;

            if (_currentActionIndex >= _currentRecording.Count)
                _currentActionIndex = 0;
        }
    }
}
