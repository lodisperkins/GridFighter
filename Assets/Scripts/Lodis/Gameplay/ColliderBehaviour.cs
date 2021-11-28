using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    /// <summary>
    /// Event used when collisions occur. 
    /// Arg[0] = The game object collided with.
    /// Arg[1] = The collision data. Is a collider type when on trigger enter/stay is called,
    /// and is a collision type when on collision enter is called
    /// </summary>
    /// <param name="args"></param>
    public delegate void CollisionEvent(params object[] args);
    public class ColliderBehaviour : MonoBehaviour
    {
        
        [Tooltip("If true, the hit collider will despawn after the amount of active frames have been surpassed.")]
        [SerializeField]
        protected bool DespawnsAfterTimeLimit = false;
        [Tooltip("How many frames the hitbox will be active for.")]
        [SerializeField]
        protected float TimeActive;
        [Tooltip("If true, the hit collider will call the onHit event multiple times")]
        [SerializeField]
        protected bool IsMultiHit;
        [SerializeField]
        protected bool DestroyOnHit;
        protected float CurrentTimeActive;
        protected float StartTime;
        protected GameObject Owner;
        protected List<GameObject> Collisions;
        protected string OwnerName = "NoOwner";
        /// <summary>
        /// Collision event called when this collider hits another. 
        /// First argument is game object it collided with.
        /// </summary>
        public CollisionEvent OnHit;
        protected float _lastHitFrame;
        public float HitFrames;
        public bool IgnoreColliders = true;

        public GameObject ColliderOwner
        {
            get
            {
                return Owner;
            }
            set
            {
                Owner = value;
            }
        }

        public ColliderBehaviour() {}

        /// <summary>
        /// Initializes this colliders stats
        /// </summary>
        /// <param name="despawnAfterTimeLimit">If true the hit box despawns when its not active</param>
        /// <param name="timeActive">How long this object will be active it is set to despawn</param>
        /// <param name="owner">The game object that spawned the collider</param>
        /// <param name="destroyOnHit">If true, the collider will destroy itself when it hits</param>
        /// <param name="isMultiHit">If true, the hit collider will trigger a collision with objects that enter it multiple times</param>
        public ColliderBehaviour(bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false)
        {
            Init(despawnAfterTimeLimit, timeActive, owner, destroyOnHit, isMultiHit);
        }

        private void Awake()
        {
            Collisions = new List<GameObject>();
        }

        private void Start()
        {
            StartTime = Time.time;
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1">The collider that will have its values copied</param>
        /// <param name="collider2">The collider that will have its values overwritten</param>
        public static void Copy(ColliderBehaviour collider1, ColliderBehaviour collider2)
        {
            collider2.Init(collider1.DespawnsAfterTimeLimit, collider1.TimeActive, collider1.Owner, collider1.DestroyOnHit, collider1.IsMultiHit);
            collider2.OnHit = collider1.OnHit;
        }

        /// <summary>
        /// If this collider despawns over some time,
        /// this will reset the timer
        /// </summary>
        public void ResetActiveTime()
        {
            StartTime = Time.time;
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
            DespawnsAfterTimeLimit = despawnAfterTimeLimit;
            TimeActive = timeActive;
            Owner = owner;
            DestroyOnHit = destroyOnHit;
            IsMultiHit = isMultiHit;
        }

        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.Contains(other.gameObject) || IsMultiHit || other.gameObject == Owner)
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
                

            if (otherCollider && IgnoreColliders)
                    return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject);

            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;

            OnHit?.Invoke(otherGameObject, otherCollider, collisionDirection);

            if (DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!IsMultiHit || other.gameObject == Owner || !CheckHitTime())
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

            if (otherCollider && IgnoreColliders)
                return;

            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;

            OnHit?.Invoke(otherGameObject, otherCollider, collisionDirection);

            if (DestroyOnHit)
                Destroy(gameObject);
        }

        /// <summary>
        /// Checks if this collider can register a collision again.
        /// Useful for multihit colliders
        /// </summary>
        /// <returns>Whether or not enough time has passed since the last hit</returns>
        protected bool CheckHitTime()
        {
            if (Time.frameCount - _lastHitFrame >= HitFrames)
            {
                _lastHitFrame = Time.frameCount;
                return true;
            }

            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.Contains(collision.gameObject) || IsMultiHit || collision.gameObject == Owner)
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

            if (otherCollider && IgnoreColliders)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(collision.gameObject);

            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;

            OnHit?.Invoke(collision.gameObject, collision, collisionDirection);

            if (DestroyOnHit)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            //Update the amount of current frames
            CurrentTimeActive = Time.time - StartTime;

            //Destroy the hit collider if it has exceeded or reach its maximum time active
            if (CurrentTimeActive >= TimeActive && DespawnsAfterTimeLimit)
                Destroy(gameObject);
        }
    }
}
