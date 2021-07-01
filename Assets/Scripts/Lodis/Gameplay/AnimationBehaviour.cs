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
        private Movement.KnockbackBehaviour _knockbackBehaviour;
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
        [SerializeField]
        private PlayerStateManagerBehaviour _playerStateManager;

        // Start is called before the first frame update
        void Start()
        {
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_playableGraph, "OutPose", _animator);
            _knockbackBehaviour.AddOnKnockBackAction(ResetAnimationGraph);
        }

        /// <summary>
        /// Switches to the next animation phase
        /// </summary>
        private void IncrementAnimationPhase()
        {
            _animationPhase++;
            if (_currentAbilityAnimating.abilityData.animationType == AnimationType.CUSTOM)
                CalculateCustomAnimationSpeed();
            else
                CalculateAnimatorSpeed();
        }

        /// <summary>
        /// Changes the speed of the animation based on the ability data
        /// </summary>
        private void CalculateAnimatorSpeed()
        {
            if (!_currentAbilityAnimating.abilityData.useAbilityTimingForAnimation)
                return;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            float newSpeed = 1;

            switch (phase)
            {
                case AnimationPhase.STARTUP:
                    if (_currentAbilityAnimating.abilityData.startUpTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.events[0].time;
                        break;
                    }
                    newSpeed = (_currentClip.events[0].time / _currentAbilityAnimating.abilityData.startUpTime);
                    break;
                case AnimationPhase.ACTIVE:
                    if (_currentAbilityAnimating.abilityData.timeActive <= 0)
                    {
                        _animator.playbackTime = _currentClip.events[1].time;
                        break;
                    }
                    newSpeed = (_currentClip.events[1].time - _currentClip.events[0].time) / _currentAbilityAnimating.abilityData.timeActive;
                    break;
                case AnimationPhase.INACTIVE:
                    if (_currentAbilityAnimating.abilityData.recoverTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.length;
                        break;
                    }
                    newSpeed = (_currentClip.length - _currentClip.events[1].time) / _currentAbilityAnimating.abilityData.recoverTime;
                    break;
            }

            _animator.speed = newSpeed;
        }

        /// <summary>
        /// Changes the speed of the animation based on the ability data
        /// </summary>
        private void CalculateCustomAnimationSpeed()
        {
            if (!_currentAbilityAnimating.abilityData.useAbilityTimingForAnimation)
                return;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            double newSpeed = 1;

            switch (phase)
            {
                case AnimationPhase.STARTUP:
                    if (_currentAbilityAnimating.abilityData.startUpTime <= 0)
                    {
                        _currentClipPlayable.SetTime(_currentClip.events[0].time);
                        break;
                    }
                    newSpeed = (_currentClip.events[0].time / _currentAbilityAnimating.abilityData.startUpTime);
                    break;
                case AnimationPhase.ACTIVE:
                    if (_currentAbilityAnimating.abilityData.timeActive <= 0)
                    {
                        _currentClipPlayable.SetTime(_currentClip.events[1].time);
                        break;
                    }
                    newSpeed = (_currentClip.events[1].time - _currentClip.events[0].time) / _currentAbilityAnimating.abilityData.timeActive;
                    break;
                case AnimationPhase.INACTIVE:
                    if (_currentAbilityAnimating.abilityData.recoverTime <= 0)
                    {
                        _currentClipPlayable.SetTime(_currentClipPlayable.GetDuration());
                        break;
                    }
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
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        _animator.Play("Cast", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAnimatorSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Cast clip for " + ability.abilityData.name);
                    break;

                case AnimationType.MELEE:
                    if (FindAnimationClip("Melee"))
                    {
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        _animator.Play("Melee", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAnimatorSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Melee animation. Couldn't find the Melee clip for " + ability.abilityData.name);
                    break;

                case AnimationType.SUMMON:
                    if (FindAnimationClip("Summon"))
                    {
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        _animator.Play("Summon", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAnimatorSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Summon animation. Couldn't find the Summon clip for " + ability.abilityData.name);
                    break;

                case AnimationType.CUSTOM:
                    if (!_currentAbilityAnimating.abilityData.GetCustomAnimation(out _currentClip))
                    {
                        Debug.LogError("Can't play custom clip. No custom clip found for " + ability.abilityData.name);
                        return;
                    }

                    _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _currentClip);

                    _output.SetSourcePlayable(_currentClipPlayable);

                    _playableGraph.Play();
                    _animatingMotion = false;
                    _animationPhase = 0;

                    CalculateCustomAnimationSpeed();
                    break;
            }
        }

        private void ResetAnimationGraph()
        {
            _playableGraph.Stop();
            _animator.Rebind();
            _animator.speed = 1;
        }

        /// <summary>
        /// Updates the type of animations playing based on the characters state
        /// </summary>
        private void UpdateAnimationsBasedOnState()
        {
            if (_currentAbilityAnimating != null)
            {
                if (!_currentAbilityAnimating.InUse && !_animatingMotion)
                {
                    ResetAnimationGraph();
                }
            }

            switch (_playerStateManager.CurrentState)
            {
                case PlayerState.IDLE:
                    _animator.Play("IdleRun");
                    _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x);
                    _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);
                    _animatingMotion = true;
                    break;

                case PlayerState.KNOCKBACK:
                    _animator.Play("Tumbling");
                    _animatingMotion = true;
                    break;

                case PlayerState.FREEFALL:
                    _animator.Play("FreeFall");
                    _animatingMotion = true;
                    break;

                case PlayerState.PARRYING:
                    ResetAnimationGraph();
                    _animator.Play("ParryPose");
                    _animatingMotion = true;
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            UpdateAnimationsBasedOnState();
            //Debug.Log(_animator.speed);
        }
    }
}
