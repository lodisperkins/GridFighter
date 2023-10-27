using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Input;
using Lodis.Utility;
using Lodis.FX;
using Unity.MLAgents.Policies;

namespace Lodis.AI
{
    public class AIAgentBehaviour : Agent, IControllable
    {
        private static int _teamID;
        [SerializeField]
        private GameObject _character;
        private Gameplay.MovesetBehaviour _moveset;
        [Tooltip("Sets the value that amplifies the power of strong attacks")]
        [SerializeField]
        private float _attackStrength;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private Vector2 _attackDirection;
        private StateMachine _stateMachine;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private PanelBehaviour _moveTarget;

        private GameObject _opponent;
        private GridMovementBehaviour _opponentMove;
        private KnockbackBehaviour _opponentKnocback;
        private CharacterDefenseBehaviour _opponentDefense;

        private CharacterDefenseBehaviour _defense;
        private GridMovementBehaviour _movementBehaviour;

        private bool _touchingBarrier;
        private bool _touchingOpponentBarrier;
        private GridPhysicsBehaviour _gridPhysics;
        private IntVariable _playerID;
        private BufferedInput _bufferedAction;

        public Vector2 MovePosition;
        private bool _abilityBuffered;
        private MovesetBehaviour _opponentMoveset;
        private Vector2 _storedMoveInput;
        private bool _initialized;
        private BufferedInput _bufferedMoveAction;

        public StateMachine StateMachine { get => _stateMachine; }
        public GameObject Opponent { get => _opponent; }
        public MovesetBehaviour Moveset { get => _moveset; set => _moveset = value; }
        public GridMovementBehaviour AIMovement { get => _movementBehaviour; }
        public GridPhysicsBehaviour GridPhysics { get => _gridPhysics; }
        public IntVariable PlayerID { get => _playerID; set => _playerID = value; }
        public GameObject Character { get => _character; set => _character = value; }
        public bool Enabled { get => enabled; set => enabled = value; }
        public GridMovementBehaviour OpponentMove { get => _opponentMove; private set => _opponentMove = value; }
        public KnockbackBehaviour OpponentKnockback { get => _opponentKnocback; private set => _opponentKnocback = value; }
        public KnockbackBehaviour Knockback { get => _knockbackBehaviour; private set => _knockbackBehaviour = value; }
        public CharacterDefenseBehaviour Defense { get => _defense; private set => _defense = value; }
        public CharacterDefenseBehaviour OpponentDefense { get => _opponentDefense; private set => _opponentDefense = value; }

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

        public bool HasBuffered { get => _bufferedAction?.HasAction() == true; }

        private void Awake()
        {
            GetComponent<BehaviorParameters>().TeamId = _teamID;
            _teamID++;
            Initialize();
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
                MatchManagerBehaviour.Instance.Restart();
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
            base.OnActionReceived(vectorAction);

            if (Character == null || _bufferedAction?.HasAction() == true)
                return;

            //Store attack direction first to be used to decide which abiity to use
            AttackDirection = GetAttackDirection(vectorAction);

            Debug.Log("Attack Direction: " + vectorAction[3]);

            //Deciding to wall jump
            if (vectorAction[2] == 1 && _knockbackBehaviour.CurrentAirState == AirState.TUMBLING)
                AttackDirection = Character.transform.forward;

            Debug.Log("Wall Jump: " + vectorAction[2]);

            //Using the values to choose a move direction
            if (vectorAction[1] == 1)
                BufferMovement(Vector2.up);
            else if (vectorAction[1] == 2)
                BufferMovement(Vector2.down);
            else if (vectorAction[1] == 3)
                BufferMovement(Vector2.left);
            else if (vectorAction[1] == 4)
                BufferMovement(Vector2.right);

            Debug.Log("Move Direction: " + vectorAction[1]);
            //Deciding to burst
            if (vectorAction[4] == 1 && _moveset.CanBurst)
                BufferBurst();

            Debug.Log("Burst: " + vectorAction[4]);

            if (_abilityBuffered)
                return;


            Debug.Log("Attack: " + vectorAction[0]);

            //Deciding to use weak attack
            if (vectorAction[0] == 1)
                BufferNormalAbility(false, AttackDirection);
            //Deciding to use strong attack
            else if (vectorAction[0] == 2)
                BufferNormalAbility(true, AttackDirection);
            //Deciding to use a specific special attack slot
            else if (vectorAction[0] == 3)
                BufferSpecialAbility(0, AttackDirection);
            else if (vectorAction[0] == 4)
                BufferSpecialAbility(1, AttackDirection);
            //Deciding to shuffle
            else if (vectorAction[0] == 5)
                BufferShuffle();


        }

