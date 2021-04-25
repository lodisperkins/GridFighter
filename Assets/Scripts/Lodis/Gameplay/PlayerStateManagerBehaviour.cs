using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public enum PlayerState
    {
        IDLE,
        ATTACKING,
        KNOCKBACK,
        DOWN
    }

    public class PlayerStateManagerBehaviour : MonoBehaviour
    {

        private Movement.KnockbackBehaviour _knockBack;
        private MovesetBehaviour _moveset;
        private Input.InputBehaviour _input;
        private Movement.GridMovementBehaviour _movement;
        private PlayerState _currentState;

        // Start is called before the first frame update
        void Start()
        {
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
        }

        public PlayerState CurrentState
        {
            get { return _currentState; }
        }

        // Update is called once per frame
        void Update()
        {
            if (_knockBack.InHitStun)
                _currentState = PlayerState.KNOCKBACK;
            else if (_moveset.AbilityInUse)
                _currentState = PlayerState.ATTACKING;
            else
                _currentState = PlayerState.IDLE;
        }
    }
}
