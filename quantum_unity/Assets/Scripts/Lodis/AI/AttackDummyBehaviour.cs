using Ilumisoft.VisualStateMachine;
using Lodis.Gameplay;
using Lodis.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BBUnity;
using Lodis.GridScripts;
using Lodis.Input;
using Lodis.ScriptableObjects;
using Lodis.FX;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Lodis.Utility;
using Assets.Scripts.Lodis.AI;

namespace Lodis.AI
{
    public class AttackDummyBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _character;
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
        [SerializeField]
        private float _maxRange;

        [Tooltip("The direction on the grid this dummy is looking in. Useful for changing the direction of attacks")]
        [SerializeField]
        private Vector2 _attackDirection;
        private StateMachine _stateMachine;
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        private int _lastSlot;
        [SerializeField]
        private bool _enableRandomBehaviour;
        private bool _chargingAttack;
       

        public StateMachine StateMachine { get => _stateMachine; }
       
        public MovesetBehaviour Moveset { get => _moveset; set => _moveset = value; }
    
        public KnockbackBehaviour Knockback { get => _knockbackBehaviour; private set => _knockbackBehaviour = value; }
      

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

        public GameObject Character { get => _character; set => _character = value; }

        private void Start()
        {
            _stateMachine = Character.GetComponent<Gameplay.CharacterStateMachineBehaviour>().StateMachine;
            Knockback = Character.GetComponent<Movement.KnockbackBehaviour>();
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

        public void Update()
        {
            //Only attack if the dummy is grounded and delay timer is up
            if ((StateMachine.CurrentState == "Idle" || StateMachine.CurrentState == "Attacking") && Time.time - _timeOfLastAttack >= _attackDelay && !_knockbackBehaviour.LandingScript.RecoveringFromFall && !_chargingAttack)
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
                    Moveset.UseBasicAbility(_attackType, new object[] { _attackStrength, _attackDirection });

                _timeOfLastAttack = Time.time;
            }


        }
    }
}
