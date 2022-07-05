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

        private HealthBehaviour _health;
        
        private DelayedAction _stopAction;
        private float _hitStopScale = 0.4f;

        private void Awake()
        {
            _physics = GetComponent<GridPhysicsBehaviour>();
            _health = GetComponent<HealthBehaviour>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Ability"))
                return;
            
            HitColliderBehaviour collider = other.attachedRigidbody.GetComponent<HitColliderBehaviour>();

            if (!collider)
                return;
            
            if (collider.Owner == gameObject || _health.IsInvincible)
                return;

            collider.ColliderInfo.HitStopTimeModifier = Mathf.Clamp(collider.ColliderInfo.HitStopTimeModifier, 1, 5);
            float time = collider.ColliderInfo.HitStunTime * _hitStopScale * collider.ColliderInfo.HitStopTimeModifier;
            StartHitStop(time, collider.ColliderInfo.HitStunTime / 7);
            
            collider.Owner.GetComponent<HitStopBehaviour>().StartHitStop(time, collider.ColliderInfo.HitStunTime / 7);
        }

        public void StartHitStop(float time, float animationStopDelay)
        {
            if (_stopAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_stopAction);

            _physics.FreezeInPlaceByTimer(time, true, true);
            RoutineBehaviour.Instance.StartNewTimedAction(args => _animator.enabled = false,
                TimedActionCountType.SCALEDTIME, animationStopDelay);
            _stopAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _animator.enabled = true, TimedActionCountType.SCALEDTIME, time);
        }
    }
}
