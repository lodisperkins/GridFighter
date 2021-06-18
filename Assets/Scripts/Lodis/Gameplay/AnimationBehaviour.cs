using Lodis.ScriptableObjects;
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
            CalculateCustomAnimationSpeed();
        }

        /// <summary>
        /// Changes the speed of the animation based on the ability data
        /// </summary>
        private void CalculateCustomAnimationSpeed()
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

        bool FindAnimationClip(string name)
        {
            foreach (AnimationClip animationClip in _runtimeAnimator.animationClips)
                if(animationClip.name == name)
                {
                    _currentClip = animationClip;
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Plays the animation attached to this ability.
        /// </summary>
        /// <param name="ability"></param>
        public void PlayAbilityAnimation(Ability ability)
        {
            _currentAbilityAnimating = ability;

            switch (_currentAbilityAnimating.abilityData.animationType)
            {
                case AnimationType.CAST:
                    if (FindAnimationClip("Cast"))
                    {
                        _animator.SetTrigger("Cast");
                    }
                    else
                    {
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Cast clip");
                    }
                    break;
                case AnimationType.MELEE:
                    if (FindAnimationClip("Melee"))
                    {
                        _animator.SetTrigger("Melee");
                    }
                    else
                    {
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Melee clip");
                    }
                    break;
                case AnimationType.SUMMON:
                    if (FindAnimationClip("Summon"))
                    {
                        _animator.SetTrigger("Summon");
                    }
                    else
                    {
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Summon clip");
                    }
                    break;

                case AnimationType.CUSTOM:
                    if (!_currentAbilityAnimating.abilityData.GetCustomAnimation(out _currentClip))
                        return;

                    _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _currentClip);

                    _output.SetSourcePlayable(_currentClipPlayable);

                    _playableGraph.Play();
                    _animatingMotion = false;
                    _animationPhase = 0;

                    CalculateCustomAnimationSpeed();
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_currentAbilityAnimating != null)
            {
                if (!_currentAbilityAnimating.InUse && !_animatingMotion)
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
