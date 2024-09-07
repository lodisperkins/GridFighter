using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using DG.Tweening;
using Lodis.GridScripts;
using System;
using Lodis.Utility;
using FixedPoints;
using System.IO;
using Types;

namespace Lodis.Movement
{
    public delegate void ForceAddedEvent(params object[] args);

    public enum BounceCombination
    {
        AVERAGE,
        MULTIPLY,
        MINIMUM,
        MAXIMUM
    }

    /// <summary>
    /// The component that handles all physics calucations for an entity.
    /// </summary>
    public class GridPhysicsBehaviour : SimulationBehaviour
    {
        [Header("Parameters")]
        [Tooltip("How much mass the game object has. The higher the number, the less panels it travels when knocked back.")]
        [SerializeField] private float _mass = 1;
        [Tooltip("The strength of the force pushing downwards on this object once in air.")]
        [SerializeField] private float _gravity = 9.81f;
        [Tooltip("How much this object will reduce the velocity of objects that bounce off of it.")]
        [SerializeField] private float _bounceDampen = 2;
        [SerializeField] private float _bounciness = 0.8f;
        [Tooltip("Any angles for knock back force recieved in this range will send the object directly upwards.")]
        [SerializeField] private float _rangeToIgnoreUpAngle = 0.2f;
        [Tooltip("How fast will objects be allowed to travel in knockback.")]
        [SerializeField] private ScriptableObjects.FloatVariable _maxMagnitude;
        [Tooltip("How this entity will react to other physics objects when bouncig off of them.")]
        [SerializeField] private BounceCombination _bounceCombination;
        [Tooltip("The curve entities will use when jumping by default.")]
        [SerializeField] private AnimationCurve _defaultJumpCurve;

        [Header("Toggles")]
        [Tooltip("Whether or not the force of gravity will be applied every frame.")]
        [SerializeField] private bool _useGravity = true;
        [Tooltip("If true, this object will ignore all forces acting on it including gravity.")]
        [SerializeField] private bool _ignoreForces;
        [Tooltip("Whether or not this object is currently touching an entity labeled as grounded.")]
        [SerializeField] private bool _isGrounded;
        [Tooltip("Whether or not this object will bounce upwards when the land on panels.")]
        [SerializeField] private bool _panelBounceEnabled;
        [Tooltip("Whether or not this object will rotate towards the direction it's heading in.")]
        [SerializeField] private bool _faceHeading;
        [Tooltip("Whether or not this object should increase how much it bounces based on how fast it's going.")]
        [SerializeField] private bool _useVelocityForBounce;

        [Header("Scene References")]
        [Tooltip("The collider attached this object that will be used for registering collision against objects while air")]
        [SerializeField] private Collider _bounceCollider;

        //---
        private bool _objectAtRest;
        private bool _isFrozen;
        private bool _isKinematic;
        private bool _gridActive = true;
        /// <summary>
        /// Whether or not this object will bounce on panels it falls on
        /// </summary>
        public bool PanelBounceEnabled { get =>_panelBounceEnabled; }
        /// <summary>
        /// Whether or not this object should be effected by gravity
        /// </summary>
        public bool UseGravity { get => _useGravity; set =>_useGravity = value; }
        public bool IsGrounded{ get => _isGrounded; }
        public bool ObjectAtRest { get => _objectAtRest; }
        public bool IgnoreForces { get => _ignoreForces; set => _ignoreForces = value; }
        public bool IsFrozen => _isFrozen;
        public bool FaceHeading { get => _faceHeading; set => _faceHeading = value; }
        public bool IsKinematic { get => _isKinematic; set => _isKinematic = value; }

        //Physics stats
        private FVector3 _acceleration;
        private FVector3 _velocity;
        private FVector3 _lastVelocity;
        private FVector3 _groundedBoxPosition;
        private FVector3 _groundedBoxExtents;
        private FVector3 _lastForceAdded;
        private FVector3 _forceToApply;
        private FVector3 _frozenStoredForce;
        private FVector3 _frozenVelocity;

