using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public delegate void CollisionEvent(params object[] args);
    public class ColliderBehaviour : MonoBehaviour
    {
        
        [Tooltip("If true, the hit collider will despawn after the amount of active frames have been surpassed.")]
        [SerializeField]
        protected bool _despawnsAfterTimeLimit = false;
        [Tooltip("How many frames the hitbox will be active for.")]
        [SerializeField]
        protected float _timeActive;
        [Tooltip("If true, the hit collider will call the onHit event multiple times")]
        [SerializeField]
        protected bool _isMultiHit;
        [SerializeField]
        protected bool _destroyOnHit;
        protected float _currentTimeActive;
        protected float _startTime;
        protected GameObject _owner;
        protected List<GameObject> _collisions;
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

        public ColliderBehaviour() { }

        public ColliderBehaviour(bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false)
        {
            Init(despawnAfterTimeLimit, timeActive, owner, destroyOnHit, isMultiHit);
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
        public static void Copy(ColliderBehaviour collider1, ColliderBehaviour collider2)
        {
            collider2.Init(collider1._despawnsAfterTimeLimit, collider1._timeActive, collider1.Owner, collider1._destroyOnHit, collider1._isMultiHit);
            collider2.onHit = collider1.onHit;
        }

        /// <summary>
        /// Initializes this colliders stats
        /// </summary>
        /// <param name="damage">The amount of damage this attack will do</param>
        /// <param name="knockBackScale">How far back this attack will knock an object back</param>
        /// <param name="hitAngle">The angle (in radians) that the object in knock back will be launched at</param>
        /// <param name="timeActive">If true, the hit collider will damage objects that enter it multiple times</param>
        public void Init(bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false)
        {
            _despawnsAfterTimeLimit = despawnAfterTimeLimit;
            _timeActive = timeActive;
            _owner = owner;
            _destroyOnHit = destroyOnHit;
            _isMultiHit = isMultiHit;
        }

        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (_collisions.Contains(other.gameObject) || _isMultiHit || other.gameObject == _owner)
                return;

            ColliderBehaviour otherCollider = null;
            GameObject otherGameObject = null;

            if (other.attachedRigidbody)
            {
                otherGameObject = other.attachedRigidbody.gameObject;
                otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();
            }
            else
            {
                otherGameObject = other.gameObject;
            }
                

            if (otherCollider)
                    return;

            //Add the game object to the list of collisions so it is not collided with again
            _collisions.Add(other.gameObject);

            onHit?.Invoke(otherGameObject, otherCollider);

            if (_destroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!_isMultiHit || other.gameObject == _owner)
                return;

            ColliderBehaviour otherCollider = null;
            GameObject otherGameObject = null;

            if (other.attachedRigidbody)
            {
                otherGameObject = other.attachedRigidbody.gameObject;
                otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();
            }
            else
            {
                otherGameObject = other.gameObject;
            }

            if (otherCollider)
                return;

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
            GameObject otherGameObject = null;

            if (collision.collider.attachedRigidbody)
            {
                otherGameObject = collision.collider.attachedRigidbody.gameObject;
                otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();
            }
            else
            {
                otherGameObject = collision.gameObject;
            }

            if (otherCollider)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            _collisions.Add(collision.gameObject);

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
