using System;
using Lodis.Movement;
using Lodis.Utility;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class HitStopBehaviour : MonoBehaviour
    {
        private GridPhysicsBehaviour _physics;

        [SerializeField] private Animator _animator;
        [SerializeField] private ShakeBehaviour _shakeBehaviour;

        private HealthBehaviour _health;
        
        private DelayedAction _stopAction;
        private float _hitStopScale = 0.15f;
        private HitColliderBehaviour _lastCollider;

        private void Awake()
        {
            _physics = GetComponent<GridPhysicsBehaviour>();
            _health = GetComponent<HealthBehaviour>();
            KnockbackBehaviour knockBack = (KnockbackBehaviour)_health;

            if (knockBack != null)
                knockBack.AddOnKnockBackAction(StartHitStop);
            else
                _health.AddOnTakeDamageAction(StartHitStop);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Ability"))
                return;

            HitColliderBehaviour collider = null;
            if (other.attachedRigidbody)
                collider = other.attachedRigidbody.GetComponent<HitColliderBehaviour>();
            
            if(!collider)
                collider = other.GetComponent<HitColliderBehaviour>();

            if (!collider)
                return;
            
            if (collider.Owner == gameObject || _health.IsInvincible)
                return;

            _lastCollider = collider;
        }

        public void StartHitStop()
        {
            float animationStopDelay = _lastCollider.ColliderInfo.HitStunTime * 0.05f;

            _lastCollider.ColliderInfo.HitStopTimeModifier = Mathf.Clamp(_lastCollider.ColliderInfo.HitStopTimeModifier, 1, 5);
            float time = _lastCollider.ColliderInfo.HitStunTime * _hitStopScale * _lastCollider.ColliderInfo.HitStopTimeModifier;

            _lastCollider.Owner.GetComponent<HitStopBehaviour>().StartHitStop(time, animationStopDelay, false, false);
            StartHitStop(time, animationStopDelay, true, true);
        }

        public void StartHitStop(float time, float animationStopDelay, bool waitForForceApplied, bool shake)
        {
            if (_stopAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_stopAction);

            if (shake)
                _shakeBehaviour.ShakePosition(time, 0.3f, 1000);

            _physics.FreezeInPlaceByTimer(time, true, true, waitForForceApplied);
            RoutineBehaviour.Instance.StartNewTimedAction(args => _animator.enabled = false,
                TimedActionCountType.SCALEDTIME, animationStopDelay);
            _stopAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _animator.enabled = true, TimedActionCountType.SCALEDTIME, time);
        }
    }
}