        public FVector3 Acceleration { get => _acceleration; }
        public FVector3 GroundedBoxPosition { get => _groundedBoxPosition; set => _groundedBoxPosition = value; }
        public FVector3 GroundedBoxExtents { get => _groundedBoxExtents; set => _groundedBoxExtents = value; }
        public FVector3 ForceToApply { get => _forceToApply; private set => _forceToApply = value; }
        public FVector3 FrozenStoredForce { get => _frozenStoredForce; private set => _frozenStoredForce = value; }
        public FVector3 FrozenVelocity { get => _frozenVelocity; private set => _frozenVelocity = value; }
        /// <summary>
        /// Returns the velocity of the rigid body in the last fixed update
        /// </summary>
        public FVector3 Velocity { get => _velocity; set => ApplyVelocityChange(value); }
        /// <summary>
        /// How bouncy this object is
        /// </summary>
        public float Bounciness { get => _bounciness; set { _bounciness = value; } }
        public float Gravity { get =>_gravity; set => _gravity = value; }
        public float Mass { get => _mass; }
        public float BounceDampen { get => _bounceDampen; set => _bounceDampen = value; }


        //Events
        /// <summary>
        /// The event called when this object collides with another
        /// </summary>
        private CollisionEvent _onCollision;
        /// <summary>
        /// The event called when this object lands on top of a structure
        /// </summary>
        private CollisionEvent _onCollisionWithGround;
        private ForceAddedEvent _onForceAdded;
        private ForceAddedEvent _onForceAddedTemp;

        //Actions and timers
        private LerpAction _jumpSequence;
        private DelayedAction _freezeAction;
        private FixedConditionAction _bufferedJump;

        //Scene references
        private GridMovementBehaviour _movementBehaviour;
        public Collider BounceCollider { get => _bounceCollider; }
        public GridMovementBehaviour MovementBehaviour { get => _movementBehaviour; }
        public bool GridActive { get => _gridActive; private set => _gridActive = value; }


        public override void Serialize(BinaryWriter bw)
        {
            _velocity.Serialize(bw);
            _lastVelocity.Serialize(bw);
            _lastForceAdded.Serialize(bw);
            _forceToApply.Serialize(bw);
            _frozenStoredForce.Serialize(bw);
            _frozenVelocity.Serialize(bw);

            bw.Write(_useGravity);
            bw.Write(_isFrozen);
            bw.Write(_panelBounceEnabled);
            bw.Write(_isGrounded);
        }

        public override void Deserialize(BinaryReader br)
        {
            _velocity.Deserialize(br);
            _lastVelocity.Deserialize(br);
            _lastForceAdded.Deserialize(br);
            _frozenStoredForce.Deserialize(br);
            _frozenVelocity.Deserialize(br);

            _useGravity = br.ReadBoolean();
            _isFrozen = br.ReadBoolean();
            _panelBounceEnabled = br.ReadBoolean();
            _isGrounded = br.ReadBoolean();
        }

