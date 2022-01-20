using Ilumisoft.VisualStateMachine;
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
        private MovesetBehaviour _movesetBehaviour;
        [SerializeField]
        private Animator _animator;
        private Ability _currentAbilityAnimating;
        private AnimationClip _currentClip;
        [SerializeField]
        private RuntimeAnimatorController _runtimeController;
        private int _animationPhase;
        private bool _animatingMotion;
        [SerializeField]
        private CharacterStateMachineBehaviour _characterStateManager;
        private StateMachine _characterStateMachine;
        [Tooltip("THe amount of time it takes the character to get into the move pose")]
        [SerializeField]
        private float _moveAnimationStartUpTime;
        [Tooltip("THe amount of time it takes the character to exit the move pose")]
        [SerializeField]
        private float _moveAnimationRecoverTime;
        [SerializeField]
        private float _flinchStartUpTime;
        [SerializeField]
        private AnimationClip _defaultCastAnimation;
        [SerializeField]
        private AnimationClip _defaultSummonAnimation;
        [SerializeField]
        private AnimationClip _defaultMeleeAnimation;
        private AnimatorTransitionInfo _lastTransitionInfo;
        private Vector2 _normal;
        public Coroutine AbilityAnimationRoutine;
        private AnimatorOverrideController _overrideController;
        private float _currentClipStartUpTime;
        private float _currentClipActiveTime;
        private float _currentClipRecoverTime;

        // Start is called before the first frame update
        void Start()
        {
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT);
            _characterStateMachine = _characterStateManager.StateMachine;
            _knockbackBehaviour.AddOnTakeDamageAction(PlayDamageAnimation);

            _moveBehaviour.AddOnMoveBeginAction
                (
                    () =>
                    {

                        _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x);
                        _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);
                    }
                );

            _movesetBehaviour.OnUseAbility += PlayAbilityAnimation;
            _defenseBehaviour.onFallBroken += normal => _normal = normal;
            //_animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        /// <summary>
        /// Switches to the next animation phase and calculates the new speed for animations
        /// </summary>
        private void IncrementAnimationPhase()
        {
            _animationPhase++;

            CalculateAnimationSpeed();
        }

        private int GetNextIncrementAnimationPhaseEvent(float currentAnimationTime = 0)
        {
            int eventIndex = 0;

            for (int i = eventIndex; i < _currentClip.events.Length; i++)
            {
                if (_currentClip.events[i].functionName == "IncrementAnimationPhase" && currentAnimationTime == 0)
                    break;
                else if (_currentClip.events[i].functionName != "IncrementAnimationPhase" || Mathf.Abs(currentAnimationTime - _currentClip.events[i].time) > 0.05f)
                    eventIndex++;
                else
                    break;
            }

            return eventIndex;
        }

        private int GetNextIncrementAnimationPhaseEvent(int eventIndex)
        {
            eventIndex++;

            for (int i = eventIndex; i < _currentClip.events.Length; i++)
            {
                if (_currentClip.events[i].functionName != "IncrementAnimationPhase")
                    eventIndex++;
                else
                    break;
            }

            return eventIndex;
        }

        public void CalculateAnimationSpeed()
        {
            
            AnimatorStateInfo stateInfo;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            float newSpeed = 1;
            int eventIndex = 0;
            


            ///Calculates the new animation speed based on the current animation phase.
            ///If the phases time for animating is 0, the animator is set to the next phase of the animation.
            ///Otherwise, the new speed is calculated by dividing the current time it takes to get to the next phase, by the
            /// desired amount of time the animator should take be in that phase.
            switch (phase)
            {
                case AnimationPhase.STARTUP:

                    if (_animator.GetNextAnimatorClipInfo(0).Length <= 0)
                        return;

                    _currentClip = _animator.GetNextAnimatorClipInfo(0)[0].clip;
                    //Return if this clip couldn't be found or if it doesn't have animation events
                    if (!_currentClip || _currentClip.events.Length <= 0)
                        return;


                    stateInfo = _animator.GetNextAnimatorStateInfo(0);

                    if (_currentClipStartUpTime <= 0)
                    {
                        _animator.Play(stateInfo.shortNameHash, 0, _currentClip.events[0].time);
                        break;
                    }

                    eventIndex = GetNextIncrementAnimationPhaseEvent(_currentClip.length * (stateInfo.normalizedTime % 1));

                    if (eventIndex < 0 || eventIndex >= _currentClip.events.Length)
                        break;
                    newSpeed = (_currentClip.events[eventIndex].time / _currentClipStartUpTime);
                    break;

                case AnimationPhase.ACTIVE:

                    if (_currentClipActiveTime <= 0 && _currentClip.events.Length >= 2)
                    {
                        _animator.playbackTime = _currentClip.events[1].time;
                        break;
                    }

                    stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                    float nextTimeStamp = _currentClip.length;
                    eventIndex = 0;

                    if (_currentClip.events.Length > 1)
                    {
                        eventIndex = GetNextIncrementAnimationPhaseEvent(_currentClip.length * (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1));
                        int nextEventIndex = GetNextIncrementAnimationPhaseEvent(eventIndex);

                        if (nextEventIndex < 0 || nextEventIndex >= _currentClip.events.Length)
                            break;

                        nextTimeStamp = _currentClip.events[nextEventIndex].time;
                    }

                    if (eventIndex < 0 || eventIndex >= _currentClip.events.Length)
                        break;

                    newSpeed = (nextTimeStamp - _currentClip.events[eventIndex].time) / _currentClipActiveTime;
                    break;
                case AnimationPhase.INACTIVE:

                    if (_currentClip.events.Length < 2)
                        break;
                    else if (_currentClipRecoverTime <= 0)
                    {
                        _animator.playbackTime = _currentClip.length;
                        break;
                    }

                    stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    eventIndex = GetNextIncrementAnimationPhaseEvent(eventIndex);

                    newSpeed = (_currentClip.length - _currentClip.events[eventIndex].time) / _currentClipRecoverTime;
                    break;
            }

            _animator.SetFloat("AnimationSpeedScale", newSpeed);
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

            _animator.SetFloat("AnimationSpeedScale", newSpeed);
        }

        /// <summary>
        /// Changes the speed of the animation based on the move startup and end values
        /// to make the movement animation a bit smoother
        /// </summary>
        private void CalculateMovementAnimationSpeed()
        {
            _currentClip = GetCurrentAnimationClip();

            if (_currentClip == null || !_animator.GetCurrentAnimatorStateInfo(0).IsName("Movement"))
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

            _animator.SetFloat("AnimationSpeedScale", newSpeed);
        }
        
        bool SetCurrentAnimationClip(string name)
        {
            foreach (AnimationClip animationClip in _runtimeController.animationClips)
                if (animationClip.name.Contains(name))
                {
                    _currentClip = animationClip;
                    return true;
                }

            return false;
        }

        AnimationClip GetCurrentAnimationClip()
        {
            List<AnimatorClipInfo> animatorClips = new List<AnimatorClipInfo>(_animator.GetCurrentAnimatorClipInfo(0));
            animatorClips.Sort(SortByWeight);

            if (animatorClips.Count <= 0)
                return null;

            return animatorClips[0].clip;

        }

        int SortByWeight(AnimatorClipInfo lhs, AnimatorClipInfo rhs)
        {
            return lhs.weight > rhs.weight? -1 : 1;
        }

        /// <summary>
        /// Starts the animation playback for this ability. 
        /// </summary>
        /// <param name="ability">The ability that the animation belongs to</param>
        public IEnumerator StartAbilityAnimationRoutine(Ability ability)
        {
            //StopCurrentAnimation();
            _currentAbilityAnimating = ability;
            _animator.SetFloat("AnimationSpeedScale", 1);
            _animationPhase = 0;
            ///Play animation based on type
            switch (_currentAbilityAnimating.abilityData.animationType)
            {
                case AnimationType.CAST:
                    //Set the clip for the animation graph attached
                    if (_defaultCastAnimation)
                    {
                        ///Wait until the ability is allowed to play the animation.
                        ///This is here in case the animation is activated manually
                        yield return new WaitUntil(() => ability.CanPlayAnimation);
                        _currentClip = _defaultCastAnimation;
                        _overrideController["Cast"] = _defaultCastAnimation;
                        _animatingMotion = false;
                        _animationPhase = 0;
                    }
                    else
                        Debug.LogError("Couldn't play Cast animation. Couldn't find the Cast clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.MELEE:
                    //Set the clip for the animation graph attached
                    if (_defaultMeleeAnimation)
                    {
                        ///Wait until the ability is allowed to play the animation.
                        ///This is here in case the animation is activated manually
                        yield return new WaitUntil(() => ability.CanPlayAnimation);
                        _currentClip = _defaultMeleeAnimation;

                        _overrideController["Cast"] = _defaultMeleeAnimation;
                        _animatingMotion = false;
                        _animationPhase = 0;
                    }
                    else
                        Debug.LogError("Couldn't play Melee animation. Couldn't find the Melee clip for " + ability.abilityData.abilityName);
                    break;

                case AnimationType.SUMMON:
                    //Set the clip for the animation graph attached
                    if (_defaultSummonAnimation)
                    {
                        ///Wait until the ability is allowed to play the animation.
                        ///This is here in case the animation is activated manually
                        yield return new WaitUntil(() => ability.CanPlayAnimation);

                        _currentClip = _defaultSummonAnimation;
                        _overrideController["Cast"] = _defaultSummonAnimation;
                        _animatingMotion = false;
                        _animationPhase = 0;
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
                    _overrideController["Cast"] = _currentClip;

                    //Play custom clip with appropriate speed
                    _animatingMotion = false;
                    _animationPhase = 0;
                    break;
            }

            if (ability.abilityData.useAbilityTimingForAnimation)
            {
                _currentClipStartUpTime = ability.abilityData.startUpTime;
                _currentClipActiveTime = ability.abilityData.timeActive;
                _currentClipRecoverTime = ability.abilityData.recoverTime;
            }
            else
                _animationPhase = 3;

            _animator.SetTrigger("Attack");

            //if (_currentClip.events[0] != null)
            //    if (_currentClip.events[0].functionName == "CalculateAnimationSpeed")
            //        yield break;

            //AnimationEvent animationEvent = new AnimationEvent();
            //animationEvent.time = 0;
            //animationEvent.functionName = "CalculateAnimationSpeed";
            //_currentClip.AddEvent(animationEvent);
        }

        public void PlayAbilityAnimation()
        {
            StartCoroutine(StartAbilityAnimationRoutine(_movesetBehaviour.LastAbilityInUse));
        }

        /// <summary>
        /// Stops the animator and playable graph from playing the current animation
        /// </summary>
        public void StopCurrentAnimation()
        {
            _animator.StopPlayback();
            _overrideController["Cast"] = _runtimeController.animationClips[0];
            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT);
        }

        /// <summary>
        /// Plays the appropriate move clip based on the move direction
        /// </summary>
        public void PlayMovementAnimation()
        {
            _animator.SetFloat("AnimationSpeedScale", 1);
            _animatingMotion = true;
            _animationPhase = 0;

            _currentClipStartUpTime = _moveAnimationStartUpTime;

            //Calculates the time it takes to get to the destination
            Vector2 oldPosition = new Vector2();

            if (_moveBehaviour.PreviousPanel)
                oldPosition = _moveBehaviour.PreviousPanel.Position;

            float travelDistance = (oldPosition - _moveBehaviour.CurrentPanel.Position).magnitude;
            float travelTime = travelDistance / _moveBehaviour.Speed;

            _currentClipActiveTime = travelTime;
            _currentClipRecoverTime = _moveAnimationRecoverTime;

            int mirror = 1;

            if (_moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT) mirror = -1;

            _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x * mirror);
            _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);

            _animator.SetTrigger("Movement");
        }
      
        public void PlayGroundRecoveryAnimation()
        {
            _animationPhase = 0;
            _animator.SetTrigger("GroundRecovery");
            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.KnockDownRecoverTime);
        }

        public void PlayHardLandingAnimation()
        {
            _animationPhase = 0;
            _animator.SetTrigger("HardLanding");
            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.KnockDownLandingTime);
        }

        public void PlaySoftLandingAnimation()
        {
            _animationPhase = 0;
            _animator.SetTrigger("SoftLanding");
            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.LandingTime);
        }

        public void PlayDamageAnimation()
        {
            _animationPhase = 0;

            _currentClipStartUpTime = _flinchStartUpTime;

            if (_knockbackBehaviour.Physics.IsGrounded)
                _animator.SetTrigger("GroundedFlinching");
            else
                _animator.SetTrigger("InAirFlinching");

            AnimatorStateInfo nextState = _animator.GetNextAnimatorStateInfo(0);
            AnimationClip clip = GetCurrentAnimationClip();
            _currentClipActiveTime = _knockbackBehaviour.TimeInCurrentHitStun - _flinchStartUpTime;

            _animatingMotion = true;
        }

        public void UpdateInAirMoveDirection()
        {
            _animator.SetFloat("MoveDirectionInAirY", _knockbackBehaviour.Physics.LastVelocity.normalized.y);
        }

        /// <summary>
        /// Plays the animation to break a fall based on the 
        /// direction the structure is to the character
        /// </summary>
        public void PlayFallBreakAnimation()
        {
            _animationPhase = 0;
            float x = Mathf.Abs(_normal.x);
            float y = Mathf.Abs(_normal.y);

            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _defenseBehaviour.FallBreakLength);

            if (x > y)
            {
                _animator.SetTrigger("WallTech");
                _animatingMotion = true;
            }
            else if (y > x)
            {
                _animator.SetTrigger("GroundTech");
                _animatingMotion = true;
            }
        }

        private void Update()
        {
            if (_characterStateManager.StateMachine.CurrentState != "Attacking" && AbilityAnimationRoutine != null)
            {
                StopCoroutine(AbilityAnimationRoutine);
                AbilityAnimationRoutine = null;
            }
            AnimatorTransitionInfo currentInfo = _animator.GetAnimatorTransitionInfo(0);
            if (_lastTransitionInfo.nameHash != currentInfo.nameHash && currentInfo.nameHash != 0)
                _lastTransitionInfo = _animator.GetAnimatorTransitionInfo(0);

            if (!_knockbackBehaviour.Physics.IsGrounded)
                UpdateInAirMoveDirection();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (_moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT)
                _animator.SetBool("OnRightSide", true);


        }
    }
}
