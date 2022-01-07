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
        private Vector2 _normal;
        private Vector3 _modelRestPosition;
        public Coroutine AbilityAnimationRoutine;
        private AnimatorOverrideController _overrideController;
        private string _previousState;
        private int _previousNameHash;

        // Start is called before the first frame update
        void Start()
        {
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT);
            _characterStateMachine = _characterStateManager.StateMachine;
            _knockbackBehaviour.AddOnTakeDamageAction(PlayDamageAnimation);
            _moveBehaviour.AddOnMoveBeginAction(PlayMovementAnimation);
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
        public IEnumerator PlayAbilityAnimation(Ability ability)
        {
            StopCurrentAnimation();
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
                        _overrideController["Cast"] = _runtimeController.animationClips[0];

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
                    _overrideController["Cast"] = _currentClip;

                    //Play custom clip with appropriate speed
                    _animator.Play("Cast", 0, 0);
                    _animatingMotion = false;
                    _animationPhase = 0;
                    CalculateAbilityAnimationSpeed();
                    break;
            }
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
            _animatingMotion = true;
            _animationPhase = 0;

            _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.x);
            _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.y);

            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Movement"))
                _animator.Play("Movement", 0, 0);
            else
                _animator.SetTrigger("Movement");
        }
      
        public void PlayGroundRecoveryAnimation()
        {
            _animator.SetTrigger("GroundRecovery");
            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.KnockDownRecoverTime);
        }

        public void PlayHardLandingAnimation()
        {
            _animator.SetTrigger("HardLanding");
            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.KnockDownLandingTime);
        }

        public void PlaySoftLandingAnimation()
        {
            _animator.SetTrigger("SoftLanding");
            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.LandingTime);
        }

        public void PlayDamageAnimation()
        {

            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _knockbackBehaviour.TimeInCurrentHitStun);
            if (_knockbackBehaviour.Physics.IsGrounded)
                _animator.SetTrigger("GroundedFlinching");
            else
                _animator.SetTrigger("InAirFlinching");
            _animatingMotion = true;
        }

        /// <summary>
        /// Plays the animation to break a fall based on the 
        /// direction the structure is to the character
        /// </summary>
        public void PlayFallBreakAnimation()
        {
            float x = Mathf.Abs(_normal.x);
            float y = Mathf.Abs(_normal.y);

            _animator.SetFloat("AnimationSpeedScale", _animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / _defenseBehaviour.FallBreakLength);

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

        private void Update()
        {
            if (_characterStateManager.StateMachine.CurrentState != "Attacking" && AbilityAnimationRoutine != null)
            {
                StopCoroutine(AbilityAnimationRoutine);
                AbilityAnimationRoutine = null;
            }
            //GetCurrentAnimationClip();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (_moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT)
                _animator.SetBool("OnRightSide", true);

            Debug.Log(_animator.speed);
        }
    }
}
