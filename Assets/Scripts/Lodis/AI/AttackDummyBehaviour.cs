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
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

namespace Lodis.AI
{
    public class AttackDummyBehaviour : Agent, IControllable
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
        private bool _abilityBuffered;

        private Vector3 _opponentVelocity;
        private bool _hasUpdatedGameState;
        private Vector3 _opponentDisplacement;
        private float _opponentHealth;
        private List<HitColliderBehaviour> _lastAttacksInRange;
        private TimedAction _saveStateTimer;
        private DecisionTree _predictionTree;
        private PredictionNode _currentPrediction;
        private PredictionNode _lastPrediction;
        private TimedAction _checkStateTimer;
        private GridMovementBehaviour _movementBehaviour;
        private MovesetBehaviour _opponentMoveset;
        private bool _initialized;
        private bool _shouldAttack;

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
        public PredictionNode CurrentPrediction { get => _currentPrediction; private set => _currentPrediction = value; }
        public bool ShouldAttack { get => _shouldAttack; private set => _shouldAttack = value; }

        public void LoadDecisions()
        {
            if (!EnableBehaviourTree)
                return;

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
            Moveset = Character.GetComponent<Gameplay.MovesetBehaviour>();
            _stateMachine = Character.GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            Knockback = Character.GetComponent<Movement.KnockbackBehaviour>();
            _gridPhysics = Character.GetComponent<GridPhysicsBehaviour>();

            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(PlayerID);
            OpponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            OpponentKnockback = _opponent.GetComponent<KnockbackBehaviour>();
            OpponentDefense = _opponent.GetComponent<CharacterDefenseBehaviour>();

            //_senseCollider.transform.SetParent(Character.transform);
            //_senseCollider.transform.localPosition = Vector3.zero;
            
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_executor && EnableBehaviourTree)
                _executor.enabled = true;

