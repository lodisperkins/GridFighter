using System;
using FixedPoints;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class HitStopBehaviour : MonoBehaviour
    {
        [Tooltip("This is the minimum knockback needed to make an attack shake the camera.")]
        [SerializeField] private FloatVariable _knockbackToShakeCamera;
        private GridPhysicsBehaviour _physics;
        [Tooltip("The animator attached to the character. Used to stop animations during hit stop.")]
        [SerializeField] private Animator _animator;
        [Tooltip("The shake script attached to the model. Used to make the model shake during hit stop.")]
        [SerializeField] private ShakeBehaviour _shakeBehaviour;
        private MovesetBehaviour _moveset;

        private HealthBehaviour _health;
        private FixedTimeAction _stopAction;
        private FixedTimeAction _enableAction;
        private Fixed32 _hitStopScale = 0.15f;
        [SerializeField]
        private bool _hitStopActive;
        private (Vector3, Vector3) _frozenMoveVectors;

        public Animator Animator { get => _animator; set => _animator = value; }
        public bool HitStopActive { get => _hitStopActive; private set => _hitStopActive = value; }

        private void Awake()
        {
            //Initialize component values
            _physics = GetComponent<GridPhysicsBehaviour>();
            _health = GetComponent<HealthBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();

            //Adds the hitstop event to the appropriate event based on the health scrip type
            KnockbackBehaviour knockBack = (KnockbackBehaviour)_health;
            if (knockBack != null)
                knockBack.AddOnKnockBackStartAction(StartHitStop);
            else
                _health.AddOnTakeDamageAction(StartHitStop);

            _moveset.OnUseAbility += () => CancelHitStop(false);
        }
        
        /// <summary>
        /// Freezes and shakes this game object to add extra effect to a hit.
        /// </summary>
        private void StartHitStop()
        {
            if (!_health.LastCollider)
                return;

            //Gets the data for the last collider to hit this object to determine the length of the hit stun
            HitColliderData lastColliderInfo = _health.LastCollider.ColliderInfo;
            //Calculates a small delay for the animation so that it syncs up better with the hit
            //Raw value is 0.05
            Fixed32 animationStopDelay = lastColliderInfo.HitStunTime * new Fixed32(3276);
            //The length of the hit stop is found by combining the globla hit stop scale with the hit stun time and teh ability's modifier
            Fixed32 time = lastColliderInfo.HitStunTime;

            if (time == 0)
                return;

            if (HitStopActive)
            {
                CancelHitStop(true);
            }
            //_frozenMoveVectors.Item1 += frozenVectors.Item1;
            //_frozenMoveVectors.Item2 += frozenVectors.Item2;

            if (_health.LastCollider.Spawner != null)
            { 
                //Starts the hit stop for the attacker
                //Raw value is 1.5
                _health.LastCollider.Spawner.UnityObject.GetComponent<HitStopBehaviour>().StartHitStop(time * new Fixed32(98304), animationStopDelay, false, false, false,0,0,0);
            }
            //Call the same function with the new parameters found
            StartHitStop(time, lastColliderInfo.HitStopShakeStrength, true, true, lastColliderInfo.ShakesCamera, lastColliderInfo.CameraShakeStrength, lastColliderInfo.CameraShakeDuration, lastColliderInfo.CameraShakeFrequency);
        }

        /// <summary>
        /// Freezes and shakes this game object to add extra effect to a hit.
        /// </summary>
        /// <param name="time">The amount of time to stay in hit stop.</param>
        /// <param name="animationStopDelay">How long the animation should be delayed so that it syncs up with the hitstop.</param>
        /// <param name="waitForForceApplied">Whether or not we should wait until a force is applied before the hit stop is active.</param>
        /// <param name="shakeCharacter">Whether or not the character model should shake during the hitstop.</param>
        /// <param name="shakeCamera">Whether or not the camera should shake during the hitstop effect.</param>
        public void StartHitStop(Fixed32 time, Fixed32 strength, bool waitForForceApplied, bool shakeCharacter, bool shakeCamera, Fixed32 cameraShakeStrength, Fixed32 cameraShakeDuration, int cameraShakeFrequency)
        {
            HitStopActive = true;
            //If there is already a timer to make the object enabled, cancel it.
            if (_enableAction?.IsActive == true)
                FixedPointTimer.StopAction(_enableAction);

            //Shake the camera or the chracter based on the arguments given
            if (shakeCharacter)
                _shakeBehaviour.ShakePosition(time, strength, 1000);
            if (shakeCamera)
                CameraBehaviour.ShakeBehaviour.ShakeRotation(cameraShakeStrength, cameraShakeDuration, cameraShakeFrequency);

            //Calls for the physics component to freexe the object in place so it doesn't keep moving in air.
            _physics.FreezeInPlaceByTimer(time, true, true, waitForForceApplied, true);

            //The animator should be disabled only after the animation stop delay time has passed.


            _enableAction = FixedPointTimer.StartNewTimedAction(() =>
            {

                RoutineBehaviour.Instance.CharacterTimeScale = 1;
                HitStopActive = false;

                //if (_physics.FrozenStoredForce.magnitude == 0 && _physics.FrozenVelocity.magnitude == 0)
                //{
                //    _physics.ApplyVelocityChange(_frozenMoveVectors.Item1);
                //    _physics.ApplyImpulseForce(_frozenMoveVectors.Item2);
                //}

                _frozenMoveVectors.Item1 = Vector3.zero;
                _frozenMoveVectors.Item2 = Vector3.zero;
            }, time);

            RoutineBehaviour.Instance.CharacterTimeScale = 0;
        }

        public void CancelHitStop(bool cancelFreeze)
        {
            if (_moveset?.LastAbilityInUse?.abilityData.AbilityType == AbilityType.BURST)
                return;


            if (cancelFreeze)
                _physics.CancelFreeze(true, true);

            HitStopActive = false;
            _shakeBehaviour.StopShaking();
            FixedPointTimer.StopAction(_enableAction);
            RoutineBehaviour.Instance.CharacterTimeScale = 1;
        }
    }
}
