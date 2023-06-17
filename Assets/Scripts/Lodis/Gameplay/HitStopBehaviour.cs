using System;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
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
        private DelayedAction _stopAction;
        private TimedAction _enableAction;
        private float _hitStopScale = 0.15f;

        public Animator Animator { get => _animator; set => _animator = value; }

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
        }
        
        /// <summary>
        /// Freezes and shakes this game object to add extra effect to a hit.
        /// </summary>
        private void StartHitStop()
        {
            //Gets the data for the last collider to hit this object to determine the length of the hit stun
            HitColliderData lastColliderInfo = _health.LastCollider.ColliderInfo;
            //Calculates a small delay for the animation so that it syncs up better with the hit
            float animationStopDelay = lastColliderInfo.HitStunTime * 0.05f;
            //Clamps the modifier so the character isn't frozen for too long
            lastColliderInfo.HitStopTimeModifier = Mathf.Clamp(lastColliderInfo.HitStopTimeModifier, 1, 5);
            //The length of the hit stop is found by combining the globla hit stop scale with the hit stun time and teh ability's modifier
            float time = lastColliderInfo.HitStunTime * _hitStopScale * lastColliderInfo.HitStopTimeModifier;

            if (time == 0)
                return;

            if (_health.LastCollider.Owner)
            { 
                //Starts the hit stop for the attacker
                _health.LastCollider.Owner.GetComponent<HitStopBehaviour>().StartHitStop(time, animationStopDelay, false, false, false);
            }

            //Gets the total knockback of the attack so the camera only shakes if the attack is strong enough
            float totalKnockback = KnockbackBehaviour.GetTotalKnockback(lastColliderInfo.BaseKnockBack, lastColliderInfo.KnockBackScale, _health.Health);

            //Call the same function with the new parameters found
            StartHitStop(time, animationStopDelay, true, true, lastColliderInfo.ShakesCamera);
        }

        /// <summary>
        /// Freezes and shakes this game object to add extra effect to a hit.
        /// </summary>
        /// <param name="time">The amount of time to stay in hit stop.</param>
        /// <param name="animationStopDelay">How long the animation should be delayed so that it syncs up with the hitstop.</param>
        /// <param name="waitForForceApplied">Whether or not we should wait until a force is applied before the hit stop is active.</param>
        /// <param name="shakeCharacter">Whether or not the character model should shake during the hitstop.</param>
        /// <param name="shakeCamera">Whether or not the camera should shake during the hitstop effect.</param>
        public void StartHitStop(float time, float animationStopDelay, bool waitForForceApplied, bool shakeCharacter, bool shakeCamera)
        {
            //If there is already a timer to make the object stop, cancel it.
            if (_stopAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_stopAction);
            //If there is already a timer to make the object enabled, cancel it.
            if (_enableAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_enableAction);

            //Shake the camera or the chracter based on the arguments given
            if (shakeCharacter)
                _shakeBehaviour.ShakePosition(time, 0.8f, 1000);
            if (shakeCamera)
                CameraBehaviour.ShakeBehaviour.ShakeRotation(0.05f, 0.5f, 1);

            //Calls for the physics component to freexe the object in place so it doesn't keep moving in air.
            _physics.FreezeInPlaceByTimer(time, true, true, waitForForceApplied, true);
            //The animator should be disabled only after the animation stop delay time has passed.


            _enableAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {

                RoutineBehaviour.Instance.CharacterTimeScale = 1;
            }, TimedActionCountType.SCALEDTIME, time);

            RoutineBehaviour.Instance.CharacterTimeScale = 0;
        }
    }
}