        protected override void Awake()
        {
            base.Awake();
            _movementBehaviour = GetComponent<GridMovementBehaviour>();

            _onForceAdded += args => GridActive = false;

            //If this is attached to an entity with a statemachine, they should always be grid active on Idle.
            if (TryGetComponent(out CharacterStateMachineBehaviour stateMachine))
            {
                stateMachine.AddOnStateChangedAction(SetGridActive);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (!_movementBehaviour)
                return;

            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
        }

        private void SetGridActive(string state)
        {
            if (state == "Idle")
            {
                GridActive = true;
            }
        }

        /// <summary>
        /// Makes it so that this object will bounce up once it hits the collision plane.
        /// </summary>
        /// <param name="useVelocityForBounce">Whether or not the strength of the bounce should be relative to the velocity. Uses default bounce value if false.</param>
        public void EnablePanelBounce(bool useVelocityForBounce = true)
        {
            _panelBounceEnabled = true;
            _useVelocityForBounce = useVelocityForBounce;
        }

        /// <summary>
        /// Stops this object from bouncing upwards when it collides with the collision plane.
        /// </summary>
        public void DisablePanelBounce()
        {
            _panelBounceEnabled = false;
        }

        /// <summary>
        /// Sets velocity and angular velocity to be zero
        /// </summary>
        public void StopVelocity()
        {
            _velocity = FVector3.Zero;
        }

        /// <summary>
        /// Set velocity and angular velocity to be zero and disables gravity.
        /// </summary>
        public void StopAllForces()
        {
            _velocity = FVector3.Zero;
            UseGravity = false;
        }

        /// <summary>
        /// If the object is being effected by non grid forces, 
        /// freeze the object in place for the given time.
        /// </summary>
        /// <param name="time">The amount of time in seconds to freeze in place.</param>
        public void FreezeInPlaceByTimer(float time, bool keepMomentum = false, bool makeKinematic = false, bool waitUntilForceApplied = false, bool storeForceApplied = false)
        {
            if (waitUntilForceApplied)
            {
                _onForceAddedTemp +=
                    args =>
                    {
                        FrozenStoredForce = (FVector3)args[0];
                        FreezeInPlaceByTimer(time, keepMomentum, makeKinematic, false, storeForceApplied);
                    };

                return;
            }

            if (_isFrozen)
                return;

            _isFrozen = true;

            if (_jumpSequence?.IsPlaying() == true)
                _jumpSequence?.Pause();

            bool gravityEnabled = UseGravity;
            FrozenVelocity = _velocity;

            if (makeKinematic && _isKinematic)
                makeKinematic = false;

            if (makeKinematic)
                _isKinematic = true;

            StopAllForces();
            _freezeAction = RoutineBehaviour.Instance.StartNewTimedAction(args => UnfreezeObject(makeKinematic, keepMomentum, gravityEnabled, storeForceApplied),
                                                                          TimedActionCountType.SCALEDTIME, time);
        }

        /// <summary>
        /// If the object is being effected by non grid forces, 
        /// freeze the object in place 
        /// </summary>
        /// <param name="condition">The condition event that will disable the freeze once true</param>
        /// <param name="keepMomentum">If true, the object will have its original velocity applied to it after being frozen</param>
        /// <param name="makeKinematic">If true, the object won't be able to have any forces applied to it during the freeze</param>
        public void FreezeInPlaceByCondition(Condition condition, bool keepMomentum = false, bool makeKinematic = false, bool waitUntilForceApplied = false, bool storeForceApplied = false)
        {
            if (waitUntilForceApplied)
            {
                _onForceAddedTemp +=
                    args =>
                    {
                        FrozenStoredForce = (FVector3)args[0];
                        FreezeInPlaceByCondition(condition, keepMomentum, makeKinematic, false, storeForceApplied);
                    };

                return;
            }

            if (_isFrozen)
                return;

            _isFrozen = true;
            bool gravityEnabled = UseGravity;
            FrozenVelocity = _velocity;

            if (_jumpSequence?.IsPlaying() == true)
                _jumpSequence?.Pause();

            if (makeKinematic && _isKinematic)
                makeKinematic = false;

            StopAllForces();
            if (makeKinematic)
                _isKinematic = true;

            _freezeAction = RoutineBehaviour.Instance.StartNewConditionAction(args => UnfreezeObject(makeKinematic, keepMomentum, gravityEnabled, storeForceApplied), condition);
        }

        public void SetFrozenMoveVectors(FVector3 frozenVelocity, FVector3 frozenForce)
        {
            FrozenVelocity = frozenVelocity;
            FrozenStoredForce = frozenForce;
        }

        /// <summary>
        /// Immediately enables movement again if the object is frozen
        /// </summary>
        private void UnfreezeObject(bool makeKinematic,bool keepMomentum, bool gravityEnabled, bool storeForceApplied)
        {
            if (_freezeAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_freezeAction);

            if (makeKinematic)
                _isKinematic = false;

            UseGravity = gravityEnabled;
            _isFrozen = false;

            if (_jumpSequence != null)
            {
                StopVelocity();
                _jumpSequence?.Pause();
                FrozenStoredForce = FVector3.Zero;
                FrozenVelocity = FVector3.Zero;
                return;
            }

            if (keepMomentum && FrozenVelocity.Magnitude > 0)
                ApplyVelocityChange(FrozenVelocity);

            if (storeForceApplied && FrozenStoredForce.Magnitude > 0)
                ApplyImpulseForce(FrozenStoredForce);

            FrozenStoredForce = FVector3.Zero;
            FrozenVelocity = FVector3.Zero;
        }

        /// <summary>
        /// Canceled the current freeze operation by stopping the timer and enabling gravity.
        /// Does not keep momentum or apply stored forces.
        /// </summary>
        public void CancelFreeze(out (FVector3,FVector3) moveVectors, bool keepMomentum = false, bool applyStoredForce = false, bool keepStoredForce = false)
        {
            if (_freezeAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_freezeAction);

            if (_jumpSequence?.IsPlaying() == true)
                _jumpSequence?.Kill();

            if (!IsGrounded)
                _isKinematic = false;

            UseGravity = true;
            _isFrozen = false;

            if (keepMomentum && FrozenVelocity.Magnitude > 0)
                ApplyVelocityChange(FrozenVelocity);

            if (applyStoredForce && FrozenStoredForce.Magnitude > 0)
                ApplyImpulseForce(FrozenStoredForce);

            FVector3 frozenVelocity = FrozenVelocity;
            FVector3 frozenForce = FrozenStoredForce;

            moveVectors = (frozenVelocity, frozenForce);

            if (keepStoredForce)
                return;

            FrozenStoredForce = FVector3.Zero;
            FrozenVelocity = FVector3.Zero;
        }

        /// <summary>
        /// Canceled the current freeze operation by stopping the timer and enabling gravity.
        /// Does not keep momentum or apply stored forces.
        /// </summary>
        public void CancelFreeze(bool keepMomentum = false, bool applyStoredForce = false)
        {
            if (_freezeAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_freezeAction);

            if (_jumpSequence?.IsPlaying() == true)
                _jumpSequence?.Kill();

            if (!IsGrounded)
                _isKinematic = false;

            UseGravity = true;
            _isFrozen = false;

            if (keepMomentum && FrozenVelocity.Magnitude > 1)
                ApplyVelocityChange(FrozenVelocity);

            if (applyStoredForce && FrozenStoredForce.Magnitude > 0)
                ApplyImpulseForce(FrozenStoredForce);

            FrozenStoredForce = FVector3.Zero;
            FrozenVelocity = FVector3.Zero;
        }

        /// <summary>
        /// Updates the panel position to the position the object will land.
        /// New position found after calculating the knockback force
        /// </summary>
        private void UpdatePanelPosition()
        {

            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out PanelBehaviour panel, false))
                _movementBehaviour.Position = panel.Position;
        }

