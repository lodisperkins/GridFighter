   using System.Collections;
using System.Collections.Generic;
using Lodis.Utility;
using UnityEngine;
using Ilumisoft.VisualStateMachine;
using Lodis.Movement;
using UnityEngine.Events;

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
        private string _lastState;
        private UnityEvent<string> _onStateChanged;

        public StateMachine StateMachine { get => _stateMachine; }
        public string LastState { get => _lastState; private set => _lastState = value; }
        public string CurrentState { get => _stateMachine.CurrentState; }

        private void Awake()
        {
            _onStateChanged = new UnityEvent<string>();
        }

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
            _stateMachine.SetTransitionCondition("Tumbling-BreakingFall", args => _characterDefense.BreakingFall);
            _stateMachine.SetTransitionConditionByLabel("Land", args => 
            { return _knockBack.LandingScript.Landing && !_characterDefense.BreakingFall; });
            _stateMachine.SetTransitionCondition("Landing-Down", args => _knockBack.LandingScript.IsDown);
            _stateMachine.SetTransitionCondition("Down-GroundRecovery", args => _knockBack.LandingScript.RecoveringFromFall);
            _stateMachine.SetTransitionConditionByLabel("Parry", args => (_characterDefense.IsShielding || _characterDefense.IsParrying) && !_characterDefense.IsPhaseShifting && !_characterDefense.IsResting);
            _stateMachine.SetTransitionConditionByLabel("Tumbling", args => _knockBack.CurrentAirState == AirState.TUMBLING);
            _stateMachine.SetTransitionCondition("Any-Flinching", condition => false);
            _knockBack.AddOnHitStunAction(() => _stateMachine.Trigger("Any-Flinching"));
            _stateMachine.SetTransitionCondition("Any-FreeFall", args => _knockBack.CurrentAirState == AirState.FREEFALL);
            _stateMachine.SetTransitionConditionByLabel("Moving", args => _movement.IsMoving && !_moveset.AbilityInUse);
            _stateMachine.SetTransitionConditionByLabel("Shuffling", args => _moveset.LoadingShuffle);
            _stateMachine.SetTransitionCondition("Any-Idle", args => _knockBack.CheckIfIdle() && !_movement.IsMoving && !_characterDefense.BreakingFall &&
            !_characterDefense.IsDefending && !_characterDefense.IsResting && !_moveset.AbilityInUse && !_moveset.LoadingShuffle);
        }

        public void AddOnStateChangedAction(UnityAction<string> action)
        {
            _onStateChanged?.AddListener(action);
        }

        /// <summary>
        /// Returns true if the current state is one of the given states.
        /// </summary>
        public bool CompareState(params string[] states)
        {
            foreach (var item in states)
            {
                if (CurrentState == item)
                    return true;
            }

            return false;
        }

        private void Update()
        {
            //if (_currentState != _stateMachine.CurrentState)
            //    Debug.Log(_stateMachine.CurrentState);d


            _currentState = _stateMachine.CurrentState;

            if (_currentState != LastState)
            {
                _onStateChanged?.Invoke(_currentState);
                LastState = _currentState;
            }
        }
    }
}