        private Vector2 GetAttackDirection(float[] vectorAction)
        {
            Vector2 attackDirection = Vector2.zero;

            if (vectorAction[3] == 0)
                attackDirection = Vector2.up;
            else if (vectorAction[3] == 1)
                attackDirection = Vector2.down;
            else if (vectorAction[3] == 2)
                attackDirection = Vector2.left;
            else if (vectorAction[3] == 3)
                attackDirection = Vector2.right;

            return attackDirection;
        }

        private void OnTriggerEnter(Collider other)
        {

            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

            if (!other.CompareTag("Structure") || _knockbackBehaviour?.CurrentAirState != AirState.TUMBLING)
                return;

            RingBarrierBehaviour ringBarrier = other.GetComponentInParent<RingBarrierBehaviour>();

            if (!ringBarrier)
                return;

            if (ringBarrier.Owner == Character)
                TouchingBarrier = true;
            else
                TouchingOpponentBarrier = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Structure") || _knockbackBehaviour?.CurrentAirState != AirState.TUMBLING)
                return;

            RingBarrierBehaviour ringBarrier = other.GetComponentInParent<RingBarrierBehaviour>();

            if (!ringBarrier)
                return;

            if (ringBarrier.Owner == Character)
                TouchingBarrier = false;
            else
                TouchingOpponentBarrier = false;
        }

        public void BufferBurst()
        {
            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action => _moveset.UseBasicAbility(AbilityType.BURST, null), condition => { _abilityBuffered = false; return true; }, 0.2f);
            _abilityBuffered = true;
        }

        private void BufferShuffle()
        {
            if (_moveset.LoadingShuffle || _moveset.DeckReloading)
                return;

            _bufferedAction = new BufferedInput(action => _moveset.ManualShuffle(), condition => StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Moving", 0.2f);
        }

        /// <summary>
        /// Buffers input on the y axis
        /// </summary>
        /// <param name="y"></param>
        public void BufferMovement(Vector2 direction)
        {
            if (_movementBehaviour.CanMove)
                _storedMoveInput = direction;

            _bufferedMoveAction = new BufferedInput(action => _movementBehaviour.MoveToPanel(_storedMoveInput + _movementBehaviour.Position), condition => _storedMoveInput.magnitude > 0 && !_movementBehaviour.IsMoving && _movementBehaviour.CanMove, 0.2f);
        }

        /// <summary>
        /// Decides which ability to use based on the input context and activates it
        /// </summary>
        /// <param name="context">The input callback context</param>
        /// <param name="args">Any additional arguments to give to the ability. 
        public void BufferNormalAbility(bool isStrong, Vector2 attackDirection)
        {
            float attackStrength = 0.15f * 0.1f + 1;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            AbilityType abilityType = Ability.GetNormalType(attackDirection, isStrong);

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action =>
                Moveset.UseBasicAbility(abilityType, attackStrength, attackDirection), condition =>
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
        public void BufferSpecialAbility(int slotIndex, Vector2 attackDirection)
        {
            float attackStrength = 0.15f * 0.1f + 1;
            _attackDirection.x *= Mathf.Round(transform.forward.x);

            //Use a normal ability if it was not held long enough
            _bufferedAction = new BufferedInput(action =>
                Moveset.UseSpecialAbility(slotIndex, attackStrength, attackDirection), condition =>
            {
                _abilityBuffered = false;
                return _moveset.GetCanUseAbility() && !FXManagerBehaviour.Instance.SuperMoveEffectActive;
            }, 0.2f);
            _abilityBuffered = true;
        }

        private void Update()
        {

            if (_bufferedAction?.HasAction() == true)
                _bufferedAction.UseAction();
            else
                _abilityBuffered = false;

            if (_bufferedMoveAction?.HasAction() == true)
                _bufferedMoveAction.UseAction();
        }
    }
}