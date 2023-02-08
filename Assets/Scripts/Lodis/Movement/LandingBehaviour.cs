using System;
using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Movement
{
    [RequireComponent(typeof(KnockbackBehaviour))]
    public class LandingBehaviour : MonoBehaviour
    {
        [Tooltip("The amount of time it takes for this object to regain footing after landing")] [SerializeField]
        private float _landingTime;
        private TimedAction _landingAction;

        [SerializeField] private float _knockDownTime;
        [SerializeField] private float _knockDownRecoverTime;
        [SerializeField] private float _knockDownRecoverInvincibleTime;
        [SerializeField] private float _knockDownLandingTime;
        [SerializeField] private IntVariable _groundedHitMax;
        private UnityAction _onLandingStart;
        private UnityAction _onLand;
        private int _groundedHitCounter;
        private KnockbackBehaviour _knockback;
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
            _knockback.AddOnStunAction(CancelLanding);
            _knockback.Physics.AddOnForceAddedEvent(args => TryCancelLanding());
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

        private void OnTriggerStay(Collider other)
        {
            if (_knockback.CurrentAirState != AirState.NONE && other.CompareTag("Panel") && CheckFalling() && !_knockback.Physics.IsFrozen && _knockback.Physics.ObjectAtRest)
            {
                CanCheckLanding = true;
            }
        }

        private void CancelLanding()
        {
            if (!Landing) return;

            RoutineBehaviour.Instance.StopAction(_landingAction);
            _knockback.DisableInvincibility();
            _knockback.Physics.RB.isKinematic = false;
            IsDown = false;
            Landing = false;
            RecoveringFromFall = false;
        }
        
        private void TumblingLanding(object[] arguments)
        {
            RoutineBehaviour.Instance.StopAction(_knockback.GravityIncreaseTimer);
            _knockback.Physics.Gravity = _knockback.StartGravity;
            //Start knockdown
            IsDown = true;
            _knockback.SetInvincibilityByTimer(_knockDownRecoverInvincibleTime);
            _knockback.MovementBehaviour.DisableMovement(condition => !RecoveringFromFall && !IsDown, false, true);

            _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(TumblingRecover,
                TimedActionCountType.SCALEDTIME, _knockDownLandingTime);
            _onLand?.Invoke();
        }

        private void TumblingRecover(object[] args)
        {
            RecoveringFromFall = true;
            IsDown = false;
            //Start recovery from knock down
            _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(values =>
            {
                RecoveringFromFall = false;
                CanCheckLanding = false;
                Landing = false;
                _knockback.CurrentAirState = AirState.NONE;
            },
            TimedActionCountType.SCALEDTIME, KnockDownRecoverTime);
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

            RoutineBehaviour.Instance.StopAction(_landingAction);
            
            switch (_knockback.CurrentAirState)
            {
                case AirState.TUMBLING:
                    _knockback.CurrentAirState = AirState.NONE;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(TumblingLanding,
                        TimedActionCountType.SCALEDTIME, _knockDownLandingTime);
                    break;
                case AirState.FREEFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    CanCheckLanding = false;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                        {
                            Landing = false;
                            _onLand?.Invoke();
                        },
                        TimedActionCountType.SCALEDTIME, _landingTime);
                    break;
                case AirState.BREAKINGFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    CanCheckLanding = false;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        _onLand?.Invoke();
                        Landing = false;
                        _knockback.CurrentAirState = AirState.NONE;
                    }, TimedActionCountType.SCALEDTIME, _knockback.DefenseBehaviour.GroundTechLength);
                    break;
            }

            _knockback.CancelHitStun();
        }

        private bool CheckFalling()
        {
            Vector3 velocity = _knockback.Physics.LastVelocity;
            float dot = Vector3.Dot(Vector3.down, velocity.normalized);

            return dot >= 0;
        }
        
        private void LateUpdate()
        {
            if (_knockback.Physics.RB.velocity.magnitude <= _knockback.NetForceLandingTolerance &&
                !Landing && CanCheckLanding && !_knockback.Stunned && _knockback.Physics.IsGrounded)
            {
                StartLandingLag();
            }

            if (!_knockback.Physics.IsGrounded || _knockback.CheckIfIdle())
                _groundedHitCounter = 0;

            if (Landing && !_knockback.Physics.IsGrounded)
                CancelLanding();
        }
    }
}
