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
    public class CharacterAnimationBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Movement.GridMovementBehaviour _moveBehaviour;
        [SerializeField]
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        [SerializeField]
        private CharacterDefenseBehaviour _defenseBehaviour;
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
        [SerializeField]
        private float _moveAnimationStartUpTime;
        [SerializeField]
        private float _moveAnimationRecoverTime;
        private Vector2 _normal;
        private Vector3 _modelRestPosition;
        public Coroutine abilityAnimationRoutine;

        // Start is called before the first frame update
        void Start()
        {
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_playableGraph, "OutPose", _animator);
            _knockbackBehaviour.AddOnKnockBackAction(ResetAnimationGraph);
            _defenseBehaviour.onFallBroken += normal => _normal = normal;
            _modelRestPosition = _animator.transform.localPosition;
            //_moveBehaviour.AddOnMoveEndAction(() => _animator.speed = 1);
        }

        /// <summary>
        /// Switches to the next animation phase
        /// </summary>
        private void IncrementAnimationPhase()
        {
            _animationPhase++;

            if (_animatingMotion)
                CalculateMovementAnimationSpeed();
            else if (_currentAbilityAnimating.abilityData.animationType == AnimationType.CUSTOM)
                CalculateCustomAnimationSpeed();
            else
                CalculateAbilityAnimationSpeed();
        }

        /// <summary>
        /// Changes the speed of the animation based on the ability data
        /// </summary>
        private void CalculateAbilityAnimationSpeed()
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
        private void CalculateMovementAnimationSpeed()
        {
            AnimationPhase phase = (AnimationPhase)_animationPhase;
            float newSpeed = 1;

            switch (phase)
            {
                case AnimationPhase.STARTUP:
                    if (_moveAnimationStartUpTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.events[0].time;
                        break;
                    }
                    newSpeed = (_currentClip.events[0].time / _moveAnimationStartUpTime);
                    break;
                case AnimationPhase.ACTIVE:
                    Vector2 oldPosition = new Vector2();

                    if (_moveBehaviour.PreviousPanel)
                        oldPosition = _moveBehaviour.PreviousPanel.Position;

                    float travelDistance = (oldPosition - _moveBehaviour.CurrentPanel.Position).magnitude;
                    float travelTime = travelDistance / _moveBehaviour.Speed;

                    if (travelTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.events[1].time;
                        break;
                    }
                    newSpeed = (_currentClip.events[1].time - _currentClip.events[0].time) / travelTime;
                    break;
                case AnimationPhase.INACTIVE:
                    if (_moveAnimationRecoverTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.length;
                        break;
                    }
                    newSpeed = (_currentClip.length - _currentClip.events[1].time) / _moveAnimationRecoverTime;
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

        bool SetCurrentAnimationClip(string name)
        {
            foreach (AnimationClip animationClip in _runtimeAnimator.animationClips)
                if (animationClip.name.Contains(name))
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
        public IEnumerator PlayAbilityAnimation(Ability ability)
        {
            _currentAbilityAnimating = ability;

            switch (_currentAbilityAnimating.abilityData.animationType)
            {
                case AnimationType.CAST:
                    if (SetCurrentAnimationClip("Cast"))
                    {
                        yield return new WaitUntil(() => ability.CanPlayAnimation);

                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        _animator.Play("Cast", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAbilityAnimationSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Cast clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.MELEE:
                    if (SetCurrentAnimationClip("Melee"))
                    {
                        yield return new WaitUntil(() => ability.CanPlayAnimation);

                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        _animator.Play("Melee", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAbilityAnimationSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Melee animation. Couldn't find the Melee clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.SUMMON:
                    if (SetCurrentAnimationClip("Summon"))
                    {
                        yield return new WaitUntil(() => ability.CanPlayAnimation);
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        _animator.Play("Summon", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAbilityAnimationSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Summon animation. Couldn't find the Summon clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.CUSTOM:
                    if (!_currentAbilityAnimating.abilityData.GetCustomAnimation(out _currentClip))
                    {
                        Debug.LogError("Can't play custom clip. No custom clip found for " + ability.abilityData.abilityName);
                        yield return null;
                    }


                    yield return new WaitUntil(() => ability.CanPlayAnimation);

                    _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _currentClip);

                    _output.SetSourcePlayable(_currentClipPlayable);

                    _playableGraph.Play();
                    _animatingMotion = false;
                    _animationPhase = 0;

                    CalculateCustomAnimationSpeed();
                    break;
            }
        }

        public void StopCurrentAnimation()
        {
            _animator.StopPlayback();
            _playableGraph.Stop();
        }

        private void PlayMovementAnimation()
        {
            _animatingMotion = true;
            _animationPhase = 0;

            if (_playableGraph.IsPlaying())
                    _playableGraph.Stop();

            if (_moveBehaviour.MoveDirection.x > 0 && SetCurrentAnimationClip("DashForward"))
                _animator.Play("DashForward");
            else if (_moveBehaviour.MoveDirection.x < 0 && SetCurrentAnimationClip("DashBackward"))
                _animator.Play("DashBackward");
            else if (_moveBehaviour.MoveDirection.y > 0 && SetCurrentAnimationClip("DashRight"))
                _animator.Play("DashRight");
            else if (_moveBehaviour.MoveDirection.y < 0 && SetCurrentAnimationClip("DashLeft"))
                _animator.Play("DashLeft");

            CalculateMovementAnimationSpeed();
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

            if (_playerStateManager.CurrentState != PlayerState.ATTACKING && abilityAnimationRoutine != null)
            {
                StopCoroutine(abilityAnimationRoutine);
                abilityAnimationRoutine = null;
            }

            switch (_playerStateManager.CurrentState)
            {
                case PlayerState.IDLE:
                    _animator.Play("Idle");
                    _animatingMotion = true;
                    _animator.transform.localPosition = _modelRestPosition;
                    _animator.speed = 1;
                    break;

                case PlayerState.MOVING:
                    PlayMovementAnimation();
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

                case PlayerState.FALLBREAKING:
                    PlayFallBreakAnimation();
                    _animatingMotion = true;
                    break;

                case PlayerState.LANDING:
                    _animator.transform.localPosition = Vector3.zero;
                    _animator.Play("Land");
                    _animatingMotion = true;
                    break;
            }
        }

        private void PlayFallBreakAnimation()
        {
            float x = Mathf.Abs(_normal.x);
            float y = Mathf.Abs(_normal.y);

            if (x > y)
            {
                _animator.Play("WallTech");
                _animatingMotion = true;
            }
            else if (y > x)
            {
                _animator.Play("GroundTech");
                _animatingMotion = true;
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
