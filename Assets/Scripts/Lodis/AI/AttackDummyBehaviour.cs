using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class AttackDummyBehaviour : MonoBehaviour
    {
        private Gameplay.MovesetBehaviour _moveset;
        [SerializeField]
        private Gameplay.BasicAbilityType _attackType;
        [SerializeField]
        private float _attackDelay;
        private float _timeOfLastAttack;
        [SerializeField]
        private float _attackStrength;
        [SerializeField]
        private float _zDirection;
        private Gameplay.PlayerStateManagerBehaviour _playerState;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        // Start is called before the first frame update
        void Start()
        {
            _moveset = GetComponent<Gameplay.MovesetBehaviour>();
            _playerState = GetComponent<Gameplay.PlayerStateManagerBehaviour>();
            _knockbackBehaviour = GetComponent<Movement.KnockbackBehaviour>();
        }

        public void Update()
        {
            if (!_knockbackBehaviour.InHitStun && !_knockbackBehaviour.InFreeFall && Time.time - _timeOfLastAttack >= _attackDelay)
            {
                _zDirection = Mathf.Clamp(_zDirection, -1, 1);
                _moveset.UseBasicAbility(_attackType, new object[]{_attackStrength, _zDirection});
                _timeOfLastAttack = Time.time;
            }
        }
    }
}
