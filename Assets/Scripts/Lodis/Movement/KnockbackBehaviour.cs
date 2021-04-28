using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;

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
        private Condition _onRigidbodyInactive;
        private Vector2 _newPanelPosition = new Vector2(float.NaN, float.NaN );
        private float _currentKnockBackScale;
        private Vector3 _velocityOnLaunch;
        private Vector3 _lastVelocity;
        [Tooltip("Any angles for knock back force recieved in this range will send the object directly upwards")]
        [SerializeField]
        private float _rangeToIgnoreUpAngle = 0.2f;

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
                return !RigidbodyInactive(null);
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

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            _onRigidbodyInactive = RigidbodyInactive;
            _movementBehaviour.AddOnMoveEnabledAction(() => { _rigidbody.isKinematic = true; });
            _movementBehaviour.AddOnMoveEnabledAction(UpdatePanelPosition);
            
        }

        private bool RigidbodyInactive(object[] args)
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

        /// <summary>
        /// Sets velocity and angular velocity to be zero
        /// </summary>
        public void StopVelocity()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        public void StopAllForces()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = false;
        }

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

        public void FreezeInPlaceByTimer(float time)
        {
            StartCoroutine(FreezeCoroutine(time));
        }

        public void UnfreezeObject()
        {
            StopCoroutine(FreezeCoroutine(0));
        }

        public void EnableAllForces()
        {
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = true;
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

        public override void OnCollisionEnter(Collision collision)
        {
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript)
                knockBackScript = this;

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
            damageScript.TakeDamage(knockbackScale * 2, knockbackScale / BounceDampen, hitAngle, DamageType.KNOCKBACK);
        }

        public void ApplyImpulseForce(Vector3 force)
        {
            if (!InHitStun)
                _movementBehaviour.DisableMovement(_onRigidbodyInactive);

            _rigidbody.AddForce(force, ForceMode.Impulse);
        }

        /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// Index 0: The damage this object will take
        /// Index 1: How many panels backwards will the object move assuming its weight is 0
        /// Index 2: The angle to launch the object
        /// </summary>
        public override float TakeDamage(float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || !_rigidbody || IsInvincible)
                return 0;

            //Update current knockback scale
            _currentKnockBackScale = knockBackScale;

            //Adds damage to the total damage
            Health += damage;

            //Calculates force and applies it to the rigidbody
            _rigidbody.isKinematic = false;
            Vector3 knockBackForce = CalculateKnockbackForce(knockBackScale, hitAngle);

            if (knockBackForce.magnitude > 0)
            {
                _velocityOnLaunch = knockBackForce;
                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(_onRigidbodyInactive);

                //Add force to object
                _rigidbody.AddForce(_velocityOnLaunch, ForceMode.Impulse);
            }

            return damage;
        }

        private void FixedUpdate()
        {
            _lastVelocity = _rigidbody.velocity;
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
                _owner.TakeDamage(_damage, _knockbackScale, _hitAngle);
            }
        }
    }

#endif
}


