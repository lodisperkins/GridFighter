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
        [Tooltip("The move behaviour attached to the owner. Used to update movement animations")]
        [SerializeField]
        private Movement.GridMovementBehaviour _moveBehaviour;
        [Tooltip("The knock back behaviour attached to the owner. Used to update hit and freefall animations")]
        [SerializeField]
        private Movement.KnockbackBehaviour _knockbackBehaviour;
        [Tooltip("The defense behaviour attached to the owner. Used to update parry and tech animations")]
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
        [Tooltip("THe amount of time it takes the character to get into the move pose")]
        [SerializeField]
        private float _moveAnimationStartUpTime;
        [Tooltip("THe amount of time it takes the character to exit the move pose")]
        [SerializeField]
        private float _moveAnimationRecoverTime;
        private Vector2 _normal;
        private Vector3 _modelRestPosition;
        public Coroutine AbilityAnimationRoutine;

        // Start is called before the first frame update
        void Start()
        {
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _output = AnimationPlayableOutput.Create(_playableGraph, "OutPose", _animator);
            _knockbackBehaviour.AddOnKnockBackAction(ResetAnimationGraph);
            _defenseBehaviour.onFallBroken += normal => _normal = normal;
            _modelRestPosition = _animator.transform.localPosition;
        }

        /// <summary>
        /// Switches to the next animation phase and calculates the new speed for animations
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
        /// Changes the speed of the animation based on the ability data.
        /// Uses the ability animation already placed on the animation graph.
        /// </summary>
        private void CalculateAbilityAnimationSpeed()
        {
            //Return if this ability has a fixed time for the animation
            if (!_currentAbilityAnimating.abilityData.useAbilityTimingForAnimation)
                return;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            float newSpeed = 1;

            ///Calculates the new animation speed based on the current ability phase.
            ///If the phases time for animating is 0, the animator is set to the next phase of the animation.
            ///Otherwise, the new speed is calculated by dividing the current time it takes to get to the next phase, by the
            /// desired amount of time the animator should take be in that phase.
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
        /// Changes the speed of the animation based on the move startup and end values
        /// to make the movement animation a bit smoother
        /// </summary>
        private void CalculateMovementAnimationSpeed()
        {
            if (_currentClip == null)
                return;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            float newSpeed = 1;

            switch (phase)
            {
                ///Changes speed of the animation by dividing the time remaining in this phase by the time the startuo should be
                case AnimationPhase.STARTUP:
                    if (_moveAnimationStartUpTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.events[0].time;
                        break;
                    }
                    newSpeed = (_currentClip.events[0].time / _moveAnimationStartUpTime);
                    break;

                ///Changes speed of the animation by dividing the time remaining in this phase by the time
                ///it takes to travel to the destination
                case AnimationPhase.ACTIVE:

                    //Calculates the time it takes to get to the destination
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
                ///Changes speed of the animation by dividing the time remaining in this phase by the time the recover time should be
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
            //Return if this ability has a fixed time for the animation
            if (!_currentAbilityAnimating.abilityData.useAbilityTimingForAnimation)
                return;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            double newSpeed = 1;

            ///Calculates the new animation speed based on the current ability phase.
            ///If the phases time for animating is 0, the current clip is set to the next phase of the animation.
            ///Otherwise, the new speed is calculated by dividing the current time it takes to get to the next phase, by the
            ///desired amount of time the current clip should take be in that phase.
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
        /// Starts the animation playback for this ability. 
        /// </summary>
        /// <param name="ability">The ability that the animation belongs to</param>
        public IEnumerator PlayAbilityAnimation(Ability ability)
        {
            _currentAbilityAnimating = ability;
            _animator.speed = 1;

            ///Play animation based on type
            switch (_currentAbilityAnimating.abilityData.animationType)
            {
                case AnimationType.CAST:
                    //Set the clip for the animation graph attached
                    if (SetCurrentAnimationClip("Cast"))
                    {
                        ///Wait until the ability is allowed to play the animation.
                        ///This is here in case the animation is activated manually
                        yield return new WaitUntil(() => ability.CanPlayAnimation);

                        //Stop whatever animation is currently playing
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        //Start the animation with the appropriate speed
                        _animator.Play("Cast", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAbilityAnimationSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Cast clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.MELEE:
                    //Set the clip for the animation graph attached
                    if (SetCurrentAnimationClip("Melee"))
                    {
                        ///Wait until the ability is allowed to play the animation.
                        ///This is here in case the animation is activated manually
                        yield return new WaitUntil(() => ability.CanPlayAnimation);

                        //Stop whatever animation is currently playing
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        //Start the animation with the appropriate speed
                        _animator.Play("Melee", 0, 0);
                        _animatingMotion = false;
                        _animationPhase = 0;
                        CalculateAbilityAnimationSpeed();
                    }
                    else
                        Debug.LogError("Couldn't play Melee animation. Couldn't find the Melee clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.SUMMON:
                    //Set the clip for the animation graph attached
                    if (SetCurrentAnimationClip("Summon"))
                    {
                        ///Wait until the ability is allowed to play the animation.
                        ///This is here in case the animation is activated manually
                        yield return new WaitUntil(() => ability.CanPlayAnimation);

                        //Stop whatever animation is currently playing
                        if (_playableGraph.IsPlaying())
                            _playableGraph.Stop();

                        //Start the animation with the appropriate speed
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

                    ///Wait until the ability is allowed to play the animation.
                    ///This is here in case the animation is activated manually
                    yield return new WaitUntil(() => ability.CanPlayAnimation);

                    //Create a new anaimation clip and set the playable graphs current clip to it
                    _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _currentClip);
                    _output.SetSourcePlayable(_currentClipPlayable);

                    //Play custom clip with appropriate speed
                    _playableGraph.Play();
                    _animatingMotion = false;
                    _animationPhase = 0;
                    CalculateCustomAnimationSpeed();
                    break;
            }
        }

        /// <summary>
        /// Stops the animator and playable graph from playing the current animation
        /// </summary>
        public void StopCurrentAnimation()
        {
            _animator.StopPlayback();
            _playableGraph.Stop();
        }

        /// <summary>
        /// Plays the appropriate move clip based on the move direction
        /// </summary>
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

        /// <summary>
        /// Stop the playable graph from playing the current clip and
        /// reset its speed to the default
        /// </summary>
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

            if (_playerStateManager.CurrentState != PlayerState.ATTACKING && AbilityAnimationRoutine != null)
            {
                StopCoroutine(AbilityAnimationRoutine);
                AbilityAnimationRoutine = null;
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

                    if (_knockbackBehaviour.InHitStun)
                    {
                        _animator.SetTrigger("OnKnockBackLand");
                        _animator.speed = 1;
                    }
                    else
                    {
                        _animator.Play("Land");
                        _animator.speed = _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.LandingTime;
                    }

                    _animatingMotion = true;
                    break;

                case PlayerState.GROUNDRECOVERY:
                    _animator.SetTrigger("OnKnockDownGetUp");
                    _animator.speed = _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.KnockDownRecoverTime;
                    _animatingMotion = true;
                    break;

                case PlayerState.STUNNED:
                    _animator.Play("Stunned");
                    _animatingMotion = true;
                    break;
            }
        }

        /// <summary>
        /// Plays the animation to break a fall based on the 
        /// direction the structure is to the character
        /// </summary>
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