        private float ClampForceMagnitude(float forceMagnitude, float launchAngle)
        {
            float newX = forceMagnitude;

            if (!_movementBehaviour)
                return forceMagnitude;

            //Do nothing if the angle makes it impossible to be out of the ring.
            if (Math.Abs(launchAngle) > 1.5f && _movementBehaviour.Alignment == GridAlignment.RIGHT)
                return newX;
            else if (Math.Abs(launchAngle) < 1.5f && _movementBehaviour.Alignment == GridAlignment.LEFT)
                return newX;

            //Find the position of the panel they would be on if the current force was applied.
            newX = _movementBehaviour.Position.X + forceMagnitude * -_movementBehaviour.GetAlignmentX();

            //Subtract from the force magnitude to clamp it based on alignement.
            if (_movementBehaviour.Alignment == GridAlignment.LEFT && newX < 0)
                forceMagnitude -= 0 - newX;
            else if (_movementBehaviour.Alignment == GridAlignment.RIGHT && newX > BlackBoardBehaviour.Instance.Grid.Dimensions.x - 1)
                forceMagnitude -= newX - BlackBoardBehaviour.Instance.Grid.Dimensions.x;

            return forceMagnitude;
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels
        /// </summary>
        /// <param name="forceMagnitude">How many panels will the object move assuming its mass is 1</param>
        /// <param name="launchAngle">The angle to launch the object</param>
        /// <param name="clampForceWithinRing">Whether or not the grid force could push the object out of the ring.</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public FVector3 CalculatGridForce(float forceMagnitude, float launchAngle, bool clampForceWithinRing = false)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;

            //If the knockback was too weak return an empty vector
            if (forceMagnitude <= 0)
                return new FVector3();

            if (clampForceWithinRing)
            {
                forceMagnitude = ClampForceMagnitude(forceMagnitude, launchAngle);
            }

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(launchAngle - (Mathf.PI / 2)) <= _rangeToIgnoreUpAngle)
                return FVector3.Up * Mathf.Sqrt(2 * Gravity * forceMagnitude + (forceMagnitude * BlackBoardBehaviour.Instance.Grid.PanelSpacingX));

