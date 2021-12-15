using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Lodis.Movement;

namespace Lodis.Movement
{

    [RequireComponent(typeof(GridMovementBehaviour))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ConstantForce))]
    public class GridPhysicsBehaviour : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        [Tooltip("How much mass the game object has. The higher the number, the less panels it travels when knocked back.")]
        [SerializeField]
        private float _mass;
        private bool _objectAtRest;
        private Vector3 _acceleration;
        private Vector3 _lastVelocity;
        [Tooltip("The rate at which an objects move speed in air will decrease")]
        [SerializeField]
        private FloatVariable _velocityDecayRate;
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
        private bool _panelBounceEnabled = true;
        private Vector3 _normalForce;
        [SerializeField]
        private Vector3 _force;
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
        private Vector3 _lastForceAdded;
        private CustomYieldInstruction _wait;
        private GridMovementBehaviour _movementBehaviour;

        private Coroutine _currentCoroutine;
        [SerializeField]
        private float _bounceScale = 1;

        /// <summary>
        /// Whether or not this object will bounce on panels it falls on
        /// </summary>
        public bool PanelBounceEnabled { get =>_panelBounceEnabled; set => _panelBounceEnabled = value; }

        /// <summary>
        /// How bouncy this object is
        /// </summary>
        public float Bounciness { get => _bounciness; }

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

        /// <summary>
        /// The event called when this object collides with another
        /// </summary>
        public CollisionEvent OnCollision { private get; set; }

        public Vector3 Acceleration { get => _acceleration; }
        public Rigidbody Rigidbody { get => _rigidbody; }
        public Vector3 GroundedBoxPosition { get => _groundedBoxPosition; set => _groundedBoxPosition = value; }
        public Vector3 GroundedBoxExtents { get => _groundedBoxExtents; set => _groundedBoxExtents = value; }
        public float BounceDampen { get => _bounceDampen; set => _bounceDampen = value; }
        public bool ObjectAtRest { get => _objectAtRest; }
        public bool IgnoreForces { get => _ignoreForces; set => _ignoreForces = value; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;
            _constantForceBehaviour = GetComponent<ConstantForce>();
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _movementBehaviour.AddOnMoveEnabledAction(() =>
            { Rigidbody.isKinematic = true; });
            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
        }

        /// <summary>
        /// True if the rigidbody is sleeping.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool RigidbodyInactive(object[] args = null)
        {
            return Rigidbody.IsSleeping();
        }

        /// <summary>
        /// Sets velocity and angular velocity to be zero
        /// </summary>
        public void StopVelocity()
        {
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }
        /// <summary>
        /// Makes the object kinematic
        /// </summary>
        public void MakeKinematic()
        {
            Rigidbody.isKinematic = true;
        }

        /// <summary>
        /// Set velocity and angular velocity to be zero and disables gravity.
        /// </summary>
        public void StopAllForces()
        {
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            UseGravity = false;
        }

        /// <summary>
        /// Adds a force in the opposite direction of velocity to temporarily
        /// keep the object in place.
        /// </summary>
        /// <param name="time">The amount of time in seconds to freeze for.</param>
        /// <returns></returns>
        private IEnumerator FreezeTimerCoroutine(float time, bool keepMomentum = false, bool makeKinematic = false)
        {
            bool gravityEnabled = UseGravity;
            Vector3 velocity = LastVelocity;

            if (makeKinematic)
                MakeKinematic();

            StopAllForces();
            yield return new WaitForSeconds(time);

            if (makeKinematic)
                Rigidbody.isKinematic = false;

            if (keepMomentum)
                ApplyVelocityChange(velocity);

            UseGravity = gravityEnabled;
        }

        private IEnumerator FreezeConditionCoroutine(Condition condition, bool keepMomentum = false, bool makeKinematic = false)
        {
            bool gravityEnabled = UseGravity;
            Vector3 velocity = LastVelocity;

            float timeStarted = Time.time;
            float timeElapsed = 0;

            if (makeKinematic)
                MakeKinematic();

            StopAllForces();

            _wait = new WaitUntil(() => condition.Invoke());
            yield return _wait;

            if (makeKinematic)
                Rigidbody.isKinematic = false;

            if (keepMomentum)
                ApplyVelocityChange(velocity);

            UseGravity = gravityEnabled;
        }

        /// <summary>
        /// If the object is being effected by non grid forces, 
        /// freeze the object in place for the given time.
        /// </summary>
        /// <param name="time">The amount of time in seconds to freeze in place.</param>
        public void FreezeInPlaceByTimer(float time, bool keepMomentum = false, bool makeKinematic = false)
        {
            _currentCoroutine = StartCoroutine(FreezeTimerCoroutine(time, keepMomentum, makeKinematic));
        }

        /// <summary>
        /// If the object is being effected by non grid forces, 
        /// freeze the object in place 
        /// </summary>
        /// <param name="condition">The condition event that will disable the freeze once true</param>
        public void FreezeInPlaceByCondition(Condition condition, bool keepMomentum = false, bool makeKinematic = false)
        {
            _currentCoroutine = StartCoroutine(FreezeConditionCoroutine(condition, true));
        }

        /// <summary>
        /// Immediately enables movement again if the object is frozen
        /// </summary>
        public void UnfreezeObject()
        {
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);

            UseGravity = true;
        }

        /// <summary>
        /// Updates the panel position to the position the object will land.
        /// New position found after calculating the knockback force
        /// </summary>
        private void UpdatePanelPosition()
        {
            GridScripts.PanelBehaviour panel = null;

            if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel, false))
                _movementBehaviour.MoveToPanel(panel, false, GridScripts.GridAlignment.ANY);
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
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacing;

            //If the knockback was too weak return an empty vector
            if (forceMagnitude <= 0)
                return new Vector3();

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(launchAngle - (Mathf.PI / 2)) <= _rangeToIgnoreUpAngle)
                return Vector3.up * Mathf.Sqrt(2 * Gravity * forceMagnitude + (forceMagnitude * BlackBoardBehaviour.Instance.Grid.PanelSpacing));

            //Clamps hit angle to prevent completely horizontal movement
            launchAngle = Mathf.Clamp(launchAngle, .2f, 3.0f);

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * forceMagnitude) + (panelSpacing * (forceMagnitude - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * UnityEngine.Physics.gravity.magnitude;
            float val2 = Mathf.Sin(2 * launchAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
                return new Vector3();

            //Clamps magnitude to be within the limit
            magnitude = Mathf.Clamp(magnitude, 0, _maxMagnitude.Value);

            //Return the knockback force
            return new Vector3(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle)) * magnitude;
        }


        private void OnCollisionEnter(Collision collision)
        {
            OnCollision?.Invoke(collision.gameObject, collision);

            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();
            GridPhysicsBehaviour gridPhysicsBehaviour = collision.gameObject.GetComponent<GridPhysicsBehaviour>();

            if (!gridPhysicsBehaviour || !damageScript)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;


            //If no knockback script is attached, use this script to add force
            if (!knockBackScript)
                knockBackScript = GetComponent<KnockbackBehaviour>();

            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);
            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.LaunchVelocity.magnitude * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale))
                return;

            //Apply ricochet force
            gridPhysicsBehaviour.ApplyImpulseForce(CalculatGridForce(knockbackScale * _bounceScale / BounceDampen, hitAngle));
        }

        private void OnTriggerEnter(Collider other)
        {
            OnCollision?.Invoke(other.gameObject, other);

            HealthBehaviour damageScript = other.gameObject.GetComponent<HealthBehaviour>();
            GridPhysicsBehaviour gridPhysicsBehaviour = other.gameObject.GetComponent<GridPhysicsBehaviour>();

            if (!gridPhysicsBehaviour || !damageScript)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;


            //If no knockback script is attached, use this script to add force
            if (!knockBackScript)
                knockBackScript = GetComponent<KnockbackBehaviour>();

            //Calculate the knockback and hit angle for the ricochet
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            Vector3 direction = new Vector3(contactPoint.x, contactPoint.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = 0;
            float knockbackScale = 0;

            if (knockBackScript)
            {
                velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;
                knockbackScale = knockBackScript.LaunchVelocity.magnitude * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);
            }
            else
            {
                velocityMagnitude = LastVelocity.magnitude;
                knockbackScale = _lastForceAdded.magnitude * (velocityMagnitude / _lastForceAdded.magnitude);
            }

            if (knockbackScale == 0 || float.IsNaN(knockbackScale))
                return;

            //Apply ricochet force
            gridPhysicsBehaviour.ApplyImpulseForce(CalculatGridForce(knockbackScale * _bounceScale / BounceDampen, hitAngle));
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyVelocityChange(Vector3 force, bool disableMovement = false)
        {
            if (IgnoreForces)
                return;

            Rigidbody.isKinematic = false;

            if (_movementBehaviour.IsMoving)
            {
                _movementBehaviour.canCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.canCancelMovement = false;
            }

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            Rigidbody.AddForce(force, ForceMode.VelocityChange);
            _lastVelocity = force;
            _lastForceAdded = force;
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyForce(Vector3 force, bool disableMovement = false)
        {
            if (IgnoreForces)
                return;

            Rigidbody.isKinematic = false;

            if (_movementBehaviour.IsMoving)
            {
                _movementBehaviour.canCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.canCancelMovement = false;
            }

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            Rigidbody.AddForce(force, ForceMode.Force);
            _lastVelocity = force;
            _lastForceAdded = force;
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

            Rigidbody.isKinematic = false;

            _objectAtRest = false;

            if (disableMovement)
                _movementBehaviour.DisableMovement(condition => ObjectAtRest, false, true);

            Rigidbody.AddForce(force / Mass, ForceMode.Impulse);

            _lastVelocity = force;
            _lastForceAdded = force;
        }

        /// <summary>
        /// Whether or not this object is touching the ground
        /// </summary>
        /// <returns></returns>
        private bool CheckIsGrounded()
        {
            _isGrounded = false;

            //Collider[] hits = Physics.OverlapBox(GroundedBoxPosition, GroundedBoxExtents, new Quaternion(), LayerMask.GetMask(new string[] { "Structure", "Panels" }));

            //foreach (Collider collider in hits)
            //{
            //    Vector3 closestPoint = collider.ClosestPoint(transform.position);
            //    float normalY = (transform.position - closestPoint).normalized.y;
            //    normalY = Mathf.Ceil(normalY);
            //    if (normalY >= 0)
            //        _isGrounded = true;
            //}

            RaycastHit hit;
            bool hitRecieved = Physics.Raycast(GroundedBoxPosition, Vector3.down, out hit, GroundedBoxExtents.y, LayerMask.GetMask("Panels", "Structure"));

            _isGrounded = hitRecieved && (hit.collider.CompareTag("Panel") || hit.collider.CompareTag("CollisionPlane"));
            return _isGrounded;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * GroundedBoxExtents.y);
        }

        private void FixedUpdate()
        {
            _acceleration = (Rigidbody.velocity - LastVelocity) / Time.fixedDeltaTime;

            _lastVelocity = Rigidbody.velocity;

            if (Rigidbody.velocity.magnitude > 0)
                Rigidbody.velocity /= _velocityDecayRate.Value;

            if (CheckIsGrounded())
            {
                float yForce = 0;

                if (_lastVelocity.y < 0)
                    yForce = _lastVelocity.y;

                _normalForce = new Vector3(0, Gravity + yForce, 0);
            }
            else
                _normalForce = Vector3.zero;

            if (UseGravity && !IgnoreForces)
                _constantForceBehaviour.force = new Vector3(0, -Gravity, 0) + _normalForce;
            else
                _constantForceBehaviour.force = Vector3.zero;

            _force = _constantForceBehaviour.force + LastVelocity;

            _objectAtRest = CheckIsGrounded() && _force.magnitude <= 0.1f;
        }
    }
}