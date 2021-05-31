using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    public class HitColliderBehaviour : ColliderBehaviour
    {
        [Tooltip("The amount of damage this attack will do.")]
        [SerializeField]
        private float _damage;
        [Tooltip("How far back this attack will knock an object back.")]
        [SerializeField]
        private float _knockBackScale;
        [Tooltip("The angle (in radians) that the object in knock back will be launched at.")]
        [SerializeField]
        private float _hitAngle;
        [Tooltip("If true, the angle the force is applied at will change based on where it hit the target")]
        [SerializeField]
        private bool _adjustAngleBasedOnCollision;
        public DamageType damageType = DamageType.DEFAULT;

        /// <summary>
        /// Collision event called when this collider hits another. 
        /// First argument is game object it collided with.
        /// </summary>
        public CollisionEvent onHit;

        public GameObject Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
            }
        }

        public HitColliderBehaviour(float damage, float knockBackScale, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false, bool angleChangeOnCollision = true)
            : base()
        {
            Init(damage, knockBackScale, hitAngle, despawnAfterTimeLimit, timeActive, owner, destroyOnHit, isMultiHit, angleChangeOnCollision);

        }

        private void Awake()
        {
            _collisions = new List<GameObject>();
        }

        private void Start()
        {
            _startTime = Time.time;
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1"></param>
        /// <param name="collider2"></param>
        public static void Copy(HitColliderBehaviour collider1, HitColliderBehaviour collider2)
        {
            collider2.Init(collider1._damage, collider1._knockBackScale, collider1._hitAngle, collider1._despawnsAfterTimeLimit, collider1._timeActive, collider1.Owner, collider1._destroyOnHit, collider1._isMultiHit);
            collider2.onHit = collider1.onHit;
        }

        /// <summary>
        /// Initializes this colliders stats
        /// </summary>
        /// <param name="damage">The amount of damage this attack will do</param>
        /// <param name="knockBackScale">How far back this attack will knock an object back</param>
        /// <param name="hitAngle">The angle (in radians) that the object in knock back will be launched at</param>
        /// <param name="timeActive">If true, the hit collider will damage objects that enter it multiple times</param>
        public void Init(float damage, float knockBackScale, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false, bool angleChangeOnCollision = true)
        {
            _damage = damage;
            _knockBackScale = knockBackScale;
            _hitAngle = hitAngle;
            _despawnsAfterTimeLimit = despawnAfterTimeLimit;
            _timeActive = timeActive;
            _owner = owner;
            _destroyOnHit = destroyOnHit;
            _isMultiHit = isMultiHit;
            _adjustAngleBasedOnCollision = angleChangeOnCollision;
            ignoreColliders = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (_collisions.Contains(other.gameObject) || _isMultiHit || other.gameObject == _owner)
                return;

            ColliderBehaviour otherCollider = null;

            if (other.attachedRigidbody)
                otherCollider = other.attachedRigidbody.gameObject.GetComponent<ColliderBehaviour>();

            if (otherCollider && ignoreColliders)
                    return;

            if (_adjustAngleBasedOnCollision)
            {
                Vector3 directionOfImpact = other.transform.position - transform.position;

                directionOfImpact.Normalize();

                Vector3 currentForceDirection = new Vector3(Mathf.Cos(_hitAngle), Mathf.Sin(_hitAngle), 0);

                currentForceDirection.Scale(directionOfImpact);

                _hitAngle = Mathf.Acos(Vector3.Dot(currentForceDirection, Vector3.right));
            }

            //Add the game object to the list of collisions so it is not collided with again
            _collisions.Add(other.gameObject);

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();

            if (Owner)
                ownerName = Owner.name;

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(ownerName, _damage, _knockBackScale, _hitAngle, damageType);

            onHit?.Invoke(other.gameObject, otherCollider);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!_isMultiHit || other.gameObject == _owner)
                return;

            ColliderBehaviour otherCollider = null;

            if (other.attachedRigidbody)
                otherCollider = other.attachedRigidbody.gameObject.GetComponent<ColliderBehaviour>();

            if (otherCollider && ignoreColliders)
                return;

            //Grab whatever health script is attached to this object. If none return
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();


            if (_adjustAngleBasedOnCollision)
            {
                Vector3 directionOfImpact = other.transform.position - transform.position;

                directionOfImpact.Normalize();

                Vector3 currentForceDirection = new Vector3(Mathf.Cos(_hitAngle), Mathf.Sin(_hitAngle), 0);

                currentForceDirection.Scale(directionOfImpact);

                _hitAngle = Mathf.Acos(Vector3.Dot(currentForceDirection, Vector3.right));
            }

            if (Owner)
                ownerName = Owner.name;

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(ownerName, _damage, _knockBackScale, _hitAngle, damageType);

            onHit?.Invoke(other.gameObject);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            //If the object has already been hit or if the collider is multihit return
            if (_collisions.Contains(collision.gameObject) || _isMultiHit || collision.gameObject == _owner)
                return;

            ColliderBehaviour otherCollider = null;

            if (collision.collider.attachedRigidbody)
                otherCollider = collision.collider.attachedRigidbody.gameObject.GetComponent<ColliderBehaviour>();

            if (otherCollider && ignoreColliders)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            _collisions.Add(collision.gameObject);

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            //Adjust the angle of force based on the direction of impact
            if (_adjustAngleBasedOnCollision)
            {
                Vector3 directionOfImpact = collision.gameObject.transform.position - transform.position;

                directionOfImpact.Normalize();

                Vector3 currentForceDirection = new Vector3(Mathf.Cos(_hitAngle), Mathf.Sin(_hitAngle), 0);

                currentForceDirection.Scale(directionOfImpact);

                _hitAngle = Mathf.Acos(Vector3.Dot(currentForceDirection, Vector3.right));
            }

            if (Owner)
                ownerName = Owner.name;

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(ownerName, _damage, _knockBackScale, _hitAngle, damageType);

            onHit?.Invoke(collision.gameObject);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            //Update the amount of current frames
            _currentTimeActive = Time.time - _startTime;

            //Destroy the hit collider if it has exceeded or reach its maximum time active
            if (_currentTimeActive >= _timeActive && _despawnsAfterTimeLimit)
                Destroy(gameObject);
        }
    }
}