            if (_movementBehaviour)
                _aiMovementBehaviour.enabled = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _executor.enabled = false;
            _aiMovementBehaviour.enabled = false;
        }

        private void OnDestroy()
        {
            if (!Application.isEditor) return;

            _attackDecisions?.Save(Character.name);
            _defenseDecisions?.Save(Character.name);
        }

        private void Init()
        {
            _movementBehaviour = Character.GetComponent<GridMovementBehaviour>();

            Defense = Character.GetComponent<CharacterDefenseBehaviour>();
            Moveset = Character.GetComponent<Gameplay.MovesetBehaviour>();
            _stateMachine = Character.GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            Knockback = Character.GetComponent<Movement.KnockbackBehaviour>();
            _gridPhysics = Character.GetComponent<GridPhysicsBehaviour>();

            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(PlayerID);
            OpponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            OpponentKnockback = _opponent.GetComponent<KnockbackBehaviour>();
            OpponentDefense = _opponent.GetComponent<CharacterDefenseBehaviour>();


            OpponentKnockback.AddOnTakeDamageAction(() => AddReward(0.1f));
            OpponentKnockback.AddOnKnockBackAction(() => AddReward(0.2f));
            RingBarrierBehaviour opponentBarrier = _movementBehaviour.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;

            RingBarrierBehaviour aiBarrier = OpponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;

            opponentBarrier.AddOnTakeDamageAction(() => AddReward(0.5f));
            aiBarrier.AddOnTakeDamageAction(() => AddReward(-0.5f));

            _knockbackBehaviour.AddOnTakeDamageAction(() => AddReward(-0.1f));
            _knockbackBehaviour.AddOnKnockBackAction(() => AddReward(-0.2f));

            _knockbackBehaviour.AddOnDeathAction(() => AddReward(-1));
            OpponentKnockback.AddOnDeathAction(() => AddReward(1));

            _opponentMoveset = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Character).GetComponent<MovesetBehaviour>();

            MatchManagerBehaviour.Instance.AddOnMatchOverAction(() =>
            {
                EndEpisode();
            });
            _initialized = true;

            GetComponent<BehaviorParameters>().TeamId = _movementBehaviour.Alignment == GridAlignment.LEFT ? 0 : 1;
        }
        public override void CollectObservations(VectorSensor sensor)
        {
            if (Character == null)
                return;

            if (!_initialized)
                Init();

            //Ai sensor items

            //Movement and position observations
            //Space size: 14
            sensor.AddObservation(_character.transform.position);
            sensor.AddObservation(_movementBehaviour.Position);
            sensor.AddObservation(_movementBehaviour.IsMoving);
            sensor.AddObservation(_movementBehaviour.CanMove);
            sensor.AddObservation(_movementBehaviour.CanCancelMovement);
            sensor.AddObservation(_movementBehaviour.Speed);
            sensor.AddObservation(_movementBehaviour.MoveDirection);
            sensor.AddObservation((int)_movementBehaviour.Alignment);



            //Ability observations
            //Space size: 7
            sensor.AddObservation(_moveset.AbilityInUse);
            sensor.AddObservation(_moveset.Energy);
            sensor.AddObservation(_moveset.CanBurst);
            sensor.AddObservation(_moveset.GetCanUseAbility());

            Ability special1 = _moveset.GetAbilityInCurrentSlot(0);
            Ability special2 = _moveset.GetAbilityInCurrentSlot(1);

            if (special1 != null)
                sensor.AddObservation(special1.abilityData.ID);

            if (special2 != null)
                sensor.AddObservation(special2.abilityData.ID);

            int nextAbilityID = _moveset.NextAbilitySlot == null ? 0 : _moveset.NextAbilitySlot.abilityData.ID;
            sensor.AddObservation(nextAbilityID);

            //Arena observations
            //Space size: 79
            sensor.AddObservation(_knockbackBehaviour.Health);
            sensor.AddObservation(BlackBoardBehaviour.Instance.GetBarrierHealthByAlignment(_movementBehaviour.Alignment));

            sensor.AddObservation(_opponentKnocback.Health);
            sensor.AddObservation(BlackBoardBehaviour.Instance.GetBarrierHealthByAlignment(_opponentMove.Alignment));
            sensor.AddObservation(TouchingBarrier);
            sensor.AddObservation(TouchingOpponentBarrier);

            sensor.AddObservation(StateMachine.CurrentState.GetHashCode());

            //Space size must be estimate. Take max hitbox detection and multiply by amount of vector values. 
            //Estimated space size increase: +72
            foreach (HitColliderBehaviour collider in BlackBoardBehaviour.Instance.GetOpponentActiveColliders(_movementBehaviour.Alignment))
            {
                sensor.AddObservation(collider.transform.position);

                if (collider.RB)
                    sensor.AddObservation(collider.RB.velocity);
            }

            //Opponent sensor items
            //Space size: 20
            //Movement and position observations
            sensor.AddObservation(_opponent.transform.position);
            sensor.AddObservation(_opponentMove.Position);
            sensor.AddObservation(_opponentMove.IsMoving);
            sensor.AddObservation(_movementBehaviour.MoveDirection);
            sensor.AddObservation(_opponentKnocback.Physics.LastVelocity);

            //Ability observations
            sensor.AddObservation(_opponentMoveset.AbilityInUse);
            sensor.AddObservation(_opponentMoveset.Energy);
            sensor.AddObservation(_opponentMoveset.CanBurst);
            sensor.AddObservation(_opponentMoveset.GetCanUseAbility());

            special1 = _opponentMoveset.GetAbilityInCurrentSlot(0);
            special2 = _opponentMoveset.GetAbilityInCurrentSlot(1);

            if (special1 != null)
                sensor.AddObservation(special1.abilityData.ID);

            if (special2 != null)
                sensor.AddObservation(special2.abilityData.ID);

            int opponentNextAbilityID = _opponentMoveset.NextAbilitySlot == null ? 0 : _opponentMoveset.NextAbilitySlot.abilityData.ID;
            sensor.AddObservation(opponentNextAbilityID);
        }

        public override void OnActionReceived(float[] vectorAction)
        {
                _shouldAttack = vectorAction[0] == 1;
            Debug.Log(vectorAction[0]);
            if (_bufferedAction?.HasAction() != true)
            {
                Debug.Log("Should Attack: " + _shouldAttack);
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

        public void Update()
        {
            _executor.enabled = EnableBehaviourTree;

            _executor.blackboard.boolParams[1] = GridPhysics.IsGrounded;

            if (_bufferedAction?.HasAction() == true)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            if (_executor.enabled) return;

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
