using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.ScriptableObjects;

namespace Lodis.Movement
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(GridMovementBehaviour))]
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
        [SerializeField]
        private FloatVariable _velocityDecayRate;
        [SerializeField]
        private CharacterDefenseBehaviour _defenseBehaviour;

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
                _inFreeFall = value;
            }
        }

        public Vector3 Acceleration { get => _acceleration; }


        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
            _defenseBehaviour = GetComponent<CharacterDefenseBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _objectAtRest = condition => RigidbodyInactive(); 
            _movementBehaviour.AddOnMoveEnabledAction(() => { _rigidbody.isKinematic = true; });
            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
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

            if (BlackBoardBehaviour.Grid.GetPanelAtLocationInWorld(transform.position, out panel, false))
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
            _rigidbody.useGravity = false;
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
        private IEnumerator FreezeCoroutine(float time)
        {
            float timeStarted = Time.time;
            float timeElapsed = 0;

            while (timeElapsed < time)
            {
                timeElapsed = Time.time - timeStarted;
                _rigidbody.AddForce(-_rigidbody.velocity, ForceMode.VelocityChange);
                yield return new WaitForFixedUpdate();
            }
            Debug.Log("StopFreezing");
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
            float panelSize = BlackBoardBehaviour.Grid.PanelRef.transform.localScale.x;
            float panelSpacing = BlackBoardBehaviour.Grid.PanelSpacing;
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
            magnitude = Mathf.Clamp(magnitude, 0, _maxMagnitude.Val);

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

        public override void OnCollisionEnter(Collision collision)
        {
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to add force
            if (!knockBackScript)
                knockBackScript = this;

            //Prevent knockback if target is braced
            if (knockBackScript._defenseBehaviour.IsBraced)
            {
                knockBackScript.SetInvincibilityByTimer(knockBackScript._defenseBehaviour.BraceInvincibilityTime);
                knockBackScript.StopVelocity();
                return;
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

        /// <summary>
        /// Adds an instant change in velocity to the object ignoring mass.
        /// </summary>
        /// <param name="velocity">The new velocity for the object.</param>
        public void ApplyVelocityChange(Vector3 velocity)
        {
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

            if (!InHitStun)
                _movementBehaviour.DisableMovement(_objectAtRest, false);

            _rigidbody.AddForce(force, ForceMode.Impulse);
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
                _velocityOnLaunch = knockBackForce;
                _rigidbody.isKinematic = false;

                _movementBehaviour.canCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.canCancelMovement = false;

                //Add force to object
                _rigidbody.AddForce(_velocityOnLaunch, ForceMode.Impulse);

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(_objectAtRest, false);

                if (_velocityOnLaunch.magnitude > 0)
                    _onKnockBack?.Invoke();
            }

            return damage;
        }

        private void FixedUpdate()
        {
            _acceleration = (_rigidbody.velocity - LastVelocity) / Time.fixedDeltaTime;
            _lastVelocity = _rigidbody.velocity;

            if (_acceleration.magnitude <= 0 && _rigidbody.isKinematic)
                _inFreeFall = false;

            _inHitStun = !RigidbodyInactive() && !_rigidbody.isKinematic;
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


