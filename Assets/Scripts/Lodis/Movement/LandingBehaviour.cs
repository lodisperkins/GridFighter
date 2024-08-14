using System;
using System.IO;
using FixedPoints;
using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Types;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Movement
{
    [RequireComponent(typeof(KnockbackBehaviour))]
    public class LandingBehaviour : SimulationBehaviour
    {
        [Tooltip("The amount of time it takes for this object to regain footing after landing")] [SerializeField]
        private float _landingTime;
        private FixedTimeAction _landingAction;

        [SerializeField] private float _knockDownTime;
        [SerializeField] private float _knockDownRecoverTime;
        [SerializeField] private float _knockDownRecoverInvincibleTime;
        [SerializeField] private float _knockDownLandingTime;
        [SerializeField] private IntVariable _groundedHitMax;
        private UnityAction _onLandingStart;
        private UnityAction _onLand;
        private UnityAction _onRecover;
        private int _groundedHitCounter;
        private KnockbackBehaviour _knockback;
        private CharacterAnimationBehaviour _characterAnimator;
        private bool _canCheckLanding;

        public float LandingTime { get => _landingTime;}
        public bool IsDown { get; private set; }
        public bool RecoveringFromFall { get; private set; }
        public float KnockDownRecoverTime { get => _knockDownRecoverTime; set => _knockDownRecoverTime = value; }
        public float KnockDownLandingTime { get => _knockDownLandingTime; set => _knockDownLandingTime = value; }
        /// <summary>
        /// Whether or not this object is current regaining footing after hitting the ground
        /// </summary>
        public bool Landing { get; private set; }

        public bool CanCheckLanding
        {
            get => _canCheckLanding;
            set => _canCheckLanding = value;
        }

        private void Awake()
        {
            _knockback = GetComponent<KnockbackBehaviour>();
            _characterAnimator = GetComponentInChildren<CharacterAnimationBehaviour>();
            _knockback.AddOnStunAction(CancelLanding);
            _knockback.Physics.AddOnForceAddedEvent(args => TryCancelLanding());
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(CancelLanding);
        }

        private bool TryCancelLanding()
        {
            if (_groundedHitCounter > _groundedHitMax.Value)
                return false;

            CancelLanding();
            CanCheckLanding = false;

            if (_knockback.Physics.IsGrounded)
                _groundedHitCounter++;

            return true;
        }

        public void AddOnLandingStartAction(UnityAction action)
        {
            _onLandingStart += action;
        }

        public void AddOnLandAction(UnityAction action)
        {
            _onLand += action;
        }

        public void AddOnRecoverAction(UnityAction action)
        {
            _onRecover += action;
        }

        public override void OnOverlapStay(Collision other)
        {
            if (_knockback.CurrentAirState != AirState.NONE && other.Entity.UnityObject.CompareTag("Panel") && CheckFalling() && !_knockback.Physics.IsFrozen && _knockback.Physics.ObjectAtRest)
            {
                CanCheckLanding = true;
            }
        }

        public void CancelLanding()
        {
            if (!Landing) return;

            _landingAction.Stop();
            _knockback.DisableInvincibility();
            _knockback.Physics.StopAllForces();
            //_knockback.Physics.RB.isKinematic = false;
            IsDown = false;
            Landing = false;
            RecoveringFromFall = false;
        }
        
        private void TumblingLanding()
        {
            _knockback.Physics.Gravity = _knockback.StartGravity;
            //Start knockdown
            IsDown = true;
            _knockback.SetInvincibilityByTimer(_knockDownRecoverInvincibleTime);
            _knockback.MovementBehaviour.DisableMovement(condition => !RecoveringFromFall && !IsDown, false, true);

            _landingAction = FixedPointTimer.StartNewTimedAction(TumblingRecover, _knockDownLandingTime);
            _onLand?.Invoke();
        }

        private void TumblingRecover()
        {
            RecoveringFromFall = true;
            IsDown = false;
            //Start recovery from knock down
            _landingAction = FixedPointTimer.StartNewTimedAction(() =>
            {
                RecoveringFromFall = false;
                CanCheckLanding = false;
                Landing = false;
                _knockback.CurrentAirState = AirState.NONE;
                _onRecover?.Invoke();
            }, KnockDownRecoverTime);
        }

        public void StartLandingLag()
        {
            GridScripts.PanelBehaviour panel = null;

            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel, false))
                _knockback.MovementBehaviour.Position = panel.Position;

            Landing = true;
            _onLandingStart?.Invoke();
            _knockback.MovementBehaviour.DisableMovement(condition => !Landing, false, true);
            _knockback.LastTimeInKnockBack = 0;
            _knockback.CancelHitStun();

            _landingAction.Stop();
            
            switch (_knockback.CurrentAirState)
            {
                case AirState.TUMBLING:
                    _knockback.CurrentAirState = AirState.NONE;
                    CanCheckLanding = false;
                    _landingAction = FixedPointTimer.StartNewTimedAction(TumblingLanding, _knockDownLandingTime);
                    break;
                case AirState.FREEFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    CanCheckLanding = false;
                    _landingAction = FixedPointTimer.StartNewTimedAction(() =>
                        {
                            Landing = false;
                            _onLand?.Invoke();
                        }, _landingTime);
                    break;
                case AirState.BREAKINGFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    CanCheckLanding = false;
                    _landingAction = FixedPointTimer.StartNewTimedAction(() =>
                    {
                        _onLand?.Invoke();
                        Landing = false;
                        _knockback.CurrentAirState = AirState.NONE;
                    }, _knockback.DefenseBehaviour.GroundTechLength);
                    break;
            }

            _knockback.CancelHitStun();
        }

        private bool CheckFalling()
        {
            Vector3 velocity = (Vector3)_knockback.Physics.Velocity;
            if (velocity.magnitude == 0)
                return false;

            float dot = Vector3.Dot(Vector3.down, velocity.normalized);

            return dot >= 0;
        }
        
        public override void Tick(Fixed32 dt)
        {
            if (_knockback.Physics.Velocity.Magnitude <= _knockback.NetForceLandingTolerance &&
                !Landing && CanCheckLanding && !_knockback.Stunned && _knockback.Physics.IsGrounded)
            {
                StartLandingLag();
            }

            if (!_knockback.Physics.IsGrounded || _knockback.CheckIfIdle())
                _groundedHitCounter = 0;

            if (Landing && !_knockback.Physics.IsGrounded)
                CancelLanding();

            if (Landing && !RecoveringFromFall && !_characterAnimator.CompareStateName("HardLanding"))
                _characterAnimator.PlayHardLandingAnimation();
        }

        public override void Serialize(BinaryWriter bw)
        {
        }

        public override void Deserialize(BinaryReader br)
        {
        }
    }
}
