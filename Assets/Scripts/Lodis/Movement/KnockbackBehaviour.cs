using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.ScriptableObjects;
using GridGame.GamePlay.GridScripts;

namespace Lodis.Movement
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GridMovementBehaviour))]
    [RequireComponent(typeof(ConstantForce))]
    public class KnockbackBehaviour : HealthBehaviour
    {
        private Rigidbody _rigidbody;
        [Tooltip("How much mass the game object has. The higher the number, the less panels it travels when knocked back.")]
        [SerializeField]
        private float _mass;
        [Tooltip("How fast will objects be allowed to travel in knockback")]
        [SerializeField]
        private ScriptableObjects.FloatVariable _maxMagnitude;
        private GridMovementBehaviour _movementBehaviour;
        private Condition _objectAtRest;
        private Vector2 _newPanelPosition = new Vector2(float.NaN, float.NaN );
        private float _currentKnockBackScale;
        private Vector3 _velocityOnLaunch;
        private Vector3 _acceleration;
        private Vector3 _lastVelocity;
        [Tooltip("Any angles for knock back force recieved in this range will send the object directly upwards")]
        [SerializeField]
        private float _rangeToIgnoreUpAngle = 0.2f;
        private float _freeFallMagnitudeMin = 1;
        [SerializeField]
        private bool _inHitStun;
        [SerializeField]
        private bool _inFreeFall;
        private Coroutine _currentCoroutine;
        private UnityAction _onKnockBack;
        private UnityAction _onKnockBackStart;
        private UnityAction _onTakeDamage;
        [Tooltip("The rate at which an objects move speed in air will decrease")]
        [SerializeField]
        private FloatVariable _velocityDecayRate;
        [SerializeField]
        private CharacterDefenseBehaviour _defenseBehaviour;
        [Tooltip("The amount of time it takes for this object to regain footing after landing")]
        [SerializeField]
        private float _landingTime;
        [Tooltip("The strength of the force pushing downwards on this object once in air")]
        [SerializeField]
        private float _gravity = 9.81f;
        private ConstantForce _constantForceBehaviour;
        [Tooltip("The collider attached this object that will be used for registering collision against objects while air")]
        [SerializeField]
        private Collider _bounceCollider;
        [SerializeField]
        private bool _isGrounded;
        [Tooltip("The position that will be used to check if this character is grounded")]
        [SerializeField]
        private Vector3 _idleGroundedPoint;
        [SerializeField]
        private Vector3 _idleGroundedPointExtents;
        [SerializeField]
        private float _extraHeight = 0.5f;
        private Vector3 _groundedBoxPosition;
        private Vector3 _groundedBoxExtents;
        private UnityAction _onKnockBackTemp;
        private UnityAction _onKnockBackStartTemp;
        private UnityAction _onTakeDamageTemp;
        private Vector3 _normalForce;
        private Vector3 _force;
        [SerializeField]
        private float _bounciness = 0.8f;
        [SerializeField]
        private bool _panelBounceEnabled = true;
        private bool _isSpiked = false;
        [SerializeField]
        private bool _useGravity = true;
        private CustomYieldInstruction _wait;
        [SerializeField]
        private Vector3 _freeFallGroundedPoint;
        [SerializeField]
        private Vector3 _freeFallGroundedPointExtents;

        /// <summary>
        /// Whether or not this object will bounce on panels it falls on
        /// </summary>
        public bool PanelBounceEnabled
        {
            get
            {
                return _panelBounceEnabled;
            }
            set
            {
                _panelBounceEnabled = value;
            }
        }

        /// <summary>
        /// Whether or not this object is in a spiked state
        /// </summary>
        public bool IsSpiked
        {
            get
            {
                return _isSpiked;
            }
            set
            {
                _isSpiked = value;
            }
        }

        /// <summary>
        /// How bouncy this object is
        /// </summary>
        public float Bounciness
        {
            get
            {
                return _bounciness;
            }
        }

        public float Gravity
        {
            get
            {
                return _gravity;
            }
            set
            {
                _gravity = value;
            }
        }

        public float Mass
        {
            get
            {
                return _mass;
            }
        }

        /// <summary>
        /// The event called when this object collides with another
        /// </summary>
        public CollisionEvent OnCollision
        {
            private get;
            set;
        }

        /// <summary>
        /// Whether or not this object is current regaining footing after hitting the ground
        /// </summary>
        public bool Landing
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether or not this object should be effected by gravity
        /// </summary>
        public bool UseGravity
        {
            get
            {
                return _useGravity;
            }
            set
            {
                _useGravity = value;
            }
        }

        /// <summary>
        /// Returns if the object is in knockback
        /// </summary>
        public bool InHitStun
        {
            get
            {
                return _inHitStun;
            }
        }

        /// <summary>
        /// Returns the velocity of this object when it was first launched
        /// </summary>
        public Vector3 LaunchVelocity
        {
            get
            {
                return _velocityOnLaunch;
            }
        }


        /// <summary>
        /// Returns the velocity of the rigid body in the last fixed update
        /// </summary>
        public Vector3 LastVelocity
        {
            get
            {
                return _lastVelocity;
            }
        }

        /// <summary>
        /// The scale of the last knock back value applied to the object
        /// </summary>
        public float CurrentKnockBackScale
        {
            get
            {
                return _currentKnockBackScale;
            }
        }

        /// <summary>
        /// Whether or not this object is in the air without being in a tumble state
        /// </summary>
        public bool InFreeFall 
        {
            get
            {
                return _inFreeFall;
            }
            set 
            {
                if (value)
                    _inHitStun = !value;

                _inFreeFall = value;
            }
        }

        public Collider BounceCollider
        {
            get
            {
                return _bounceCollider;
            }
        }

        public Vector3 Acceleration { get => _acceleration; }
        public float LandingTime { get => _landingTime;}

        public bool IsGrounded
        {
            get { return _isGrounded; }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
            _defenseBehaviour = GetComponent<CharacterDefenseBehaviour>();
            _constantForceBehaviour = GetComponent<ConstantForce>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _objectAtRest = condition => LastVelocity.magnitude <= 0.1 && CheckIsGrounded() && _acceleration.magnitude <= 0.1; 
            _movementBehaviour.AddOnMoveEnabledAction(() => { _rigidbody.isKinematic = true; });
            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
            //OnCollision += TryStartLandingLag;
            _onKnockBack += () => Landing = false;
            _onKnockBackStart += () => { Stunned = false; _movementBehaviour.CurrentPanel.Occupied = false; };
        }

        /// <summary>
        /// True if the rigidbody is sleeping.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool RigidbodyInactive(object[] args = null)
        {
            return _rigidbody.IsSleeping();
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
        /// Sets velocity and angular velocity to be zero
        /// </summary>
        public void StopVelocity()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Makes the object kinematic
        /// </summary>
        public void MakeKinematic()
        {
            _rigidbody.isKinematic = true;
        }

        /// <summary>
        /// Set velocity and angular velocity to be zero and disables gravity.
        /// </summary>
        public void StopAllForces()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            UseGravity = false;
        }

        /// <summary>
        /// Gets whether or not this object is on the ground and not being effected by any forces
        /// </summary>
        public bool CheckIfAtRest()
        {
            return !InHitStun && !InFreeFall && _rigidbody.isKinematic;
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
                _rigidbody.isKinematic = false;

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
                _rigidbody.isKinematic = false;

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
        /// Finds the force needed to move the game object the given number of panels backwards
        /// </summary>
        /// <param name="knockbackScale">How many panels backwards will the object move assuming its weight is 0</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public Vector3 CalculateKnockbackForce(float knockbackScale, float hitAngle, bool knockBackIsFixed = false)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacing;
            //Apply the damage and weight to find the amount of knock back to be applied
            float totalKnockback = 0;
            if (!knockBackIsFixed)
                totalKnockback = (knockbackScale + (knockbackScale * (Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value * 100)));
            else
                totalKnockback = knockbackScale;

            //If the knockback was too weak return an empty vector
            if (totalKnockback <= 0)
            {
                _newPanelPosition = _movementBehaviour.Position;
                return new Vector3();
            }

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(hitAngle - (Mathf.PI / 2)) <= _rangeToIgnoreUpAngle)
            {
                return Vector3.up * Mathf.Sqrt(2 * Gravity * totalKnockback + (totalKnockback * BlackBoardBehaviour.Instance.Grid.PanelSpacing));
            }

            //Clamps hit angle to prevent completely horizontal movement
            hitAngle = Mathf.Clamp(hitAngle, .2f, 3.0f);

            //Uses the total knockback and panel distance to find how far the object is travelling
            float displacement = (panelSize * totalKnockback) + (panelSpacing * (totalKnockback - 1));
            //Finds the magnitude of the force vector to be applied 
            float val1 = displacement * Physics.gravity.magnitude;
            float val2 = Mathf.Sin(2 * hitAngle);
            float val3 = Mathf.Sqrt(val1 / Mathf.Abs(val2));
            float magnitude = val3;

            //If the magnitude is not a number the attack must be too weak. Return an empty vector
            if (float.IsNaN(magnitude))
            {
                _newPanelPosition = _movementBehaviour.Position;
                return new Vector3();
            }

            //Clamps magnitude to be within the limit
            magnitude = Mathf.Clamp(magnitude, 0, _maxMagnitude.Value);

            //Return the knockback force
            return new Vector3(Mathf.Cos(hitAngle), Mathf.Sin(hitAngle)) * magnitude;
        }

        /// <summary>
        /// Add a listener to the onKnockBack event.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackAction(UnityAction action)
        {
            _onKnockBack += action;
        }

        /// <summary>
        /// Add a listener to the onKnockBack event.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackTempAction(UnityAction action)
        {
            _onKnockBackTemp += action;
        }

        /// <summary>
        /// Add a listener to the onKnockBackStart event. Called before knock back is applied.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackStartAction(UnityAction action)
        {
            _onKnockBackStart += action;
        }



        /// <summary>
        /// Add a listener to the onKnockBackStart event. Called before knock back is applied.
        /// Cleared after called
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackStartTempAction(UnityAction action)
        {
            _onKnockBackStartTemp += action;
        }

        /// <summary>
        /// Removes the listener from the on knock back start temporary event
        /// </summary>
        /// <param name="action"></param>
        public void RemoveOnKnockBackStartTempAction(UnityAction action)
        {
            _onKnockBackStartTemp -= action;
        }

        /// <summary>
        /// Adds an action to the event called when this object is damaged
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageAction(UnityAction action)
        {
            _onTakeDamage += action;
        }

        /// <summary>
        /// Adds an action to the event called when this object is damaged.
        /// Listeners cleared after event is called
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageTempAction(UnityAction action)
        {
            _onTakeDamageTemp += action;
        }

        /// <summary>
        /// Starts landing lag if the object just fell onto a structure
        /// </summary>
        /// <param name="args"></param>
        private void TryStartLandingLag(params object[] args)
        {
            if (!InFreeFall && !InHitStun)
                return;

            _currentCoroutine = StartCoroutine(StartLandingLag());
        }

        protected override IEnumerator ActivateStun(float time)
        {
            MovesetBehaviour moveset = GetComponent<MovesetBehaviour>();
            Input.InputBehaviour inputBehaviour = GetComponent<Input.InputBehaviour>();
            GridMovementBehaviour movement = GetComponent<GridMovementBehaviour>();

            Stunned = true;

            if (InFreeFall || InHitStun)
               FreezeInPlaceByCondition(condition =>!Stunned, false, true);

            if (moveset)
            {
                moveset.enabled = false;
                moveset.EndCurrentAbility();
            }
            if (inputBehaviour)
            {
                inputBehaviour.enabled = false;
                inputBehaviour.StopAllCoroutines();
            }
            if (movement)
                movement.DisableMovement(condition => Stunned == false, false, true);

            _onKnockBackTemp += CancelStun;

            yield return new WaitForSeconds(time);

            if (moveset)
                moveset.enabled = true;
            if (inputBehaviour)
                inputBehaviour.enabled = true;

            Stunned = false;
        }

        public override void CancelStun()
        {
            base.CancelStun();

            UnfreezeObject();
        }

        public override void OnCollisionEnter(Collision collision)
        {
            OnCollision?.Invoke(collision.gameObject, collision);

            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null || !InHitStun)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to add force
            if (!knockBackScript)
                knockBackScript = this;

            if (_defenseBehaviour)
            {
                //Prevent knockback if target is braced
                if (_defenseBehaviour.IsBraced && collision.gameObject.CompareTag("Structure"))
                {
                    SetInvincibilityByTimer(knockBackScript._defenseBehaviour.BraceInvincibilityTime);
                    StopVelocity();

                    Vector3 collisionDirection = (collision.transform.position - transform.position).normalized;

                    if (collisionDirection.x != 0)
                        transform.LookAt(new Vector2(collisionDirection.x, transform.position.y));

                    _defenseBehaviour.onFallBroken?.Invoke(collisionDirection);
                    _inFreeFall = true;
                    Debug.Log("teched wall");
                    return;
                }
            }

            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);
            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = knockBackScript.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.CurrentKnockBackScale * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale))
                return;

            //Apply ricochet force and damage
            damageScript.TakeDamage(name, velocityMagnitude, knockbackScale / BounceDampen, hitAngle, DamageType.KNOCKBACK);
        }

        private void OnTriggerEnter(Collider other)
        {
            OnCollision?.Invoke(other.gameObject);

            HealthBehaviour damageScript = other.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to add force
            if (!knockBackScript)
                knockBackScript = this;

            if (_defenseBehaviour)
            {
                //Prevent knockback if target is braced
                if (_defenseBehaviour.IsBraced && other.gameObject.CompareTag("Structure"))
                {
                    SetInvincibilityByTimer(knockBackScript._defenseBehaviour.BraceInvincibilityTime);
                    StopVelocity();

                    Vector3 collisionDirection = (other.transform.position - transform.position).normalized;

                    if (collisionDirection.x != 0)
                        transform.LookAt(new Vector2(collisionDirection.x, transform.position.y));

                    _defenseBehaviour.onFallBroken?.Invoke(collisionDirection);
                    _inFreeFall = true;
                    Debug.Log("teched wall");
                    return;
                }
            }

            //Calculate the knockback and hit angle for the ricochet
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            Vector3 direction = (contactPoint - transform.position).normalized;
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = knockBackScript.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.CurrentKnockBackScale * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale))
                return;

            //Apply ricochet force and damage
            damageScript.TakeDamage(name, velocityMagnitude, knockbackScale / BounceDampen, hitAngle, DamageType.KNOCKBACK);
        }

        private IEnumerator StartLandingLag()
        {
            Landing = true;
            _movementBehaviour.DisableMovement(condition => !Landing, false, true);
            yield return new WaitForSeconds(_landingTime);
            Landing = false;
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyVelocityChange(Vector3 velocity)
        {
            _rigidbody.isKinematic = false;
            _movementBehaviour.canCancelMovement = true;
            _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
            _movementBehaviour.canCancelMovement = false;

            //Prevent movement if not in hitstun.
            if (!InHitStun)
                _movementBehaviour.DisableMovement(_objectAtRest, false);

            _rigidbody.AddForce(velocity, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyForce(Vector3 velocity)
        {
            _rigidbody.isKinematic = false;
            _movementBehaviour.canCancelMovement = true;
            _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
            _movementBehaviour.canCancelMovement = false;

            //Prevent movement if not in hitstun.
            if (!InHitStun)
                _movementBehaviour.DisableMovement(_objectAtRest, false);

            _rigidbody.AddForce(velocity, ForceMode.Force);
        }

        /// <summary>
        /// Adds an instant force impulse using the objects mass.
        /// Disables movement if not in hitstun.
        /// </summary>
        /// <param name="force">The force to apply to the object.</param>
        public void ApplyImpulseForce(Vector3 force)
        {
            _movementBehaviour.canCancelMovement = true;
            _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
            _movementBehaviour.canCancelMovement = false;

            _rigidbody.isKinematic = false;

            if (!InHitStun)
                _movementBehaviour.DisableMovement(_objectAtRest, false);

            _rigidbody.AddForce(force, ForceMode.Impulse);
        }

        /// <summary>
        /// Whether or not this object is touching the ground
        /// </summary>
        /// <returns></returns>
        private bool CheckIsGrounded()
        {
            _isGrounded = false;

            if (InHitStun)
            {
                _groundedBoxPosition = _bounceCollider.bounds.center;
                _groundedBoxExtents = _bounceCollider.bounds.extents * 2;
            }
            else if(InFreeFall)
            {
                _groundedBoxPosition = _freeFallGroundedPoint + transform.position;
                _groundedBoxExtents = _freeFallGroundedPointExtents;
            }
            else
            {
                _groundedBoxPosition = _idleGroundedPoint + transform.position;
                _groundedBoxExtents = _idleGroundedPointExtents;
            }

            Collider[] hits = Physics.OverlapBox(_groundedBoxPosition, _groundedBoxExtents, new Quaternion(), LayerMask.GetMask(new string[] { "Structure", "Panels" }));

            foreach (Collider collider in hits)
            {
                Vector3 closestPoint = collider.ClosestPoint(transform.position);
                float normalY = (transform.position - closestPoint).normalized.y;
                normalY = Mathf.Ceil(normalY);
                if (normalY >= 0)
                    _isGrounded = true;
            }

            return _isGrounded;
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                Gizmos.DrawCube(_groundedBoxPosition, _groundedBoxExtents);
            else
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(_freeFallGroundedPoint + transform.position, _freeFallGroundedPointExtents);
                Gizmos.color = Color.green;
                Gizmos.DrawCube(_idleGroundedPoint + transform.position, _idleGroundedPointExtents);
            }
        }

        /// /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage thid object will take</param>
        public override float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || !_rigidbody || IsInvincible)
                return 0;

            //Update current knockback scale
            _currentKnockBackScale = knockBackScale;

            //Adds damage to the total damage
            Health += damage;
            Health = Mathf.Clamp(Health, 0, BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value);

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = CalculateKnockbackForce(knockBackScale, hitAngle);

            if (knockBackForce.magnitude > 0)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                _rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(_objectAtRest, false, true);

                //Add force to objectd
                _rigidbody.AddForce(_velocityOnLaunch / Mass, ForceMode.Impulse);

                _lastVelocity = _velocityOnLaunch;

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _inHitStun = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }

        /// /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale">How many panels far will this attakc make the object travel</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <param name="knockBackIsFixed">If true, the knock back won't be scaled based on health</param>
        /// <param name="ignoreMass">If true, the force applied to the object won't change based in mass</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public float TakeDamage(string attacker, float damage, float knockBackScale, float hitAngle, bool knockBackIsFixed, bool ignoreMass, DamageType damageType = DamageType.DEFAULT)
        {
            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || !_rigidbody || IsInvincible)
                return 0;

            //Update current knockback scale
            _currentKnockBackScale = knockBackScale;

            //Adds damage to the total damage
            Health += damage;
            Health = Mathf.Clamp(Health, 0, BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value);

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = CalculateKnockbackForce(knockBackScale, hitAngle, knockBackIsFixed);

            if (knockBackForce.magnitude > 0)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                _rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(_objectAtRest, false, true);

                //Add force to objectd
                if (!ignoreMass)
                    _rigidbody.AddForce(_velocityOnLaunch / Mass, ForceMode.Impulse);
                else
                    _rigidbody.AddForce(_velocityOnLaunch, ForceMode.Impulse);

                _lastVelocity = _velocityOnLaunch;

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _inHitStun = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }

        private void FixedUpdate()
        {
            if (_wait != null)
                Debug.Log(_wait.keepWaiting);
            _acceleration = (_rigidbody.velocity - LastVelocity) / Time.fixedDeltaTime;

            if (_rigidbody.velocity.magnitude > _maxMagnitude.Value)
                _rigidbody.velocity = _rigidbody.velocity.normalized * _maxMagnitude.Value;

            _lastVelocity = _rigidbody.velocity;

            if (_rigidbody.velocity.magnitude > 0)
                _rigidbody.velocity /= _velocityDecayRate.Value;

            if (_acceleration.magnitude <= 0 && _rigidbody.isKinematic)
                _inFreeFall = false;

            if (RigidbodyInactive() || _rigidbody.isKinematic || InFreeFall)
                _inHitStun = false;

            if (CheckIsGrounded())
            {
                float yForce = 0;

                if (_lastVelocity.y < 0)
                    yForce = _lastVelocity.y;

                _normalForce = new Vector3(0, Gravity + yForce, 0);

                if (LastVelocity.magnitude <= 0.1f && _acceleration.magnitude <= 0.1f && InHitStun)
                    StopVelocity();
                else if (InFreeFall)
                {
                    StopVelocity();
                    TryStartLandingLag();
                    InFreeFall = false;
                    MakeKinematic();
                }
            }
            else
                _normalForce = Vector3.zero;

            if (UseGravity)
                _constantForceBehaviour.force = new Vector3(0, -Gravity, 0) + _normalForce;
            else
                _constantForceBehaviour.force = Vector3.zero;

            _force = _constantForceBehaviour.force + LastVelocity;
        }
    }



    /// <summary>
    /// Editor script to test attacks
    /// </summary>
#if UNITY_EDITOR

    [CustomEditor(typeof(KnockbackBehaviour))]
    class KnockbackEditor : Editor
    {
        private KnockbackBehaviour _owner;
        private float _damage;
        private float _knockbackScale;
        private float _hitAngle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _owner = (KnockbackBehaviour)target;

            _damage = EditorGUILayout.FloatField("Damage", _damage);
            _knockbackScale = EditorGUILayout.FloatField("Knockback Scale", _knockbackScale);
            _hitAngle = EditorGUILayout.FloatField("Hit Angle", _hitAngle);

            if (GUILayout.Button("Test Attack"))
            {
                _owner.TakeDamage(name, _damage, _knockbackScale, _hitAngle, DamageType.KNOCKBACK);
            }
        }
    }

#endif
}


