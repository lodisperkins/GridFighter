using GridGame.GamePlay.GridScripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class AttackDummyBehaviour : MonoBehaviour
    {
        private Gameplay.MovesetBehaviour _moveset;
        [SerializeField]
        private Gameplay.AbilityType _attackType;
        [SerializeField]
        private float _attackDelay;
        private float _timeOfLastAttack;
        [SerializeField]
        private float _attackStrength;
        [SerializeField]
        private float _zDirection;
        private Gameplay.PlayerStateManagerBehaviour _playerState;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private int _lastSlot;

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

                if ((int)_attackType == 9)
                    return;

                if ((int)_attackType == 8)
                {
                    if (_lastSlot == 0)
                        _lastSlot = 1;
                    else
                        _lastSlot = 0;

                    _moveset.UseSpecialAbility(_lastSlot, new object[] { _attackStrength, _zDirection });
                }
                else
                    _moveset.UseBasicAbility(_attackType, new object[]{_attackStrength, _zDirection});

                _timeOfLastAttack = Time.time;
            }
        }
    }
}
