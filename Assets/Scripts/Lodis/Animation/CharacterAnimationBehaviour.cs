using CustomEventSystem;
using FixedPoints;
using Ilumisoft.VisualStateMachine;
using Lodis.ScriptableObjects;
using Lodis.Sound;
using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    public struct CustomAnimationEvent
    {
        public CustomAnimationEvent(string eventName, UnityAction action)
        {
            EventName = eventName;
            Action = action;
        }

        public string EventName;
        public UnityAction Action;
    }

    enum AnimationPhase
    {
        STARTUP,
        ACTIVE,
        INACTIVE
    }

    // AI-generated note:
    // This enum supports rollback-safe animation restoration. A saved frame needs to know
    // whether the visible clip came from the controller directly, the attack override slot,
    // or the generic custom-action override slot so deserialization can reapply the same source
    // before jumping back into the saved animator state.
    enum AnimationOverrideType
    {
        NONE,
        ATTACK,
        CUSTOM_ACTION
    }

    [RequireComponent(typeof(Animator))]
    public class CharacterAnimationBehaviour : SimulationBehaviour
    {
        [Header("Action Behaviour References")]
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

        [Header("Animation References")]
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private RuntimeAnimatorController _runtimeController;
        [SerializeField]
        private CharacterStateMachineBehaviour _characterStateManager;
        [SerializeField] private Transform _characterMesh;

        [Header("Animation Settings")]
        [Tooltip("The amount of time it takes the character to get into the move pose")]
        [SerializeField]
        private Fixed32 _moveAnimationStartUpTime;
        [Tooltip("The amount of time it takes the character to exit the move pose")]
        [SerializeField]
        private Fixed32 _moveAnimationRecoverTime;
        [SerializeField]
        private Fixed32 _flinchStartUpTime;
        [SerializeField]
        private AnimationClip _defaultCastAnimation;
        [SerializeField]
        private AnimationClip _defaultSummonAnimation;
        [SerializeField]
        private AnimationClip _defaultMeleeAnimation;
        [SerializeField]
        private int _animationLayer = 0;
        [SerializeField]
        private Fixed32 _moveAnimationHangTime;

        [Header("Shuffle Settings")]
        [SerializeField]
        [Tooltip("How long it will take to start manually shuffling.")]
        private FloatVariable _manualShuffleStartTime;
        [SerializeField]
        [Tooltip("How long it will take to activate the manual shuffle.")]
        private FloatVariable _manualShuffleActiveTime;
        [SerializeField]
        [Tooltip("How long it will take to move again after shuffling.")]
        private FloatVariable _manualShuffleRecoverTime;

        //---
        private int _animationPhase;
        private bool _animatingMotion;
        private Ability _currentAbilityAnimating;
        private AnimationClip _currentClip;
        private StateMachine _characterStateMachine;
        private ConditionAction _bufferedAnimation;
        private AnimatorTransitionInfo _lastTransitionInfo;
        public Coroutine AbilityAnimationRoutine;
        private AnimatorOverrideController _overrideController;
        private Fixed32 _currentClipStartUpTime;
        private Fixed32 _currentClipActiveTime;
        private Fixed32 _currentClipRecoverTime;
        private bool _bracedAgainstFloor;
        private TimedAction _timedMoveAction;
        private bool _animatingAbility;
        private bool _airTravelLocked;
        private Vector2 _lockedAirDirection;
        private Fixed32 _targetSpeed = 1;
        private List<CustomAnimationEvent> _animationEvents = new List<CustomAnimationEvent>();
        private FixedConditionAction _winAnimCondition;
        private CharacterFeedbackBehaviour _characterFeedbackBehaviour;
        private AnimationOverrideType _currentOverrideType;
        private Fixed32 _savedPlaybackTime;
        private string _savedStateName = string.Empty;
        private AnimationOverrideType _savedOverrideType;
        private string _savedClipName = string.Empty;
        private bool _savedMirror;
        private bool _hasSavedPlaybackState;
        private bool _manuallyUpdatingAnimatorDuringResim;
        private bool _evaluatingAnimatorState;

        //Was thinking of maybe keeping track of the current character state machine and what the last animation played was through any playanimation func. Then just restoring the last thing played
     
        private Fixed32 _currentAnimationTime;

        public override string LogName => "CharacterAnimationBehaviour";

        // Start is called before the first frame update
        public override void Begin()
        {
            base.Begin();
            //_animator.enabled = false;

            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT);
            _characterStateMachine = _characterStateManager.StateMachine;
            _characterFeedbackBehaviour = GetComponent<CharacterFeedbackBehaviour>();

            _characterStateManager.AddOnStateChangedAction(state =>
            {
                if (state == "Attacking" || state == "Shuffling")
                    return;

                _targetSpeed = 1;
            });
            _knockbackBehaviour.AddOnTakeDamageAction(PlayDamageAnimation);

            _movesetBehaviour.OnUseAbility += () =>
            {
                if (!_movesetBehaviour.LastAbilityInUse.abilityData.playAnimationManually)
                    PlayAbilityAnimation();
            };

            MatchManagerBehaviour.Instance.AddOnMatchCountdownStartAction(() =>
            {
                FixedPointTimer.StopAction(_winAnimCondition);
                StopCurrentAnimation();

                if (SceneManagerBehaviour.Instance.CurrentGameMode != (int)GameMode.PRACTICE && SceneManagerBehaviour.Instance.CurrentGameMode != (int)GameMode.TUTORIAL)
                    FixedPointTimer.StartNewConditionAction(() => PlayState("Intro"), condition => _characterStateMachine.CurrentState == "Idle");
            });

            //Adding a delay here to the intro anim since the animator needs to be updated before the intro anim is played. Otherwise, the intro anim will not play.
            if (SceneManagerBehaviour.Instance.CurrentGameMode != (int)GameMode.PRACTICE && SceneManagerBehaviour.Instance.CurrentGameMode != (int)GameMode.TUTORIAL)
                FixedPointTimer.StartNewTimedAction(() => PlayState("Intro"), Fixed32.PointOne);

            MatchManagerBehaviour.Instance.AddOnMatchOverAction(() =>
            {
                if (!gameObject.activeInHierarchy || MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.DRAW || MatchManagerBehaviour.Instance.LastMatchResult == MatchResult.UNDECIDED)
                    return;

                if (SceneManagerBehaviour.Instance.CurrentGameMode != (int)GameMode.PRACTICE && SceneManagerBehaviour.Instance.CurrentGameMode != (int)GameMode.TUTORIAL)
                {
                    _winAnimCondition = FixedPointTimer.StartNewConditionAction(() =>
                    {
                        StopCurrentAnimation();
                        PlayState("Win");
                    }, condition => _characterStateMachine.CurrentState == "Idle");
                }
            });
        }

        public void ResetTargetSpeed()
        {
            _targetSpeed = 1;
            ApplyAnimatorSpeed();
        }

        /// <summary>
        /// Converts animator-facing float timing data into fixed-point so rollback math stays
        /// deterministic even though Unity's animator API still expects float inputs.
        /// </summary>
        private static Fixed32 ToFixedTime(float value)
        {
            return (Fixed32)value;
        }

        /// <summary>
        /// Returns the current clip length in fixed-point for deterministic speed calculations.
        /// </summary>
        private static Fixed32 GetClipLength(AnimationClip clip)
        {
            return clip ? ToFixedTime(clip.length) : 0;
        }

        /// <summary>
        /// Returns a clip event timestamp in fixed-point so animation phase math does not depend on float arithmetic.
        /// </summary>
        private static Fixed32 GetEventTime(AnimationEvent animationEvent)
        {
            return ToFixedTime(animationEvent.time);
        }

        /// <summary>
        /// Normalizes Unity's looping normalizedTime into a 0-1 range using fixed-point math.
        /// </summary>
        private static Fixed32 GetLoopedNormalizedTime(AnimatorStateInfo stateInfo)
        {
            Fixed32 normalizedTime = ToFixedTime(stateInfo.normalizedTime);
            return normalizedTime - Fixed32.FloorToInt(normalizedTime);
        }

        /// <summary>
        /// Applies the cached fixed-point playback speed to Unity's animator using an explicit float cast
        /// only at the engine boundary.
        /// </summary>
        private void ApplyAnimatorSpeed(bool respectPause = false)
        {
            Fixed32 speed = _targetSpeed * RoutineBehaviour.Instance.CharacterTimeScale;

            if (respectPause)
                speed *= Convert.ToInt32(!GridGame.IsPaused);

            _animator.speed = speed;
        }

        /// <summary>
        /// Jumps directly to a named animator state and evaluates it immediately instead of relying on trigger transitions.
        /// </summary>
        private void PlayState(string stateName, Fixed32 normalizedTime = default)
        {
            _savedStateName = stateName;
            _animator.Play(stateName, _animationLayer, normalizedTime);
            _evaluatingAnimatorState = true;

            try
            {
                _animator.Update(0f);
            }
            finally
            {
                _evaluatingAnimatorState = false;
            }
        }

        /// <summary>
        /// Jumps directly to a hashed animator state while still updating the saved debug state name.
        /// </summary>
        private void PlayState(int stateHash, string stateName, Fixed32 normalizedTime = default)
        {
            _savedStateName = stateName;
            _animator.Play(stateHash, _animationLayer, normalizedTime);
            _evaluatingAnimatorState = true;

            try
            {
                _animator.Update(0f);
            }
            finally
            {
                _evaluatingAnimatorState = false;
            }
        }

        /// <summary>
        /// Switches to the next animation phase and calculates the new speed for animations
        /// </summary>
        private void IncrementAnimationPhase()
        {
            _animationPhase++;

            CalculateAnimationSpeed();
        }

        private int GetNextIncrementAnimationPhaseEvent(Fixed32 currentAnimationTime = default)
        {
            int eventIndex = 0;

            for (int i = eventIndex; i < _currentClip.events.Length; i++)
            {
                if (_currentClip.events[i].functionName == "IncrementAnimationPhase" && currentAnimationTime == 0)
                    break;
                else if (_currentClip.events[i].functionName != "IncrementAnimationPhase" || Fixed32.Abs(currentAnimationTime - GetEventTime(_currentClip.events[i])) > Fixed32.PointOne / 2)
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

        public void ResetAnimationPhase()
        {
            _animationPhase = 0;
        }

        public void SetCharacterModelEnabled(float delay)
        {
            SetCharacterModelEnabled((Fixed32)delay);
        }

        public void SetCharacterModelEnabled(Fixed32 delay)
        {
            _characterMesh.gameObject.SetActive(false);

            RoutineBehaviour.Instance.StartNewTimedAction(a =>
            {
                _characterMesh.gameObject.SetActive(true);
            }, TimedActionCountType.SCALEDTIME, delay);
        }

        public void SetCharacterModelEnabled()
        {
            _characterMesh.gameObject.SetActive(true);
        }

        public void SetCharacterModelDisabled()
        {
            _characterMesh.gameObject.SetActive(false);
        }

        public void SetAccessoryToWinPosition()
        {
            _characterFeedbackBehaviour.SetAccessoryToWinPosition();
        }

        public void SpawnObject(UnityEngine.Object unityObject)
        {
            GameObject spawnObject = Instantiate(unityObject as GameObject, transform.position, Camera.main.transform.rotation);
            if (spawnObject == null)
            {
                Debug.LogError("Failed to spawn object: " + unityObject.name);
                return;
            }
        }

        public void ShakeScreen()
        {
            CameraBehaviour.ShakeBehaviour.ShakeRotation();
        }

        public void PlaySound(UnityEngine.Object soundClip)
        {
            SoundManagerBehaviour.Instance.PlaySound(soundClip as AudioClip);
        }

        public void PlayVoiceSound(int clipType)
        {
            _characterFeedbackBehaviour.PlayVoiceSound(clipType, true);
        }

        public void EnableAccessory()
        {
            _characterFeedbackBehaviour.EnableAccessory();
        }

        public void DisableAccessory()
        {
            _characterFeedbackBehaviour.DisableAccessory();
        }

        public void SetRotationY(float rotation)
        {
            SetRotationY((Fixed32)rotation);
        }

        public void SetRotationY(Fixed32 rotation)
        {
            transform.rotation = Quaternion.Euler(0, rotation, 0);
        }

        public void CalculateAnimationSpeed()
        {
            if (_evaluatingAnimatorState)
                return;

             
            AnimatorStateInfo stateInfo;

            AnimationPhase phase = (AnimationPhase)_animationPhase;
            Fixed32 newSpeed = 1;
            int eventIndex = 0;
            


            ///Calculates the new animation speed based on the current animation phase.
            ///If the phases time for animating is 0, the animator is set to the next phase of the animation.
            ///Otherwise, the new speed is calculated by dividing the current time it takes to get to the next phase, by the
            /// desired amount of time the animator should take be in that phase.
            switch (phase)
            {
                case AnimationPhase.STARTUP:

                    if (_animator.GetCurrentAnimatorClipInfo(0).Length <= 0)
                        return;

                    _currentClip = _animator.GetCurrentAnimatorClipInfo(0)[0].clip;
                    //Return if this clip couldn't be found or if it doesn't have animation events
                    if (!_currentClip || _currentClip.events.Length <= 0)
                        return;


                    stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                    if (_currentClipStartUpTime <= 0 || _movesetBehaviour.LastAbilityInUse != null
                        && (int)_movesetBehaviour.LastAbilityInUse.CurrentAbilityPhase > 0 && _animatingAbility)
                    {
                        PlayState(stateInfo.shortNameHash, _savedStateName, GetEventTime(_currentClip.events[0]));
                        break;
                    }

                    eventIndex = GetNextIncrementAnimationPhaseEvent(GetClipLength(_currentClip) * GetLoopedNormalizedTime(stateInfo));

                    if (eventIndex < 0 || eventIndex >= _currentClip.events.Length)
                        break;
                    newSpeed = GetEventTime(_currentClip.events[eventIndex]) / _currentClipStartUpTime;
                    break;

                case AnimationPhase.ACTIVE:
                    if (!_currentClip)
                        _currentClip = _animator.GetCurrentAnimatorClipInfo(0).Length > 0 ? _animator.GetCurrentAnimatorClipInfo(0)[0].clip : null;

                    if (!_currentClip)
                        break;

                    if ((_currentClipActiveTime <= 0 || _movesetBehaviour.LastAbilityInUse != null && (int)_movesetBehaviour.LastAbilityInUse.CurrentAbilityPhase > 1)
                        && _currentClip.events.Length >= 2 && _animatingAbility)
                    {
                        _animator.StartPlayback();
                        _animator.playbackTime = GetEventTime(_currentClip.events[1]);
                        break;
                    }

                    stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                    Fixed32 nextTimeStamp = GetClipLength(_currentClip);
                    eventIndex = 0;

                    if (_currentClip.events.Length > 1)
                    {
                        eventIndex = GetNextIncrementAnimationPhaseEvent(GetClipLength(_currentClip) * GetLoopedNormalizedTime(_animator.GetCurrentAnimatorStateInfo(0)));
                        int nextEventIndex = GetNextIncrementAnimationPhaseEvent(eventIndex);

                        if (nextEventIndex < 0 || nextEventIndex >= _currentClip.events.Length)
                            break;

                        nextTimeStamp = GetEventTime(_currentClip.events[nextEventIndex]);
                    }

                    if (eventIndex < 0 || eventIndex >= _currentClip.events.Length)
                        break;

                    newSpeed = (nextTimeStamp - GetEventTime(_currentClip.events[eventIndex])) / _currentClipActiveTime;
                    break;
                case AnimationPhase.INACTIVE:

                    if (!_currentClip)
                        break;

                    if (_currentClip.events.Length < 2)
                        break;
                    else if (_currentClipRecoverTime <= 0)
                    {
                        _animator.playbackTime = GetClipLength(_currentClip);
                        break;
                    }

                    stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                    eventIndex = GetNextIncrementAnimationPhaseEvent(eventIndex);

                    newSpeed = (GetClipLength(_currentClip) - GetEventTime(_currentClip.events[eventIndex])) / _currentClipRecoverTime;
                    break;
            }

            _targetSpeed = newSpeed;
            ApplyAnimatorSpeed();
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
        /// AI-generated:
        /// Resolves the clip that belongs in the attack override slot for the currently active ability.
        /// The important detail here is that the animator state alone is not enough to restore an attack.
        /// Multiple abilities can drive the same "Attack" state, and some of them swap different clips
        /// into the override controller at runtime. Rollback therefore needs a deterministic way to
        /// reconstruct the exact clip that should be bound to the attack state before playback resumes.
        /// </summary>
        private bool TryGetAttackAnimationClip(Ability ability, out AnimationClip clip)
        {
            clip = null;

            if (ability?.abilityData == null)
                return false;

            switch (ability.abilityData.animationType)
            {
                case AnimationType.CAST:
                    clip = _defaultCastAnimation;
                    break;
                case AnimationType.MELEE:
                    clip = _defaultMeleeAnimation;
                    break;
                case AnimationType.SUMMON:
                    clip = _defaultSummonAnimation;
                    break;
                case AnimationType.CUSTOM:
                    return ability.abilityData.GetCustomAnimation(out clip);
            }

            return clip != null;
        }

        /// <summary>
        /// AI-generated:
        /// Searches for a clip by name across every location this component can reasonably source
        /// animation clips from during gameplay.
        ///
        /// Why name-based lookup is used here:
        /// rollback only stores lightweight playback data, not direct object references, so we need
        /// to reconstruct the correct clip from serialized identity. Attack animations can come from
        /// default attack clips, a custom ability clip, or an additional animation referenced directly
        /// by ability scripts via PlayAnimation(...). Looking in all of those places lets deserialization
        /// recover the same clip that was active when the frame was saved.
        /// </summary>
        private AnimationClip FindAnimationClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
                return null;

            if (_currentClip && _currentClip.name == clipName)
                return _currentClip;

            Ability[] abilitySources =
            {
                _currentAbilityAnimating,
                _movesetBehaviour.LastAbilityInUse
            };

            for (int i = 0; i < abilitySources.Length; i++)
            {
                Ability ability = abilitySources[i];
                if (ability?.abilityData == null)
                    continue;

                if (ability.abilityData.GetCustomAnimation(out AnimationClip customClip) && customClip && customClip.name == clipName)
                    return customClip;

                for (int animationIndex = 0; ability.abilityData.GetAdditionalAnimation(animationIndex, out AnimationClip additionalClip); animationIndex++)
                {
                    if (additionalClip && additionalClip.name == clipName)
                        return additionalClip;
                }
            }

            AnimationClip[] defaultAttackClips =
            {
                _defaultCastAnimation,
                _defaultMeleeAnimation,
                _defaultSummonAnimation
            };

            for (int i = 0; i < defaultAttackClips.Length; i++)
            {
                if (defaultAttackClips[i] && defaultAttackClips[i].name == clipName)
                    return defaultAttackClips[i];
            }

            for (int i = 0; i < _runtimeController.animationClips.Length; i++)
            {
                AnimationClip runtimeClip = _runtimeController.animationClips[i];

                if (runtimeClip && runtimeClip.name == clipName)
                    return runtimeClip;
            }

            return null;
        }

        /// <summary>
        /// AI-generated:
        /// Applies a clip back into the same runtime override slot that originally produced the saved
        /// animation state. This happens before Animator.Play(...) during deserialization so the state
        /// hash points at the same motion source it did when the frame was serialized.
        ///
        /// Without this step, restoring the Attack state would only recover the controller state name,
        /// while the actual clip playing inside that state could silently differ after rollback.
        /// </summary>
        private void ApplyOverrideClip(AnimationClip clip, AnimationOverrideType overrideType)
        {
            if (!clip || overrideType == AnimationOverrideType.NONE)
                return;

            _currentClip = clip;
            _currentOverrideType = overrideType;

            switch (overrideType)
            {
                case AnimationOverrideType.ATTACK:
                    _overrideController["Cast"] = clip;
                    break;
                case AnimationOverrideType.CUSTOM_ACTION:
                    _overrideController["rig|rigAction"] = clip;
                    break;
            }
        }

        /// <summary>
        /// AI-generated:
        /// Rebuilds the timing context that attack playback depends on. Some attack animations do not
        /// simply run at clip speed; their startup/active/recover pacing is derived from the current
        /// ability data. If rollback restores the visible attack clip but not these timing fields,
        /// later speed calculations can diverge even though the state and clip were restored correctly.
        /// </summary>
        private void CacheAttackAnimationState(Ability ability)
        {
            _currentAbilityAnimating = ability;

            if (ability?.abilityData == null)
                return;

            _animationPhase = (int)ability.CurrentAbilityPhase;

            if (ability.abilityData.useAbilityTimingForAnimation)
            {
                _currentClipStartUpTime = ability.abilityData.startUpTime;
                _currentClipActiveTime = ability.abilityData.timeActive;
                _currentClipRecoverTime = ability.abilityData.recoverTime;
            }
            else
            {
                _animationPhase = 3;
            }
        }

        public void PlayAnimation(AnimationClip clip, Fixed32 speed, bool stopCurrentAnimation)
        {
            if (stopCurrentAnimation)
                StopCurrentAnimation();

            _targetSpeed = speed;
            ApplyAnimatorSpeed();
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            ApplyOverrideClip(clip, AnimationOverrideType.CUSTOM_ACTION);

            PlayState("CustomFromScript");
        }

        public void PlayAnimation(AnimationClip clip, Fixed32 speed, bool stopCurrentAnimation, bool shouldMirror)
        {
            if (stopCurrentAnimation)
                StopCurrentAnimation();

            _targetSpeed = speed;
            ApplyAnimatorSpeed();
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            ApplyOverrideClip(clip, AnimationOverrideType.CUSTOM_ACTION);
            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT && shouldMirror);
            PlayState("CustomFromScript");
        }

        public void PlayAnimation(float time, AnimationClip clip)
        {
            PlayAnimation((Fixed32)time, clip);
        }

        public void PlayAnimation(Fixed32 time, AnimationClip clip)
        {
            if (time <= 0)
              return;

            Fixed32 newSpeed = GetClipLength(clip) / time;

            PlayAnimation(clip, newSpeed, false, true);
        }

        public void PlayAbilityAnimation()
        {

            Ability ability = _movesetBehaviour.LastAbilityInUse;

            StopCurrentAnimation();
            _currentAbilityAnimating = ability;
            _animationPhase = 0;

            _animator.Update(GridGame.FixedTimeStep);

            ///Play animation based on type
            if (!TryGetAttackAnimationClip(_currentAbilityAnimating, out AnimationClip attackClip))
            {
                Debug.LogError("Couldn't play attack animation. Couldn't find the attack clip for " + ability.abilityData.abilityName);
                return;
            }

            ApplyOverrideClip(attackClip, AnimationOverrideType.ATTACK);
            _animatingMotion = false;
            _animationPhase = 0;

            if (ability.abilityData.useAbilityTimingForAnimation)
            {
                _currentClipStartUpTime = ability.abilityData.startUpTime;
                _currentClipActiveTime = ability.abilityData.timeActive;
                _currentClipRecoverTime = ability.abilityData.recoverTime;
            }
            else
                _animationPhase = 3;

            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT && ability.abilityData.ShouldMirror);
            PlayState("Attack");
        }

        /// <summary>
        /// Stops the animator and playable graph from playing the current animation
        /// </summary>
        public void StopCurrentAnimation()
        {
            _animator.Rebind();
            _animator.StopPlayback();
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _currentClip = null;
            _overrideController["Cast"] = _runtimeController.animationClips[0];
            _animator.SetBool("OnRightSide", _moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT);
            ApplyAnimatorSpeed();
        }

        public void LockAirTravelAnim(Vector2 lockedDirection)
        {
            _lockedAirDirection = lockedDirection;
            _airTravelLocked = true;
        }

        public void UnlockAirTravelAnim()
        {
            _airTravelLocked = false;
            _lockedAirDirection = Vector2.zero;
        }

        /// <summary>
        /// Plays the appropriate move clip based on the move direction
        /// </summary>
        public void PlayMovementAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _animatingMotion = true;
            _animationPhase = 0;

            //Calculates the time it takes to get to the destination
            FVector2 oldPosition = new FVector2();

            if (_moveBehaviour.PreviousPanel)
                oldPosition = _moveBehaviour.PreviousPanel.Position;

            Fixed32 travelDistance = (oldPosition - _moveBehaviour.CurrentPanel.Position).Magnitude;
            Fixed32 travelTime = travelDistance / _moveBehaviour.Speed;
            _currentClipStartUpTime = _moveAnimationStartUpTime;
            _currentClipActiveTime = travelTime + _moveAnimationHangTime;
            _currentClipRecoverTime = _defenseBehaviour.IsPhaseShifting ? _moveAnimationRecoverTime + _defenseBehaviour.DefaultPhaseShiftRestTime.FixedValue : _moveAnimationRecoverTime;

            SetMoveAnimParameters();

            PlayState("Movement");
        }
      
        public void PlayGroundRecoveryAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _animationPhase = 0;
            PlayState("GroundRecovery");
            AnimatorClipInfo[] info = _animator.GetCurrentAnimatorClipInfo(0);

            if (info.Length == 0)
                return;
            _targetSpeed = GetClipLength(_animator.GetCurrentAnimatorClipInfo(0)[0].clip) / (_knockbackBehaviour.LandingScript.KnockDownRecoverTime - Fixed32.PointOne);
        }

        public void PlayHardLandingAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _animationPhase = 0;
            PlayState("HardLanding");
            AnimatorClipInfo[] info = _animator.GetCurrentAnimatorClipInfo(0);

            if (info.Length == 0)
                return;

            _targetSpeed = GetClipLength(_animator.GetCurrentAnimatorClipInfo(0)[0].clip) / _knockbackBehaviour.LandingScript.KnockDownLandingTime;
        }

        public void PlaySoftLandingAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _animationPhase = 0;
            PlayState("SoftLanding");
            AnimatorClipInfo[] info = _animator.GetCurrentAnimatorClipInfo(0);

            if (info.Length == 0)
                return;
            _targetSpeed = GetClipLength(_animator.GetCurrentAnimatorClipInfo(0)[0].clip) / _knockbackBehaviour.LandingScript.LandingTime;
        }

        public void PlayDamageAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;

            _targetSpeed = 1;
            if (_knockbackBehaviour.TimeInCurrentHitStun <= 0)
                return;

            _animationPhase = 0;

            _currentClipStartUpTime = _flinchStartUpTime;

            if (_knockbackBehaviour.Physics.IsGrounded && _knockbackBehaviour.CurrentAirState == Movement.AirState.NONE)
                PlayState("GroundedFlinching");
            else
                PlayState("InAirFlinching");

            AnimatorStateInfo nextState = _animator.GetNextAnimatorStateInfo(0);
            AnimationClip clip = GetCurrentAnimationClip();
            _currentClipActiveTime = _knockbackBehaviour.TimeInCurrentHitStun - _flinchStartUpTime;

            _animatingMotion = true;
        }

        public void PlayStunAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _targetSpeed = 1;
            if (!_knockbackBehaviour.Stunned)
                return;

            _animationPhase = 0;

            if (_knockbackBehaviour.FixedTransform.WorldPosition.Y <= new Fixed32(32768) && _knockbackBehaviour.CurrentAirState == Movement.AirState.NONE)
            {
                PlayState("Stunned");
            }
            else
            {
                PlayState("AirStun");
                //_characterFeedbackBehaviour.ShakeCharacter(_knockbackBehaviour.TimeInCurrentStun, 0.5f, 1000);
            }

            _animatingMotion = true;
        }

        /// <summary>
        /// Plays the tumbling animation when the character enters the tumbling air state.
        /// </summary>
        public void PlayTumblingAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _targetSpeed = 1;
            _animationPhase = 0;
            PlayState("Tumbling");
            _animatingMotion = true;
        }

        /// <summary>
        /// Plays the free-fall animation when the character is airborne without entering tumbling.
        /// </summary>
        public void PlayFreeFallAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _targetSpeed = 1;
            _animationPhase = 0;
            PlayState("FreeFall");
            _animatingMotion = true;
        }

        /// <summary>
        /// Returns the character to the idle animation state when no movement or knockback animation should be active.
        /// </summary>
        public void PlayIdleAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _targetSpeed = 1;
            _animationPhase = 0;
            PlayState("Idle");
            _animatingMotion = false;
            _animatingAbility = false;
        }


        public void UpdateInAirMoveDirection()
        {
            if (_airTravelLocked)
            {
                _animator.SetFloat("VelocityInAirY", _lockedAirDirection.y);
                _animator.SetFloat("VelocityInAirX", _lockedAirDirection.x);
                return;
            }

            _animator.SetFloat("VelocityInAirY", _knockbackBehaviour.Physics.Velocity.Y);
            _animator.SetFloat("VelocityInAirX", _knockbackBehaviour.Physics.Velocity.X * _moveBehaviour.GetAlignmentX());
        }

        public bool CompareStateName(string name)
        {
            return _animator.GetCurrentAnimatorStateInfo(0).IsName(name);
        }

        /// <summary>
        /// Plays the animation to break a fall based on the 
        /// direction the structure is to the character
        /// </summary>
        public void PlayFallBreakAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _animationPhase = 0;

            string animationName = "";
            Fixed32 techLength = 0;
            if (_bracedAgainstFloor)
            {
                animationName = "GroundTech";
                techLength = (Fixed32)_defenseBehaviour.GroundTechLength;
            }
            else
            {
                animationName = "WallTech";
                techLength = (Fixed32)_defenseBehaviour.WallTechJumpDuration;
            }

            AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length <= 0) return;

            _targetSpeed = GetClipLength(clipInfo[0].clip) / techLength;
            PlayState(animationName);
            _animatingMotion = true;
        }

        public void PlayManualShuffleAnimation()
        {
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentAbilityAnimating = null;
            _animatingAbility = false;
            _animationPhase = 0;

            _animatingMotion = false;
            _animatingAbility = false;

            StopCurrentAnimation();
            _currentClipStartUpTime = _manualShuffleStartTime;
            _currentClipActiveTime = _manualShuffleActiveTime;
            _currentClipRecoverTime = _manualShuffleRecoverTime;

            PlayState("Shuffle");
        }

        /// <summary>
        /// Adds a listener to a custom animationn event that's attached to the animation clip.
        /// </summary>
        /// <param name="eventName">The name of the event that matches the string parameter in the clip event arguments.</param>
        /// <param name="action">The action to perform when called.</param>
        public void AddEventListener(string eventName, UnityAction action)
        {
            _animationEvents.Add(new CustomAnimationEvent(eventName, action));
        }

        /// <summary>
        /// Adds a listener to a custom animationn event that's attached to the animation clip.
        /// </summary>
        /// <param name="eventName">The name of the event that matches the string parameter in the clip event arguments.</param>
        /// <param name="action">The action to perform when called.</param>
        public void RemoveEventListener(string eventName)
        {
            CustomAnimationEvent custEvent = _animationEvents.Find(c => c.EventName == eventName);

            _animationEvents.Remove(custEvent);
        }

        public void RaiseCustomEvent(string eventName)
        {
            CustomAnimationEvent targetEvent = _animationEvents.Find(custEvent => custEvent.EventName == eventName);
            if (!targetEvent.Equals(default(CustomAnimationEvent)))
                targetEvent.Action.Invoke();
        }

        public void SetMoveAnimParameters()
        {
            int mirror = 1;

            if (_moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT) mirror = -1;

            _animator.SetFloat("MoveDirectionX", _moveBehaviour.MoveDirection.X * mirror);
            _animator.SetFloat("MoveDirectionY", _moveBehaviour.MoveDirection.Y);
        }    

        public override void Tick(Fixed32 deltaTime)
        {
            base.Tick(deltaTime);

            //if (_animator.enabled)
            //    _animator.enabled = false;

            //_animator.Update(deltaTime);

            if (_characterStateMachine.CurrentState == "Moving")
                SetMoveAnimParameters();

            if (_characterStateManager.StateMachine.CurrentState != "Attacking" && AbilityAnimationRoutine != null)
            {
                StopCoroutine(AbilityAnimationRoutine);
                AbilityAnimationRoutine = null;
            }
            AnimatorTransitionInfo currentInfo = _animator.GetAnimatorTransitionInfo(0);
            if (_lastTransitionInfo.nameHash != currentInfo.nameHash && currentInfo.nameHash != 0)
                _lastTransitionInfo = _animator.GetAnimatorTransitionInfo(0);

            ApplyAnimatorSpeed(true);

            _currentAnimationTime += deltaTime * (_targetSpeed * RoutineBehaviour.Instance.CharacterTimeScale);
            //Debug.Log("TimeScale: " + RoutineBehaviour.Instance.CharacterTimeScale);

            if (!_knockbackBehaviour.Physics.IsGrounded)
                UpdateInAirMoveDirection();
        }

        // Update is called once per frame
        public override void LateTick(Fixed32 deltaTime)
        {
            base.LateTick(deltaTime);

            if (_moveBehaviour.Alignment == GridScripts.GridAlignment.RIGHT)
                _animator.SetBool("OnRightSide", true);

            _animatingAbility = _animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");

            if (_animatingAbility)
            {
                _currentOverrideType = AnimationOverrideType.ATTACK;
            }
            else if (_currentOverrideType == AnimationOverrideType.ATTACK)
            {
                _currentOverrideType = AnimationOverrideType.NONE;
            }
            else if (_currentOverrideType == AnimationOverrideType.CUSTOM_ACTION)
            {
                AnimationClip currentClip = GetCurrentAnimationClip();

                if (!currentClip || (_currentClip && currentClip != _currentClip))
                    _currentOverrideType = AnimationOverrideType.NONE;
            }
        }

        /// <summary>
        /// Saves the current animation state, including the current timestamp of the playing animation.
        /// </summary>
        public override void Serialize(BinaryWriter bw)
        {
            //CaptureSerializedAnimationState();
            //// AI-generated:
            //// This save payload captures the minimum state needed to restore animation playback
            //// deterministically after rollback:
            //// 1. the exact animator state to jump back into
            //// 2. the playback time within that state
            //// 3. which runtime override slot supplied the motion, if any
            //// 4. the concrete clip bound to that override slot
            //// 5. the current mirroring flag, because some states visually depend on it
            ////
            //// Storing the clip name is especially important for attack animations and custom action
            //// animations because those states can be reused while swapping clips at runtime.
            //bw.Write(_savedStateName);
            //_currentAnimationTime.Serialize(bw);
            //bw.Write((int)_savedOverrideType);
            //bw.Write(_savedClipName ?? string.Empty);
            //bw.Write(_savedMirror);
        }

        /// <summary>
        /// Loads the saved animation state and resumes the animation from the saved timestamp.
        /// </summary>
        public override void Deserialize(BinaryReader br)
        {
            //_savedStateName = br.ReadString();
            //_currentAnimationTime = _currentAnimationTime.Deserialize(br);
            //_savedOverrideType = (AnimationOverrideType)br.ReadInt32();
            //_savedClipName = br.ReadString();
            //_savedMirror = br.ReadBoolean();

            //_hasSavedPlaybackState = true;

            //// AI-generated:
            //// Deserialization intentionally rebuilds playback in this order:
            //// 1. restore mirrored presentation flags
            //// 2. restore/resolve the override clip identity
            //// 3. rebuild any attack-specific timing context
            //// 4. jump back into the saved animator state at the saved normalized time
            ////
            //// The ordering matters. If Animator.Play(...) runs before the correct override clip is rebound,
            //// the animator can enter the correct state while still playing the wrong motion.
            //ApplySavedAnimationState(_savedStateName, _currentAnimationTime, _savedOverrideType, _savedClipName, _savedMirror);
        }

        /// <summary>
        /// Hashes the serialized animation playback state so desyncs caused by
        /// animation rollback can be isolated to this component.
        /// </summary>
        protected override string[] GetLogItems()
        {
            return new string[]
            {
                //$"Saved State Name: {_savedStateName}",
                //$"Saved Playback Time: {_currentAnimationTime}",
                //$"Saved Override Type: {_savedOverrideType}",
                //$"Saved Clip Name: {_savedClipName}",
                //$"Saved Mirror: {_savedMirror}",
             };
        }

        /// <summary>
        /// Reapplies the saved visual animation state in the same order used by rollback
        /// deserialization so overrides are bound before the animator jumps to the
        /// requested state and time.
        /// </summary>
        private void ApplySavedAnimationState(string saveStateName, Fixed32 playbackTime, AnimationOverrideType overrideType, string clipName, bool shouldMirror)
        {
            _animator.SetBool("OnRightSide", shouldMirror);
            _currentOverrideType = AnimationOverrideType.NONE;
            _currentClip = null;
            _animatingAbility = overrideType == AnimationOverrideType.ATTACK;
            _savedMirror = shouldMirror;

            if (_animatingAbility)
                CacheAttackAnimationState(_movesetBehaviour.LastAbilityInUse);

            //TO DO: Should make a serialized list of all clips used. On deserialize, in addition to the state should restore the specific ability clip.
            if (!string.IsNullOrEmpty(clipName))
            {
                AnimationClip clip = FindAnimationClip(clipName);

                if (clip)
                {
                    ApplyOverrideClip(clip, overrideType);
                }
                else
                {
                    Debug.LogWarning($"Failed to restore animation clip '{clipName}' on {name}.");
                }
            }
            else if (overrideType == AnimationOverrideType.ATTACK && TryGetAttackAnimationClip(_movesetBehaviour.LastAbilityInUse, out AnimationClip attackClip))
            {
                ApplyOverrideClip(attackClip, AnimationOverrideType.ATTACK);
            }

            PlayState(saveStateName, playbackTime);
        }

        /// <summary>
        /// Captures the exact fields written by Serialize so logging and replay-complete
        /// visual correction both work from the same payload shape.
        /// </summary>
        private void CaptureSerializedAnimationState()
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            AnimationClip currentClip = GetCurrentAnimationClip();
            bool hasOverrideClip = _currentOverrideType != AnimationOverrideType.NONE && currentClip;

            _savedStateName = _characterStateManager?.StateMachine?.CurrentState ?? _savedStateName;
            _savedOverrideType = _currentOverrideType;
            _savedClipName = currentClip ? currentClip.name : string.Empty;
            _savedMirror = _animator.GetBool("OnRightSide");

            _hasSavedPlaybackState = true;
        }
    }
}
