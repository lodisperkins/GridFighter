using System.Collections;
using System.Collections.Generic;
using Lodis.Utility;
using UnityEngine;
using Ilumisoft.VisualStateMachine;

namespace Lodis.Gameplay
{
    public class CharacterStateMachineBehaviour : MonoBehaviour
    {
        [SerializeField]
        private StateMachine _stateMachine;
        private Movement.KnockbackBehaviour _knockBack;
        private MovesetBehaviour _moveset;
        private Input.InputBehaviour _input;
        private Movement.GridMovementBehaviour _movement;
        private CharacterDefenseBehaviour _characterDefense;
        [SerializeField]
        private string _currentState;

        public StateMachine StateMachine { get => _stateMachine; }


        // Start is called before the first frame update
        void Start()
        {
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _characterDefense = GetComponent<CharacterDefenseBehaviour>();
            _stateMachine.SetTransitionCondition("Any-Stunned", args => _knockBack.Stunned);
            _stateMachine.SetTransitionConditionByLabel("Attack", args => _moveset.AbilityInUse);
            _stateMachine.SetTransitionCondition("Parrying-BreakingFall", args => _characterDefense.BreakingFall);
            _stateMachine.SetTransitionConditionByLabel("Land", args => 
            { return _knockBack.Landing; });
            _stateMachine.SetTransitionCondition("Landing-Down", args => _knockBack.IsDown);
            _stateMachine.SetTransitionCondition("Down-GroundRecovery", args => _knockBack.RecoveringFromFall);
            _stateMachine.SetTransitionConditionByLabel("Parry", args => _characterDefense.IsParrying);
            _stateMachine.SetTransitionCondition("Any-Tumbling", args => _knockBack.Tumbling);
            _stateMachine.SetTransitionCondition("Any-FreeFall", args => _knockBack.InFreeFall);
            _stateMachine.SetTransitionCondition("Idle-Moving", args => _movement.IsMoving);
            _stateMachine.SetTransitionCondition("Any-Idle", args => _knockBack.CheckIfIdle() && !_movement.IsMoving && !_characterDefense.BreakingFall && !_characterDefense.IsParrying && !_moveset.AbilityInUse && !_knockBack.IsDown && !_knockBack.RecoveringFromFall && !_knockBack.Landing);
        }
        private void Update()
        {
            _currentState = _stateMachine.CurrentState;
        }
    }
}