            //Clamps hit angle to prevent completely horizontal movement
            //launchAngle = Mathf.Clamp(launchAngle, .2f, 3.0f);

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * forceMagnitude) + (panelSpacing * (forceMagnitude - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * Gravity;
            float val2 = Mathf.Sin(2 * launchAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
                return new FVector3();

            if (_maxMagnitude == null)
                return new FVector3();

            //Clamps magnitude to be within the limit
            magnitude = Mathf.Clamp(magnitude, 0, _maxMagnitude.Value);

            //Return the knockback force
            return new FVector3(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle), 0) * (magnitude * Mass);
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels
        /// </summary>
        /// <param name="forceMagnitude">How many panels will the object move assuming its mass is 1</param>
        /// <param name="launchAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public static FVector3 CalculatGridForce(float forceMagnitude, float launchAngle, float gravity = 9.81f, float mass = 1)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;

            //If the knockback was too weak return an empty vector
            if (forceMagnitude <= 0)
                return new FVector3();

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(launchAngle - (Mathf.PI / 2)) <= 0.2f)
                return FVector3.Up * Mathf.Sqrt(2 * gravity * forceMagnitude + (forceMagnitude * BlackBoardBehaviour.Instance.Grid.PanelSpacingX));

            //Clamps hit angle to prevent completely horizontal movement
            //launchAngle = Mathf.Clamp(launchAngle, .2f, 3.0f);

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * forceMagnitude) + (panelSpacing * (forceMagnitude - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * gravity;
            float val2 = Mathf.Sin(2 * launchAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
                return new FVector3();

            //Return the knockback force
            return new FVector3(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle), 0) * (magnitude * mass);
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels
        /// </summary>
        /// <param name="forceMagnitude">How many panels will the object move assuming its mass is 1</param>
        /// <param name="launchAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public FVector3 CalculatGridForce(float forceMagnitude, float launchAngle, float gravity = 9.81f, float mass = 1, bool clampForceWithinRing = false)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;

            //If the knockback was too weak return an empty vector
            if (forceMagnitude <= 0)
                return new FVector3();

            if (clampForceWithinRing)
                forceMagnitude = ClampForceMagnitude(forceMagnitude, launchAngle);

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(launchAngle - (Mathf.PI / 2)) <= 0.2f)
                return FVector3.Up * Mathf.Sqrt(2 * gravity * forceMagnitude + (forceMagnitude * BlackBoardBehaviour.Instance.Grid.PanelSpacingX));

            //Clamps hit angle to prevent completely horizontal movement
            //launchAngle = Mathf.Clamp(launchAngle, .2f, 3.0f);

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * forceMagnitude) + (panelSpacing * (forceMagnitude - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * gravity;
            float val2 = Mathf.Sin(2 * launchAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
                return new FVector3();

            //Return the knockback force
            return new FVector3(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle), 0) * (magnitude * mass);
        }

        /// <summary>
        /// Adds an event to the event called when this object collides with another.
        /// </summary>
        /// <param name="collisionEvent">The delegate to invoke upon collision</param>
        public void AddOnCollisionEvent(CollisionEvent collisionEvent)
        {
            _onCollision += collisionEvent;
        }

        /// <summary>
        /// Adds a method to the event called when a force is applied to this object.
        /// </summary>
        /// <param name="forceEvent">The delegate to invoke upon collision</param>
        public void AddOnForceAddedEvent(ForceAddedEvent forceEvent)
        {
            _onForceAdded += forceEvent;
        }

        /// <summary>
        /// Adds a method to the event called when a force is applied to this object.
        /// </summary>
        /// <param name="forceEvent">The delegate to invoke upon collision</param>
        public void AddOnForceAddedTempEvent(ForceAddedEvent forceEvent)
        {
            _onForceAddedTemp += forceEvent;
        }

        /// <summary>
        /// Adds an event to the event called when this object collides lands on a structure.
        /// </summary>
        /// <param name="collisionEvent">The delegate to invoke upon collision</param>
        public void AddOnCollisionWithGroundEvent(CollisionEvent collisionEvent)
        {
            _onCollisionWithGround += collisionEvent;
        }

        public  void ResolveCollision(Collision collision)
        {
            EntityData otherEntity = collision.Collider.Owner;

            HealthBehaviour damageScript = otherEntity.GetComponent<HealthBehaviour>();
            GridPhysicsBehaviour gridPhysicsBehaviour = otherEntity.GetComponent<GridPhysicsBehaviour>();

            _onCollision?.Invoke(collision);
            float bounceDampening = BounceDampen;

            if (!gridPhysicsBehaviour || !damageScript)
            {
                CollisionPlaneBehaviour collisionPlane = otherEntity.GetComponent<CollisionPlaneBehaviour>();
                if (!collisionPlane || !PanelBounceEnabled)
                    return;

                gridPhysicsBehaviour = this;
                bounceDampening = collisionPlane.BounceDampening;
            }

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to add force
            if (!knockBackScript)
                knockBackScript = GetComponent<KnockbackBehaviour>();

            //Calculate the knockback and hit angle for the ricochet
            Vector3 direction = new(collision.Normal.X, collision.Normal.Y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float baseKnockBack = 1;



            float velocityMagnitude;
            if (knockBackScript && _useVelocityForBounce)
            {
                velocityMagnitude = knockBackScript.Physics.Velocity.Magnitude;
                baseKnockBack = knockBackScript.LaunchVelocity.Magnitude / velocityMagnitude + bounceDampening;
            }
            else if (_useVelocityForBounce)
            {
                velocityMagnitude = Velocity.Magnitude;
                baseKnockBack = _lastForceAdded.Magnitude / velocityMagnitude + bounceDampening;
            }

            if (baseKnockBack == 0 || float.IsNaN(baseKnockBack))
                return;

            float bounce = 0;

            switch (_bounceCombination)
            {
                case BounceCombination.AVERAGE:
                    bounce = (Bounciness + gridPhysicsBehaviour.Bounciness) / 2;
                    break;
                case BounceCombination.MULTIPLY:
                    bounce = gridPhysicsBehaviour == this ? Bounciness : Bounciness * gridPhysicsBehaviour.Bounciness;
                    break;
                case BounceCombination.MINIMUM:
                    bounce = Bounciness < gridPhysicsBehaviour.Bounciness ? Bounciness : gridPhysicsBehaviour.Bounciness;
                    break;
                case BounceCombination.MAXIMUM:
                    bounce = Bounciness > gridPhysicsBehaviour.Bounciness ? Bounciness : gridPhysicsBehaviour.Bounciness;
                    break;
            }

            //Apply ricochet force
            gridPhysicsBehaviour.ApplyVelocityChange(CalculatGridForce(baseKnockBack * bounce, hitAngle, true));
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="force">The new velocity for the object.</param>
        public void ApplyVelocityChange(FVector3 force, bool disableMovement = false)
        {
            if (IgnoreForces)
                return;

            GridActive = false;

            //Try to snap the object to a panel center before applying force.
            if (_movementBehaviour?.IsMoving == true)
            {
                //Cancel movement and then move.
                _movementBehaviour.CanCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.CanCancelMovement = false;
                
            }

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            //Store force data for later use.
            _lastForceAdded = force;
            ForceToApply = force;

            //Forces should be flipped upwards if applied directly downwards on a grounded object.
            if (_panelBounceEnabled && IsGrounded && force.Y < 0)
                force.Y *= -1;

            //Apply force if we can.
            if (IsFrozen)
                FrozenVelocity = _lastForceAdded;
            else
                _velocity = force;

            //Handle force events.
            _onForceAdded?.Invoke(force);
            _onForceAddedTemp?.Invoke(force);
            _onForceAddedTemp = null;
        }

        /// <summary>
        /// Adds a force to the object using it's mass.
        /// </summary>
        /// <param name="force">The force to apply to the object.</param>
        /// <param name="disableMovement">Will tell the movement component to disable movement if true.</param>
        /// <param name="ignoreMomentum">If true, will stop the velocity of the object entirely before adding the force.</param>
        public void ApplyForce(FVector3 force, bool disableMovement = false, bool ignoreMomentum = false)
        {
            if (IgnoreForces)
                return;

            if (ignoreMomentum)
                StopVelocity();

            GridActive = false;

            //Try to snap the object to a panel center before applying force.
            if (_movementBehaviour?.IsMoving == true)
            {
                _movementBehaviour.CanCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.CanCancelMovement = false;

                if (disableMovement)
                    _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);
            }

            //If a new force is added in the opposite direction, this will instantly flip that value.
            //This makes the object snap to the other direction and feels more responsive.
            float xDot = FVector2.Dot(new FVector2(force.X, 0).GetNormalized(), new FVector2(Velocity.X,0).GetNormalized());
            float yDot = FVector2.Dot(new FVector2(0, force.Y).GetNormalized(), new FVector2(0, Velocity.Y).GetNormalized());
            if (xDot < 0)
                _velocity = new FVector3(0, _velocity.Y, _velocity.Z);
            if (yDot < 0)
                _velocity = new FVector3(_velocity.X, 0, _velocity.Z);

            //Cache force data for later use.
            _lastForceAdded = force / Mass;
            ForceToApply = _lastForceAdded;

            //Forces should be flipped upwards if applied directly downwards on a grounded object.
            if (_panelBounceEnabled && IsGrounded && force.Y < 0)
                force.Y *= -1;

            //Apply the force.
            if (IsFrozen)
                FrozenVelocity = _lastForceAdded;
            else
                _velocity += (force / Mass) * GridGame.FixedTimeStep;

            //Handle force events.
            _onForceAdded?.Invoke(force / Mass);
            _onForceAddedTemp?.Invoke(force);
            _onForceAddedTemp = null;

        }

        /// <summary>
        /// Adds an instant force impulse using the objects mass.
        /// Disables movement if not in hitstun.
        /// </summary>
        /// <param name="force">The force to apply to the object.</param>
        public void ApplyImpulseForce(FVector3 force, bool disableMovement = false, bool ignoreMomentum = false)
        {
            if (IgnoreForces)
                return;


            if (ignoreMomentum)
                StopVelocity();

            if (_movementBehaviour?.IsMoving == true)
            {
                _movementBehaviour.CanCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.CanCancelMovement = false;

                if (disableMovement)
                    _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);
            }

            GridActive = false;
            
            //If a new force is added in the opposite direction, this will instantly flip that value.
            //This makes the object snap to the other direction and feels more responsive.
            float xDot = FVector2.Dot(new FVector2(force.X, 0).GetNormalized(), new FVector2(Velocity.X, 0).GetNormalized());
            float yDot = FVector2.Dot(new FVector2(0, force.Y).GetNormalized(), new FVector2(0, Velocity.Y).GetNormalized());
            if (xDot < 0)
                _velocity = new FVector3(0, _velocity.Y, _velocity.Z);
            if (yDot < 0)
                _velocity = new FVector3(_velocity.X, 0, _velocity.Z);

            //Forces should be flipped upwards if applied directly downwards on a grounded object.
            if (IsGrounded && force.Y < 0)
                force.Y *= -1f;

            _lastForceAdded = force / Mass;
            ForceToApply = _lastForceAdded;

            if (IsFrozen)
                FrozenVelocity = _lastForceAdded;
            else
                _velocity += force / Mass;
            

            if (IsFrozen)
                FrozenStoredForce = _lastForceAdded;

            _onForceAdded?.Invoke(force / Mass);
            _onForceAddedTemp?.Invoke(force);
            _onForceAddedTemp = null;
            _acceleration = (FVector3)force / Mass;
        }

        /// <summary>
        /// Interpolates from the objects current position to a panel at the given distance while adding a jump effect on the y.
        /// </summary>
        /// <param name="panelDistance">How many panels far the object will jump</param>
        /// <param name="height">The maximum height of the jump</param>
        /// <param name="duration">The amount of time the jump will last</param>
        /// <param name="jumpToClosestAvailablePanel">If true, the object will try to jump to a closer panel if the destination isn't available</param>
        /// <param name="canBeOccupied">If true, the destination panel can be occupied by another object</param>
        /// <param name="alignment">The alignment of the panels this object is allowed to jump on</param>
        public void Jump(float height, int panelDistance,  float duration, bool jumpToClosestAvailablePanel = false, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY, FVector3 panelOffset = default, FixedAnimationCurve curve = null)
        {
            _jumpSequence?.Kill();

            if (curve == null)
                curve = new FixedAnimationCurve(_defaultJumpCurve);

            if (_isFrozen)
            {
                if (_bufferedJump?.IsActive == true)
                    _bufferedJump.Stop();

                _bufferedJump = FixedPointTimer.StartNewConditionAction(
                    () => Jump(height, panelDistance, duration, jumpToClosestAvailablePanel, canBeOccupied, alignment, panelOffset, curve),
                    condition => !_isFrozen);

                return;
            }

            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;
            float displacement = (panelSize * panelDistance) + (panelSpacing * (panelDistance - 1));
            //Try to find a panel at the location
            FVector3 panelPosition = FixedTransform.WorldPosition + FixedTransform.Forward * panelDistance;
            BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld((Vector3)panelPosition, out PanelBehaviour panel, canBeOccupied, alignment);

            //Returns if a panel couldn't be found and we don't want to keep looking
            if (!panel && !jumpToClosestAvailablePanel) return;


            //Looks for a panel to land on
            for (int i = panelDistance - 1; i > 0; i--)
            {
                BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld((Vector3)panelPosition, out panel, canBeOccupied, alignment);

                if (panel) break;
            }

            //Perform the jump
            _jumpSequence = FixedLerp.DoJump(FixedTransform, panelPosition + panelOffset, height, 1, duration, curve);
            _jumpSequence.onKill += () => _jumpSequence = null;
            _jumpSequence.onComplete += () => _jumpSequence = null;

            //Cancel the jump if a force is added
            _onForceAdded += args =>
            {
                _jumpSequence?.Kill();
            };
        }

        /// <summary>
        /// Whether or not this object is touching the ground
        /// </summary>
        /// <returns></returns>
        private bool CheckIsGrounded()
        {
            bool grounded = false;

            Collider[] hits = Physics.OverlapBox((Vector3)GroundedBoxPosition, (Vector3)GroundedBoxExtents, new Quaternion(), LayerMask.GetMask(new string[] { "Structure"}));

            foreach (Collider collider in hits)
            {
                var position = transform.position;
                Vector3 closestPoint = collider.ClosestPoint(position);
                float normalY = (position - closestPoint).normalized.y;
                normalY = Mathf.Ceil(normalY);
                if (normalY == 1)
                {
                    if (!_isGrounded) _onCollisionWithGround?.Invoke(default);
                    grounded = true;
                }
            }
            
            return grounded;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * GroundedBoxExtents.Y);
        }

        /// <summary>
        /// Get the current panel coordinate for this object. 
        /// If there isn't a movement behaviour attached, it will find it based on location.
        /// </summary>
        public FVector2 GetGridPosition()
        {
            FVector2 position = new();

            if (!_movementBehaviour)
            {
                GridBehaviour.Grid.GetPanelAtLocationInWorld(transform.position, out PanelBehaviour panel);

                if (panel != null)
                {
                    position.X = panel.Position.X;
                    position.Y = panel.Position.Y;
                }
            }
            else
            {
                position.X = _movementBehaviour.Position.X;
                position.Y = _movementBehaviour.Position.Y;
            }

            return position;
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);
            _isGrounded = CheckIsGrounded();

            //Disable all forces if this object is actively moving on the grid or kinematic.
            if (IsKinematic || GridActive)
            {
                if (_movementBehaviour?.IsMoving == true)
                    _velocity = _movementBehaviour.MoveDirection;
                else
                    _velocity = FVector3.Zero;

                _acceleration = FVector3.Zero;
                _objectAtRest = true;

                return;
            }

            //Code that was ran in unity update.
            if (FaceHeading && Velocity.Magnitude > 0)
                FixedTransform.Forward = Velocity.GetNormalized();
            
            //Code that ran in unity fixed update.
            _acceleration = (_lastVelocity - Velocity) / Time.fixedDeltaTime;

            if (UseGravity && !IgnoreForces && !IsGrounded)
                _velocity += new FVector3(0, -Gravity, 0);

            _objectAtRest = IsGrounded && _velocity.Magnitude <= 0.01f;

            ForceToApply = FVector3.Zero;

            FixedTransform.WorldPosition += Velocity * dt;
        }
    }
}