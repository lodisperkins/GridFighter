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
        
        public void StartHitStop()
        {
            float animationStopDelay = _health.LastCollider.ColliderInfo.HitStunTime * 0.05f;

            _health.LastCollider.ColliderInfo.HitStopTimeModifier = Mathf.Clamp(_health.LastCollider.ColliderInfo.HitStopTimeModifier, 1, 5);
            float time = _health.LastCollider.ColliderInfo.HitStunTime * _hitStopScale * _health.LastCollider.ColliderInfo.HitStopTimeModifier;

            _health.LastCollider.Owner.GetComponent<HitStopBehaviour>().StartHitStop(time, animationStopDelay, false, false);
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
