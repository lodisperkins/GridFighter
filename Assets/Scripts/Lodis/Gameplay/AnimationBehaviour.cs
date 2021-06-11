using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace Lodis.Gameplay
{
    enum AnimationPhase
    {
        STARTUP,
        ACTIVE,
        INACTIVE
    }

    public class AnimationBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Movement.GridMovementBehaviour _moveBehaviour;
        [SerializeField]
        private Animator _animator;
        private Ability _currentAbilityAnimating;
        [SerializeField]
        private RuntimeAnimatorController _runtimeAnimator;

        // Start is called before the first frame update
        void Start()
        {
        }

        private float CalculateAnimationSpeed(AnimationClip animation, AnimationPhase phase)
        {
            switch (phase)
            {
                case AnimationPhase.STARTUP:
                        return (_animator.speed * _currentAbilityAnimating.abilityData.startUpTime) / animation.events[0].time;
                case AnimationPhase.ACTIVE:
                    return (_animator.speed * _currentAbilityAnimating.abilityData.timeActive) / animation.events[1].time - animation.events[0].time;
                case AnimationPhase.INACTIVE:
                    return (_animator.speed * _currentAbilityAnimating.abilityData.recoverTime) / animation.length - animation.events[1].time;
            }

            return 1;
        }

        public void PlayAbilityAnimation(Ability ability)
        {
            AnimationClip animation;
            if (!ability.abilityData.GetCustomAnimation(out animation))
                return;

        }

        // Update is called once per frame
        void Update()
        {
            _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x);
            _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);
            Debug.Log(_moveBehaviour.MoveDirection);
        }
    }
}
