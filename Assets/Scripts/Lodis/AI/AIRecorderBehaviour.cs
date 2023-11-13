using Assets.Scripts.Lodis.AI;
using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class AIRecorderBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _character;
        [SerializeField]
        private string _recordingName;
        private DecisionTree _actionTree;
        
        private Gameplay.MovesetBehaviour _moveset;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private Vector2 _attackDirection;
        private StateMachine _stateMachine;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private List<HitColliderBehaviour> _attacksInRange = new List<HitColliderBehaviour>();
        private GridMovementBehaviour _movementBehaviour;

        private GameObject _opponent;
        private GridMovementBehaviour _opponentMove;
        private KnockbackBehaviour _opponentKnocback;

        [SerializeField]
        private int _maxDecisionCount;
        private GridPhysicsBehaviour _gridPhysics;
        private GridPhysicsBehaviour _opponentGridPhysics;
        private IntVariable _playerID;

        public Vector2 MovePosition;

        private Vector3 _opponentVelocity;
        private Vector3 _opponentDisplacement;
        private float _opponentHealth;
        private List<HitColliderBehaviour> _lastAttacksInRange;
        private RingBarrierBehaviour _ownerBarrier;
        private RingBarrierBehaviour _opponentBarrier;
        private MovesetBehaviour _opponentMoveset;

        // Start is called before the first frame update
        void Start()
        {
            _actionTree = new DecisionTree(0.98f);
            _actionTree.MaxDecisionsCount = _maxDecisionCount;
            _actionTree.SaveLoadPath = Application.persistentDataPath + "/RecordedDecisionData";
            _actionTree.Load("_" + _recordingName);

            _movementBehaviour = _character.GetComponent<GridMovementBehaviour>();
            _moveset = _character.GetComponent<MovesetBehaviour>();
            _stateMachine = _character.GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            _knockbackBehaviour = _character.GetComponent<KnockbackBehaviour>();
            _gridPhysics = _character.GetComponent<GridPhysicsBehaviour>();

            _opponent = BlackBoardBehaviour.Instance.GetOpponentForPlayer(_character);
            _opponentMove = _opponent.GetComponent<GridMovementBehaviour>();
            _opponentKnocback = _opponent.GetComponent<KnockbackBehaviour>();
            _opponentGridPhysics = _opponent.GetComponent<GridPhysicsBehaviour>();
            _opponentMoveset = _opponent.GetComponent<MovesetBehaviour>();

            _moveset.OnUseAbility += () => CreateNewAction(true);
            _movementBehaviour.AddOnMoveBeginAction(() =>
            {
                if (_stateMachine.CurrentState != "Attacking")
                {
                    CreateNewAction(false);
                };
            }
            );

            _opponentBarrier = _movementBehaviour.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
            _ownerBarrier = _opponentMove.Alignment == GridAlignment.LEFT ? BlackBoardBehaviour.Instance.RingBarrierRHS : BlackBoardBehaviour.Instance.RingBarrierLHS;
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

        private void OnDestroy()
        {
            if (!Application.isEditor) return;
            _actionTree?.Save("_" + _recordingName);
        }

        private void CreateNewAction(bool isAttacking)
        {
            ActionNode action = new ActionNode(null, null);

            action.CurrentState = _stateMachine.CurrentState;

            action.AlignmentX = (int)_movementBehaviour.GetAlignmentX();
            action.AveragePosition = GetAveragePosition();
            action.AverageVelocity = GetAverageVelocity();
            action.MoveDirection = _movementBehaviour.MoveDirection;
            action.IsGrounded = _gridPhysics.IsGrounded;
            action.Energy = _moveset.Energy;

            if (isAttacking)
            {
                action.CurrentAbilityID = _moveset.LastAbilityInUse.abilityData.ID;
            }
            else
            {
                action.CurrentAbilityID = -1;
            }

            action.IsAttacking = isAttacking;

            action.Health = _knockbackBehaviour.Health;
            action.BarrierHealth = _ownerBarrier.Health;

            action.OwnerToTarget = _opponent.transform.position - _character.transform.position;

            action.OpponentVelocity = _opponentGridPhysics.LastVelocity;
            action.OpponentEnergy = _opponentMoveset.Energy;
            action.OpponentMoveDirection = _opponentMove.MoveDirection;
            action.OpponentHealth = _opponentKnocback.Health;
            action.OpponentBarrierHealth = _opponentBarrier.Health;

            _actionTree.AddDecision(action);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}