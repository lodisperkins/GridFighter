using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lodis.Gameplay
{
    enum AnimationPhase
    {
        STARTUP,
        ACTIVE,
        INACTIVE
    }

    [RequireComponent(typeof(Animator))]
    public class AnimationBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Movement.GridMovementBehaviour _moveBehaviour;
        [SerializeField]
        private Animator _animator;
        private Ability _currentAbilityAnimating;
        private AnimationClip _currentClip;
        [SerializeField]
        private RuntimeAnimatorController _runtimeAnimator;
        private PlayableGraph _playableGraph;
        private AnimationPlayableOutput _output;
        private AnimationClipPlayable _currentClipPlayable;
        private int _animationPhase;
        private bool _animatingMotion;

        // Start is called before the first frame update
        void Start()
        {
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_playableGraph, "OutPose", _animator);
        }

        /// <summary>
        /// Switches to the next animation phase
        /// </summary>
        private void IncrementAnimationPhase()
        {
            _animationPhase++;
            CalculateAnimationSpeed();
        }

        /// <summary>
        /// Changes the speed of the animation based on the ability data
        /// </summary>
        private void CalculateAnimationSpeed()
        {
            AnimationPhase phase = (AnimationPhase)_animationPhase;
            double newSpeed = 1;

            switch (phase)
            {
                case AnimationPhase.STARTUP:
                    if (_currentAbilityAnimating.abilityData.startUpTime <= 0)
                    {
                        _currentClipPlayable.SetTime(_currentClip.events[1].time);
                        break;
                    }
                    newSpeed = (_currentClip.events[0].time / _currentAbilityAnimating.abilityData.startUpTime);
                    break;
                case AnimationPhase.ACTIVE:
                    newSpeed = (_currentClip.events[1].time - _currentClip.events[0].time) / _currentAbilityAnimating.abilityData.timeActive;
                    break;
                case AnimationPhase.INACTIVE:
                    newSpeed = (_currentClip.length - _currentClip.events[1].time) / _currentAbilityAnimating.abilityData.recoverTime;
                    break;
            }

            _currentClipPlayable.SetSpeed(newSpeed);
        }

        /// <summary>
        /// Plays the animation attached to this ability.
        /// </summary>
        /// <param name="ability"></param>
        public void PlayAbilityAnimation(Ability ability)
        {
            _currentAbilityAnimating = ability;

            if (!_currentAbilityAnimating.abilityData.GetCustomAnimation(out _currentClip))
                return;
            
            _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _currentClip);

            _output.SetSourcePlayable(_currentClipPlayable);

            _playableGraph.Play();
            _animatingMotion = false;
            _animationPhase = 0;
            CalculateAnimationSpeed();
        }

        // Update is called once per frame
        void Update()
        {
            if (_currentClipPlayable.IsValid())
            {
                if (_currentClipPlayable.IsDone() && !_animatingMotion)
                {
                    _playableGraph.Stop();
                    _animator.Rebind();
                    _animatingMotion = true;
                }
            }

            _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x);
            _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);
            Debug.Log(_animator.speed);
        }
    }
}
