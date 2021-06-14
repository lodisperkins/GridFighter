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

        // Start is called before the first frame update
        void Start()
        {
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_playableGraph, "OutPose", _animator);
        }

        private void CalculateAnimationSpeed(int newPhase)
        {
            AnimationPhase phase = (AnimationPhase)newPhase;
            double newTime = 1;

            switch (phase)
            {
                case AnimationPhase.STARTUP:
                    if (_currentAbilityAnimating.abilityData.startUpTime <= 0)
                    {
                        _currentClipPlayable.SetTime(_currentClip.events[1].time);
                        break;
                    }

                    newTime = (_currentClipPlayable.GetSpeed() * _currentAbilityAnimating.abilityData.startUpTime) / _currentClip.events[0].time;
                    break;
                case AnimationPhase.ACTIVE:
                    newTime = (_currentClipPlayable.GetSpeed() * _currentAbilityAnimating.abilityData.timeActive) / _currentClip.events[1].time - _currentClip.events[0].time;
                    break;
                case AnimationPhase.INACTIVE:
                    newTime = (_currentClipPlayable.GetSpeed() * _currentAbilityAnimating.abilityData.recoverTime) / _currentClip.length - _currentClip.events[1].time;
                    break;
            }

            _currentClipPlayable.SetSpeed(newTime);
        }

        public void PlayAbilityAnimation(Ability ability)
        {
            _currentAbilityAnimating = ability;

            if (!_currentAbilityAnimating.abilityData.GetCustomAnimation(out _currentClip))
                return;
            
            _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _currentClip);

            _output.SetSourcePlayable(_currentClipPlayable);

            _playableGraph.Play();

            CalculateAnimationSpeed((int)AnimationPhase.STARTUP);
        }

        // Update is called once per frame
        void Update()
        {
            _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x);
            _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);
            Debug.Log(_animator.speed);
        }
    }
}
