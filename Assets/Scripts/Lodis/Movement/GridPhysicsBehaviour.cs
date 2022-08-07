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

namespace Lodis.Movement
{
    public delegate void ForceAddedEvent(params object[] args);

    [RequireComponent(typeof(GridMovementBehaviour))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ConstantForce))]
    public class GridPhysicsBehaviour : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        [Tooltip("How much mass the game object has. The higher the number, the less panels it travels when knocked back.")]
        [SerializeField]
        private float _mass = 1;
        private bool _objectAtRest;
        private Vector3 _acceleration;
        private Vector3 _lastVelocity;
        [Tooltip("The strength of the force pushing downwards on this object once in air")]
        [SerializeField]
        private float _gravity = 9.81f;
        [Tooltip("How much this object will reduce the velocity of objects that bounce off of it.")]
        [SerializeField]
        private float _bounceDampen = 2;
        private ConstantForce _constantForceBehaviour;
        [Tooltip("The collider attached this object that will be used for registering collision against objects while air")]
        [SerializeField]
        private Collider _bounceCollider;
        [SerializeField]
        private bool _isGrounded;
        [SerializeField]
        private float _extraHeight = 0.5f;
        private Vector3 _groundedBoxPosition;
        private Vector3 _groundedBoxExtents;
        [SerializeField]
        private float _bounciness = 0.8f;
        [SerializeField]
        private bool _panelBounceEnabled;
        private Vector3 _normalForce;
        [SerializeField]
        private bool _useGravity = true;
        [Tooltip("If true, this object will ignore all forces acting on it including gravity")]
        [SerializeField]
        private bool _ignoreForces;
        [Tooltip("Any angles for knock back force recieved in this range will send the object directly upwards")]
        [SerializeField]
        private float _rangeToIgnoreUpAngle = 0.2f;
        [Tooltip("How fast will objects be allowed to travel in knockback")]
        [SerializeField]
        private ScriptableObjects.FloatVariable _maxMagnitude;
        [SerializeField]
        private bool _faceHeading;
        private Vector3 _lastForceAdded;
        private CustomYieldInstruction _wait;
        private GridMovementBehaviour _movementBehaviour;

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


        private Coroutine _currentCoroutine;
        private Sequence _jumpSequence;
        private bool _isFrozen;
        private bool _useVelocityForBounce;
        private Vector3 _frozenStoredForce;
        private Vector3 _frozenVelocity;
        private DelayedAction _freezeAction;
        private ConditionAction _bufferedJump;

        /// <summary>
        /// Whether or not this object will bounce on panels it falls on
        /// </summary>
        public bool PanelBounceEnabled { get =>_panelBounceEnabled; }

        /// <summary>
        /// How bouncy this object is
        /// </summary>
        public float Bounciness { get => _bounciness; set { _bounciness = value; } }

        public float Gravity { get =>_gravity; set => _gravity = value; }

        public float Mass { get => _mass; }

        /// <summary>
        /// Whether or not this object should be effected by gravity
        /// </summary>
        public bool UseGravity { get => _useGravity; set =>_useGravity = value; }

        /// <summary>
        /// Returns the velocity of the rigid body in the last fixed update
        /// </summary>
        public Vector3 LastVelocity { get => _lastVelocity; }

        public Collider BounceCollider { get => _bounceCollider; }

        public bool IsGrounded{ get => _isGrounded; }

        public Vector3 Acceleration { get => _acceleration; }
        public Rigidbody RB { get => _rigidbody; }
        public Vector3 GroundedBoxPosition { get => _groundedBoxPosition; set => _groundedBoxPosition = value; }
        public Vector3 GroundedBoxExtents { get => _groundedBoxExtents; set => _groundedBoxExtents = value; }
        public float BounceDampen { get => _bounceDampen; set => _bounceDampen = value; }
        public bool ObjectAtRest { get => _objectAtRest; }
        public bool IgnoreForces { get => _ignoreForces; set => _ignoreForces = value; }
        public GridMovementBehaviour MovementBehaviour { get => _movementBehaviour; }

        public bool IsFrozen => _isFrozen;

        public bool FaceHeading { get => _faceHeading; set => _faceHeading = value; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            RB.isKinematic = true;
            RB.useGravity = false;
            _constantForceBehaviour = GetComponent<ConstantForce>();
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _movementBehaviour.AddOnMoveBeginAction(() =>
            { RB.isKinematic = true; });
            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
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
        /// True if the rigidbody is sleeping.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool RigidbodyInactive(object[] args = null)
        {
            return RB.IsSleeping();
        }

        /// <summary>
        /// Sets velocity and angular velocity to be zero
        /// </summary>
        public void StopVelocity()
        {
            RB.velocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;
        }
        /// <summary>
        /// Makes the object kinematic
        /// </summary>
        public void MakeKinematic()
        {
            RB.isKinematic = true;
        }

        /// <summary>
        /// Set velocity and angular velocity to be zero and disables gravity.
        /// </summary>
        public void StopAllForces()
        {
            RB.velocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;
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
                        _frozenStoredForce = (Vector3)args[0];
                        FreezeInPlaceByTimer(time, keepMomentum, makeKinematic, false, storeForceApplied);
                    };

                return;
            }

            if (_isFrozen)
                return;

            _isFrozen = true;

            if (_jumpSequence?.IsPlaying() == true)
                _jumpSequence?.TogglePause();

            bool gravityEnabled = UseGravity;
            _frozenVelocity = _rigidbody.velocity;

            if (makeKinematic && _rigidbody.isKinematic)
                makeKinematic = false;

            if (makeKinematic)
                MakeKinematic();

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
                        _frozenStoredForce = (Vector3)args[0];
                        FreezeInPlaceByCondition(condition, keepMomentum, makeKinematic, false, storeForceApplied);
                    };

                return;
            }

            if (_isFrozen)
                return;

            _isFrozen = true;
            bool gravityEnabled = UseGravity;
            _frozenVelocity = _rigidbody.velocity;
            if (_jumpSequence?.active == true)
                _jumpSequence?.TogglePause();

            if (makeKinematic && _rigidbody.isKinematic)
                makeKinematic = false;

            if (makeKinematic)
                MakeKinematic();

            StopAllForces();
            _freezeAction = RoutineBehaviour.Instance.StartNewConditionAction(args => UnfreezeObject(makeKinematic, keepMomentum, gravityEnabled, storeForceApplied), condition);
        }

        /// <summary>
        /// Immediately enables movement again if the object is frozen
        /// </summary>
        private void UnfreezeObject(bool makeKinematic,bool keepMomentum, bool gravityEnabled, bool storeForceApplied)
        {
            if (_freezeAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_freezeAction);

            if (makeKinematic)
                RB.isKinematic = false;

            UseGravity = gravityEnabled;
            _isFrozen = false;

            if (_jumpSequence != null)
            {
                StopVelocity();
                _jumpSequence?.TogglePause();
                _frozenStoredForce = Vector3.zero;
                _frozenVelocity = Vector3.zero;
                return;
            }

            if (keepMomentum && _frozenVelocity.magnitude > 1)
                ApplyVelocityChange(_frozenVelocity);

            if (storeForceApplied && _frozenStoredForce.magnitude > 0)
                ApplyImpulseForce(_frozenStoredForce);

            _frozenStoredForce = Vector3.zero;
            _frozenVelocity = Vector3.zero;
        }

        /// <summary>
        /// Canceled the current freeze operation by stopping the timer and enabling gravity.
        /// Does not keep momentum or apply stored forces.
        /// </summary>
        public void CancelFreeze()
        {
            if (_freezeAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_freezeAction);

            if (_jumpSequence?.active == true)
                _jumpSequence?.Kill();

            UseGravity = true;
            _isFrozen = false;
            _frozenStoredForce = Vector3.zero;
            _frozenVelocity = Vector3.zero;
        }

        /// <summary>
        /// Updates the panel position to the position the object will land.
        /// New position found after calculating the knockback force
        /// </summary>
        private void UpdatePanelPosition()
        {
            GridScripts.PanelBehaviour panel = null;

            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel, false))
                _movementBehaviour.Position = panel.Position;
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels
        /// </summary>
        /// <param name="forceMagnitude">How many panels will the object move assuming its mass is 1</param>
        /// <param name="launchAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public Vector3 CalculatGridForce(float forceMagnitude, float launchAngle)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;

            //If the knockback was too weak return an empty vector
            if (forceMagnitude <= 0)
                return new Vector3();

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(launchAngle - (Mathf.PI / 2)) <= _rangeToIgnoreUpAngle)
                return Vector3.up * Mathf.Sqrt(2 * Gravity * forceMagnitude + (forceMagnitude * BlackBoardBehaviour.Instance.Grid.PanelSpacingX));

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
                return new Vector3();

            if (_maxMagnitude == null)
                return new Vector3();

            //Clamps magnitude to be within the limit
            magnitude = Mathf.Clamp(magnitude, 0, _maxMagnitude.Value);

            //Return the knockback force
            return new Vector3(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle)) * (magnitude * Mass);
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels
        /// </summary>
        /// <param name="forceMagnitude">How many panels will the object move assuming its mass is 1</param>
        /// <param name="launchAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public static Vector3 CalculatGridForce(float forceMagnitude, float launchAngle, float gravity = 9.81f, float mass = 1)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;

            //If the knockback was too weak return an empty vector
            if (forceMagnitude <= 0)
                return new Vector3();

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(launchAngle - (Mathf.PI / 2)) <= 0.2f)
                return Vector3.up * Mathf.Sqrt(2 * gravity * forceMagnitude + (forceMagnitude * BlackBoardBehaviour.Instance.Grid.PanelSpacingX));

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
                return new Vector3();

            //Return the knockback force
            return new Vector3(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle)) * (magnitude * mass);
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

        private void OnTriggerEnter(Collider other)
        {
            GameObject otherGameObject;
            HealthBehaviour damageScript = (otherGameObject = other.gameObject).GetComponent<HealthBehaviour>();

            _onCollision?.Invoke(otherGameObject, other.gameObject.GetComponent<ColliderBehaviour>(), other, GetComponent<Collider>(), damageScript);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {

            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();
            GridPhysicsBehaviour gridPhysicsBehaviour = collision.gameObject.GetComponent<GridPhysicsBehaviour>();

            _onCollision?.Invoke(collision.gameObject, collision.gameObject.GetComponent<ColliderBehaviour>(), collision, GetComponent<Collider>(), damageScript);

            CollisionPlaneBehaviour collisionPlane = null;
            float bounceDampening = BounceDampen;

            if (!gridPhysicsBehaviour || !damageScript)
            {
                collisionPlane = collision.gameObject.GetComponent<CollisionPlaneBehaviour>();
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
            ContactPoint contactPoint = collision.GetContact(0);
            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = 0;
            float baseKnockBack = 1;

            if (knockBackScript && _useVelocityForBounce)
            {
                velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;
                baseKnockBack = knockBackScript.LaunchVelocity.magnitude / velocityMagnitude + bounceDampening;
            }
            else if (_useVelocityForBounce)
            {
                velocityMagnitude = LastVelocity.magnitude;
                baseKnockBack = _lastForceAdded.magnitude / velocityMagnitude + bounceDampening;
            }

            if (baseKnockBack == 0 || float.IsNaN(baseKnockBack))
                return;

            //Apply ricochet force
            gridPhysicsBehaviour.ApplyImpulseForce(CalculatGridForce(baseKnockBack * gridPhysicsBehaviour.Bounciness, hitAngle));
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyVelocityChange(Vector3 force, bool disableMovement = false)
        {
            if (IgnoreForces)
                return;

            RB.isKinematic = false;

            if (_movementBehaviour.IsMoving)
            {
                _movementBehaviour.canCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.canCancelMovement = false;
            }

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            RB.AddForce(force, ForceMode.VelocityChange);
            _lastVelocity = force;
            _lastForceAdded = force;

            if (IsFrozen)
                _frozenVelocity = _lastForceAdded;

            _onForceAdded?.Invoke(force);
            _onForceAddedTemp?.Invoke(force);
            _onForceAddedTemp = null;
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyForce(Vector3 force, bool disableMovement = false)
        {
            if (IgnoreForces)
                return;

            RB.isKinematic = false;

            if (_movementBehaviour.IsMoving)
            {
                _movementBehaviour.canCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.canCancelMovement = false;
            }

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            RB.AddForce(force / Mass, ForceMode.Force);
            _lastForceAdded = force / Mass;

            if (IsFrozen)
                _frozenStoredForce = _lastForceAdded;

            _onForceAdded?.Invoke(force / Mass);
            _onForceAddedTemp?.Invoke(force);
            _onForceAddedTemp = null;

        }

        /// <summary>
        /// Adds an instant force impulse using the objects mass.
        /// Disables movement if not in hitstun.
        /// </summary>
        /// <param name="force">The force to apply to the object.</param>
        public void ApplyImpulseForce(Vector3 force, bool disableMovement = false)
        {
            if (IgnoreForces)
                return;

            if (_movementBehaviour.IsMoving)
            {
                _movementBehaviour.canCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.canCancelMovement = false;
            }

            RB.isKinematic = false;

            _objectAtRest = false;

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            RB.AddForce(force / Mass, ForceMode.Impulse);
            

            _lastForceAdded = force / Mass;

            if (IsFrozen)
                _frozenStoredForce = _lastForceAdded;

            _onForceAdded?.Invoke(force / Mass);
            _onForceAddedTemp?.Invoke(force);
            _onForceAddedTemp = null;
            _acceleration = force / Mass;
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
        public void Jump(int panelDistance, float height, float duration, bool jumpToClosestAvailablePanel = false, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY, Vector3 panelOffset = default(Vector3), Ease ease = Ease.InOutSine)
        {
            _jumpSequence?.Kill();

            if (_isFrozen)
            {
                if (_bufferedJump?.GetEnabled() == true)
                    RoutineBehaviour.Instance.StopAction(_bufferedJump);

                _bufferedJump = RoutineBehaviour.Instance.StartNewConditionAction(
                    args => Jump(panelDistance, height, duration, jumpToClosestAvailablePanel, canBeOccupied, alignment, panelOffset, ease),
                    condition => !_isFrozen);

                return;
            }

            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;
            float displacement = (panelSize * panelDistance) + (panelSpacing * (panelDistance - 1));
            RB.isKinematic = false;
            //Try to find a panel at the location
            PanelBehaviour panel;
            Vector3 panelPosition = transform.position + transform.forward * panelDistance;
            BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(panelPosition, out panel, canBeOccupied, alignment);

            //Returns if a panel couldn't be found and we don't want to keep looking
            if (!panel && !jumpToClosestAvailablePanel) return;


            //Looks for a panel to land on
            for (int i = panelDistance - 1; i > 0; i--)
            {
                BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(panelPosition, out panel, canBeOccupied, alignment);

                if (panel) break;
            }

            //Perform the jump
            _jumpSequence = _rigidbody.DOJump(panelPosition + panelOffset, height, 1, duration).SetEase(ease);
            _jumpSequence.onKill += () => _jumpSequence = null;
            _jumpSequence.onComplete += () => _jumpSequence = null;

            //Cancel the jump if a force is added
            _onForceAdded += args =>
            {
                _jumpSequence?.Kill();
            };
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
        public void Jump(int panelDistance, float height, float duration, bool jumpToClosestAvailablePanel = false, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY, Vector3 panelOffset = default(Vector3), AnimationCurve curve = null)
        {
            _jumpSequence?.Kill();

            if (_isFrozen)
            {
                if (_bufferedJump?.GetEnabled() == true)
                    RoutineBehaviour.Instance.StopAction(_bufferedJump);

                _bufferedJump = RoutineBehaviour.Instance.StartNewConditionAction(
                    args => Jump(panelDistance, height, duration, jumpToClosestAvailablePanel, canBeOccupied, alignment, panelOffset, curve),
                    condition => !_isFrozen);

                return;
            }

            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacingX;
            float displacement = (panelSize * panelDistance) + (panelSpacing * (panelDistance - 1));
            RB.isKinematic = false;
            //Try to find a panel at the location
            PanelBehaviour panel;
            Vector3 panelPosition = transform.position + transform.forward * panelDistance;
            BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(panelPosition, out panel, canBeOccupied, alignment);

            //Returns if a panel couldn't be found and we don't want to keep looking
            if (!panel && !jumpToClosestAvailablePanel) return;


            //Looks for a panel to land on
            for (int i = panelDistance - 1; i > 0; i--)
            {
                BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(panelPosition, out panel, canBeOccupied, alignment);

                if (panel) break;
            }

            //Perform the jump
            _jumpSequence = _rigidbody.DOJump(panelPosition + panelOffset, height, 1, duration).SetEase(curve);
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

            Collider[] hits = Physics.OverlapBox(GroundedBoxPosition, GroundedBoxExtents, new Quaternion(), LayerMask.GetMask(new string[] { "Structure"}));

            foreach (Collider collider in hits)
            {
                var position = transform.position;
                Vector3 closestPoint = collider.ClosestPoint(position);
                float normalY = (position - closestPoint).normalized.y;
                normalY = Mathf.Ceil(normalY);
                if (normalY == 1)
                {
                    if (!_isGrounded) _onCollisionWithGround?.Invoke();
                    grounded = true;
                }
            }
            
            return grounded;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * GroundedBoxExtents.y);
        }

        private void FixedUpdate()
        {
            _acceleration = (RB.velocity - LastVelocity) / Time.fixedDeltaTime;

            _lastVelocity = RB.velocity;
            _lastVelocity = RB.velocity;

            if (UseGravity && !IgnoreForces)
                _constantForceBehaviour.force = new Vector3(0, -Gravity, 0);
            else
                _constantForceBehaviour.force = Vector3.zero;

            _isGrounded = CheckIsGrounded();
            _objectAtRest = IsGrounded && _rigidbody.velocity.magnitude <= 0.01f;
        }

        private void Update()
        {
            if (FaceHeading && LastVelocity.magnitude > 0)
                transform.forward = LastVelocity.normalized;
        }
    }
}