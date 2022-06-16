using System;
using Lodis.Utility;
using UnityEngine;

namespace Lodis.Movement
{
    [RequireComponent(typeof(KnockbackBehaviour))]
    public class LandingBehaviour : MonoBehaviour
    {
        [Tooltip("The amount of time it takes for this object to regain footing after landing")] [SerializeField]
        private float _landingTime;
        private RoutineBehaviour.TimedAction _landingAction;

        [SerializeField] private float _knockDownTime;
        [SerializeField] private float _knockDownRecoverTime;
        [SerializeField] private float _knockDownRecoverInvincibleTime;
        [SerializeField] private float _knockDownLandingTime;
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
            _knockback.AddOnKnockBackAction(() =>
            {
                CancelLanding();
                if (_knockback.LaunchVelocity.magnitude > _knockback.MinimumLaunchMagnitude.Value)
                    CanCheckLanding = false;
            });
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_knockback.CurrentAirState != AirState.NONE &&
                other.CompareTag("Panel") && !_knockback.Stunned && CheckFalling())
            {
                CanCheckLanding = true;
            }
        }

        private void CancelLanding()
        {
            if (!Landing) return;

            RoutineBehaviour.Instance.StopTimedAction(_landingAction);
            _knockback.DisableInvincibility();
            _knockback.Physics.Rigidbody.isKinematic = false;
            IsDown = false;
            Landing = false;
            RecoveringFromFall = false;
        }
        
        private void TumblingLanding(object[] arguments)
        {
            RoutineBehaviour.Instance.StopTimedAction(_knockback.GravityIncreaseTimer);
            _knockback.Physics.Gravity = _knockback.StartGravity;
            //Start knockdown
            IsDown = true;
            _knockback.SetInvincibilityByTimer(_knockDownRecoverInvincibleTime);
            _knockback.MovementBehaviour.DisableMovement(condition => !RecoveringFromFall && !IsDown, false, true);

            _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(TumblingRecover,
                TimedActionCountType.SCALEDTIME, _knockDownLandingTime);
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
                },
                TimedActionCountType.SCALEDTIME, KnockDownRecoverTime);
        }

        public void StartLandingLag()
        {
            Landing = true;
            _knockback.MovementBehaviour.DisableMovement(condition => !Landing, false, true);
            _knockback.LastTimeInKnockBack = 0;
            _knockback.Physics.StopVelocity();

            RoutineBehaviour.Instance.StopTimedAction(_landingAction);
            
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
                        },
                        TimedActionCountType.SCALEDTIME, _landingTime);
                    break;
                case AirState.BREAKINGFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    CanCheckLanding = false;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        Landing = false;
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
        
        private void FixedUpdate()
        {
            if (_knockback.Physics.Rigidbody.velocity.magnitude <= _knockback.NetForceLandingTolerance &&
                !Landing && CanCheckLanding)
            {
                StartLandingLag();
            }
        }
    }
}
