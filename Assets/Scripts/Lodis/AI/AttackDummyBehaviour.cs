using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BBUnity;
using Lodis.GridScripts;

namespace Lodis.AI
{
    public class AttackDummyBehaviour : MonoBehaviour
    {
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
        private float _senseRadius = 6;
        private BehaviorExecutor _executor;
        private Coroutine _moveRoutine;
        private PanelBehaviour _moveTarget;
        private bool _needPath;
        private List<PanelBehaviour> _currentPath;
        private int _currentPathIndex;
        private GameObject _opponent;
        private AIDummyMovementBehaviour _movementBehaviour;
        private AttackDecisionTree _attackDecisions;
        private DefenseDecisionTree _defenseDecisions;
        [SerializeField]
        private int _maxDecisionCount;
        [Tooltip("The amount of time the dummy has to be in knock back to consider using a burst.")]
        [SerializeField]
        private float _timeNeededToBurst;
        private GridPhysicsBehaviour _gridPhysics;
        public float SenseRadius { get => _senseRadius; set => _senseRadius = value; }
        public StateMachine StateMachine { get => _stateMachine; }
        public GameObject Opponent { get => _opponent; }
        public MovesetBehaviour Moveset { get => _moveset; set => _moveset = value; }
        public AIDummyMovementBehaviour AIMovement { get => _movementBehaviour; }
        public AttackDecisionTree AttackDecisions { get => _attackDecisions; }
        public GridPhysicsBehaviour GridPhysics { get => _gridPhysics; }
        public BehaviorExecutor Executor { get => _executor; }
        public DefenseDecisionTree DefenseDecisions { get => _defenseDecisions; }
        public float TimeNeededToBurst { get => _timeNeededToBurst; }

        public DefenseNode LastDefenseDecision;

        public Vector2 MovePosition;
        public bool EnableBehaviourTree;

        // Start is called before the first frame update
        void Start()
        {
            if (EnableBehaviourTree)
            {
                _attackDecisions = new AttackDecisionTree();
                _attackDecisions.MaxDecisionsCount = _maxDecisionCount;
                _attackDecisions.Load(name);
                _defenseDecisions = new DefenseDecisionTree();
                _defenseDecisions.MaxDecisionsCount = _maxDecisionCount;
                _defenseDecisions.Load(name);
            }

            Moveset = GetComponent<Gameplay.MovesetBehaviour>();
            _stateMachine = GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            _knockbackBehaviour = GetComponent<Movement.KnockbackBehaviour>();
            _executor = GetComponent<BehaviorExecutor>();
            _movementBehaviour = GetComponent<AIDummyMovementBehaviour>();
            _gridPhysics = GetComponent<GridPhysicsBehaviour>();

            if (GetComponent<GridMovementBehaviour>().Alignment == GridAlignment.LEFT)
                _opponent = BlackBoardBehaviour.Instance.Player2;
            else
                _opponent = BlackBoardBehaviour.Instance.Player1;

        }

        public List<HitColliderBehaviour> GetAttacksInRange()
        {
            if (_attacksInRange.Count > 0)
                _attacksInRange.RemoveAll(hitCollider =>
                { 
                    if ((object)hitCollider != null)
                        return hitCollider == null;

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

        private void OnTriggerEnter(Collider other)
        {
            HitColliderBehaviour collider = other.GetComponent<HitColliderBehaviour>();
            if (collider)
                GetAttacksInRange().Add(collider);

        }


        public void Update()
        {
            _executor.enabled = EnableBehaviourTree;

            _executor.blackboard.boolParams[1] = GridPhysics.IsGrounded;

            if (_executor.enabled) return;

            //Only attack if the dummy is grounded and delay timer is up
            if ((StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Attacking") && Time.time - _timeOfLastAttack >= _attackDelay && !_knockbackBehaviour.RecoveringFromFall && !_chargingAttack)
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

        private void OnApplicationQuit()
        {
            _attackDecisions?.Save(name);
            _defenseDecisions?.Save(name);
        }
    }
}
