using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        // Start is called before the first frame update
        void Start()
        {
            _moveset = GetComponent<Gameplay.MovesetBehaviour>();
            _stateMachine = GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            _knockbackBehaviour = GetComponent<Movement.KnockbackBehaviour>();
        }

        private IEnumerator ChargeRoutine(float chargeTime)
        {
            _chargingAttack = true;
            yield return new WaitForSeconds(chargeTime);

            if ((_stateMachine.CurrentState == "Idle" || _stateMachine.CurrentState == "Attacking"))
            {
                _moveset.UseBasicAbility(_attackType, new object[] { _attackStrength, _attackDirection });
            }
                _chargingAttack = false;
        }

        public void Update()
        {
            //Only attack if the dummy is grounded and delay timer is up
            if ((_stateMachine.CurrentState == "Idle" || _stateMachine.CurrentState == "Attacking") && Time.time - _timeOfLastAttack >= _attackDelay && !_knockbackBehaviour.RecoveringFromFall && !_chargingAttack)
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
                        StartCoroutine(ChargeRoutine((_attackStrength - 1) / 0.1f));
                        return;
                    }
                }

                if (_attackType == Gameplay.AbilityType.NONE || _stateMachine.CurrentState == "Stunned")
                    return;

                //Attack based on the ability type selected
                if (_attackType == Gameplay.AbilityType.SPECIAL)
                {
                    if (_lastSlot == 0)
                        _lastSlot = 1;
                    else
                        _lastSlot = 0;

                    _moveset.UseSpecialAbility(_lastSlot, new object[] { _attackStrength, _attackDirection });
                }
                else
                    _moveset.UseBasicAbility(_attackType, new object[]{_attackStrength, _attackDirection});

                _timeOfLastAttack = Time.time;
            }
        }
    }
}
