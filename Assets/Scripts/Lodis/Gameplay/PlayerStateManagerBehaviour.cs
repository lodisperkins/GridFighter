using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public enum PlayerState
    {
        IDLE,
        MOVING,
        ATTACKING,
        KNOCKBACK,
        FREEFALL,
        PARRYING,
        FALLBREAKING,
        LANDING,
        DOWN,
        GROUNDRECOVERY,
        STUNNED
    }

    public class PlayerStateManagerBehaviour : MonoBehaviour
    {

        private Movement.KnockbackBehaviour _knockBack;
        private MovesetBehaviour _moveset;
        private Input.InputBehaviour _input;
        private Movement.GridMovementBehaviour _movement;
        private CharacterDefenseBehaviour _characterDefense;
        [Tooltip("The current state the character is in")]
        [SerializeField]
        private PlayerState _currentState;


        // Start is called before the first frame update
        void Start()
        {
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _characterDefense = GetComponent<CharacterDefenseBehaviour>();
        }

        public PlayerState CurrentState
        {
            get { return _currentState; }
            private set { _currentState = value; }
        }

        // Update is called once per frame
        void Update()
        {
            if (_knockBack.Stunned)
                _currentState = PlayerState.STUNNED;
            else if (_characterDefense.BreakingFall)
                _currentState = PlayerState.FALLBREAKING;
            else if (_knockBack.Landing)
                _currentState = PlayerState.LANDING;
            else if (_knockBack.RecoveringFromFall)
                _currentState = PlayerState.GROUNDRECOVERY;
            else if (_characterDefense.IsParrying)
                _currentState = PlayerState.PARRYING;
            else if (_knockBack.InHitStun)
                _currentState = PlayerState.KNOCKBACK;
            else if (_moveset.AbilityInUse)
                _currentState = PlayerState.ATTACKING;
            else if (_knockBack.InFreeFall)
                _currentState = PlayerState.FREEFALL;
            else if (_movement.IsMoving)
                _currentState = PlayerState.MOVING;
            else
                _currentState = PlayerState.IDLE;
        }
    }
}
