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
        [Tooltip("How heavy the game object is. The higher the number, the less panels it travels when knocked back.")]
        [SerializeField]
        private float _weight;
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
        private bool _inHitStun;
        private bool _inFreeFall;
        private Coroutine _currentCoroutine;
        private UnityAction _onKnockBack;
        private UnityAction _onKnockBackStart;
        [SerializeField]
        private FloatVariable _velocityDecayRate;
        [SerializeField]
        private CharacterDefenseBehaviour _defenseBehaviour;
        [SerializeField]
        private float _landingTime;
        [SerializeField]
        private float _gravity = 9.81f;
        private ConstantForce _constantForceBehaviour;
        private Collider _collider;

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


        public CollisionEvent OnCollision
        {
            private get;
            set;
        }

        public bool Landing
        {
            get;
            private set;
        }

        public bool UseGravity
        {
            get
            {
                return _rigidbody.useGravity;
            }
            set
            {
                _rigidbody.useGravity = value;
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

        public Vector3 Acceleration { get => _acceleration; }


        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
            _defenseBehaviour = GetComponent<CharacterDefenseBehaviour>();
            _constantForceBehaviour = GetComponent<ConstantForce>();
            _collider = GetComponent<Collider>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _objectAtRest = condition => RigidbodyInactive(); 
            _movementBehaviour.AddOnMoveEnabledAction(() => { _rigidbody.isKinematic = true; });
            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
            OnCollision += TryStartLandingLag;
            _onKnockBack += () => Landing = false;
            _onKnockBackStart += () => { if (Stunned) { UnfreezeObject(); } };

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

        ///Was only used for airdodging. A better implementation should be considered.
        //public void MoveRigidBodyToLocation(Vector3 position)
        //{
        //    _rigidbody.MovePosition(position);
        //}

        /// <summary>
        /// Sets velocity and angular velocity to be zero
        /// </summary>
        public void StopVelocity()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Set velocity and angular velocity to be zero and disables gravity.
        /// </summary>
        public void StopAllForces()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

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
        private IEnumerator FreezeCoroutine(float time, bool keepMomentum = false)
        {
            Vector3 velocity = LastVelocity;

            float timeStarted = Time.time;
            float timeElapsed = 0;

            while (timeElapsed < time)
            {
                timeElapsed = Time.time - timeStarted;
                _rigidbody.AddForce(-_rigidbody.velocity, ForceMode.VelocityChange);
                yield return new WaitForFixedUpdate();
            }

            if (keepMomentum)
                ApplyVelocityChange(velocity);
        }

        /// <summary>
        /// If the object is being effected by non grid forces, 
        /// freeze the object in place for the given time.
        /// </summary>
        /// <param name="time">The amount of time in seconds to freeze in place.</param>
        public void FreezeInPlaceByTimer(float time)
        {
            _currentCoroutine = StartCoroutine(FreezeCoroutine(time));
        }

        public void UnfreezeObject()
        {
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);
        }

        /// <summary>
        /// Finds the force needed to move the game object the given number of panels backwards
        /// </summary>
        /// <param name="knockbackScale">How many panels backwards will the object move assuming its weight is 0</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <returns>The force needed to move the object to the panel destination</returns>
        public Vector3 CalculateKnockbackForce(float knockbackScale, float hitAngle)
        {
            //Find the space between each panel and the panels size to use to find the total displacement
            float panelSize = BlackBoardBehaviour.Instance.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Instance.Grid.PanelSpacing;
            //Apply the damage and weight to find the amount of knock back to be applied
            float totalKnockback = (knockbackScale + (knockbackScale * (Health /100))) - _weight;

            //If the knockback was too weak return an empty vector
            if (totalKnockback <= 0)
            {
                _newPanelPosition = _movementBehaviour.Position;
                return new Vector3();
            }

            //If the angle is within a certain range, ignore the angle and apply an upward force
            if (Mathf.Abs(hitAngle - (Mathf.PI / 2)) <= _rangeToIgnoreUpAngle)
            {
                ApplyImpulseForce(Vector3.up * knockbackScale * 2);
                return Vector3.up * knockbackScale * 2;
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
        /// Add a listener to the onKnockBackStart event. Called before knock back is applied.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackStartAction(UnityAction action)
        {
            _onKnockBackStart += action;
        }

        private void TryStartLandingLag(params object[] args)
        {
            GameObject plane = (GameObject)args[0];
            if (!InFreeFall || !plane.CompareTag("Structure"))
                return;

            _currentCoroutine = StartCoroutine(StartLandingLag());
        }

        protected override IEnumerator ActivateStun(float time)
        {
            MovesetBehaviour moveset = GetComponent<MovesetBehaviour>();
            Input.InputBehaviour inputBehaviour = GetComponent<Input.InputBehaviour>();

            if (InFreeFall || InHitStun)
                FreezeInPlaceByTimer(time);

            if (moveset)
            {
                moveset.enabled = false;
                moveset.StopAllCoroutines();
            }
            if (inputBehaviour)
            {
                inputBehaviour.enabled = false;
                inputBehaviour.StopAllCoroutines();
            }

            yield return new WaitForSeconds(time);

            if (moveset)
                moveset.enabled = true;
            if (inputBehaviour)
                inputBehaviour.enabled = true;
        }

        public override void OnCollisionEnter(Collision collision)
        {
            OnCollision?.Invoke(collision.gameObject, collision);

            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null)
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

        private bool IsGrounded()
        {
            float extraHeight = 0.0f;
            bool hit = Physics.Raycast(_collider.bounds.center, Vector3.down, _collider.bounds.extents.y + extraHeight, LayerMask.GetMask(new string[]{ "Structure", "Panels"}));
            Debug.DrawRay(_collider.bounds.center, Vector3.down * (_collider.bounds.extents.y + extraHeight));

            return hit;
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

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = CalculateKnockbackForce(knockBackScale, hitAngle);

            if (knockBackForce.magnitude > 0)
            {
                _onKnockBackStart?.Invoke();

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
                _rigidbody.AddForce(_velocityOnLaunch, ForceMode.Impulse);


                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _inHitStun = true;
                    _onKnockBack?.Invoke();
                }
            }

            return damage;
        }

        private void FixedUpdate()
        {
            _acceleration = (_rigidbody.velocity - LastVelocity) / Time.fixedDeltaTime;

            if (_rigidbody.velocity.magnitude > _maxMagnitude.Value)
                _rigidbody.velocity = _rigidbody.velocity.normalized * _maxMagnitude.Value;

            _lastVelocity = _rigidbody.velocity;

            //if (_rigidbody.velocity.magnitude > 0)
            //    _rigidbody.velocity -= _rigidbody.velocity.normalized * _velocityDecayRate.Value;
            
            if (_acceleration.magnitude <= 0 && _rigidbody.isKinematic)
                _inFreeFall = false;

            if (RigidbodyInactive() || _rigidbody.isKinematic || InFreeFall)
                _inHitStun = false;

            if (!IsGrounded())
                _constantForceBehaviour.force = new Vector3(0, -Gravity, 0);
            else
                _constantForceBehaviour.force = Vector3.zero;
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


