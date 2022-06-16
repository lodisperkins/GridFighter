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
        private bool CanCheckLanding;

        public float LandingTime { get => _landingTime;}
        public bool IsDown { get; private set; }
        public bool RecoveringFromFall { get; private set; }
        public float KnockDownRecoverTime { get => _knockDownRecoverTime; set => _knockDownRecoverTime = value; }
        public float KnockDownLandingTime { get => _knockDownLandingTime; set => _knockDownLandingTime = value; }
        /// <summary>
        /// Whether or not this object is current regaining footing after hitting the ground
        /// </summary>
        public bool Landing { get; private set; }

        private void Awake()
        {
            _knockback = GetComponent<KnockbackBehaviour>();
            _knockback.Physics.AddOnCollisionWithGroundEvent(args =>
            {
                if (_knockback.CurrentAirState == AirState.FREEFALL) TryStartLandingLag();
            });
            _knockback.AddOnStunAction(() => { if (Landing) RoutineBehaviour.Instance.StopTimedAction(_landingAction); });
            _knockback.AddOnKnockBackStartAction(CancelLanding);
        }

        // Start is called before the first frame update
        void Start()
        {
            _knockback.AddOnKnockBackAction(() => Landing = false);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_knockback.CheckIfIdle()) return;

            if (collision.gameObject.CompareTag("CollisionPlane") && (_knockback.CurrentAirState == AirState.TUMBLING || _knockback.CurrentAirState == AirState.FREEFALL))
            {
                CanCheckLanding = true;
            }
        }

        private void OnCollisionExit(Collision other)
        {
            if (_knockback.CheckIfIdle()) return;

            if (other.gameObject.CompareTag("CollisionPlane") && (_knockback.CurrentAirState == AirState.TUMBLING || _knockback.CurrentAirState == AirState.FREEFALL))
            {
                CanCheckLanding = false;
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
    
        /// <summary>
        /// Starts landing lag if the object just fell onto a structure
        /// </summary>
        /// <param name="args"></param>
        public void TryStartLandingLag(params object[] args)
        {
            if (_knockback.Stunned || _knockback.Physics.Rigidbody.velocity.magnitude > _knockback.NetForceLandingTolerance) return;

            StartLandingLag();
        }

        private void TumblingLanding(object[] arguments)
        {
            if (!Landing) return;
            RoutineBehaviour.Instance.StopTimedAction(_knockback.GravityIncreaseTimer);
            _knockback.Physics.Gravity = _knockback.StartGravity;
            Landing = false;
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
            _knockback.Physics.MakeKinematic();
            _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(values =>
                {
                    RecoveringFromFall = false;
                    CanCheckLanding = false;
                },
                TimedActionCountType.SCALEDTIME, KnockDownRecoverTime);
        }
        
        private void StartLandingLag()
        {
            Landing = true;
            _knockback.MovementBehaviour.DisableMovement(condition => !Landing, false, true);
            _knockback.LastTimeInKnockBack = 0;
            _knockback.Physics.StopVelocity();

            switch (_knockback.CurrentAirState)
            {
                case AirState.TUMBLING:
                    _knockback.CurrentAirState = AirState.NONE;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(TumblingLanding,
                        TimedActionCountType.SCALEDTIME, _knockDownLandingTime);
                    break;
                case AirState.FREEFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                        {
                            Landing = false;
                            CanCheckLanding = false;
                        },
                        TimedActionCountType.SCALEDTIME, _landingTime);
                    break;
                case AirState.BREAKINGFALL:
                    _knockback.CurrentAirState = AirState.NONE;
                    _landingAction = RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        Landing = false;
                        _knockback.Physics.MakeKinematic();
                        CanCheckLanding = false;
                    }, TimedActionCountType.SCALEDTIME, _knockback.DefenseBehaviour.GroundTechLength);
                    break;
            }

            _knockback.CancelHitStun();
        }

        private void Update()
        {
            if (CanCheckLanding && !Landing)
                TryStartLandingLag();
        }
    }
}
