using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    [System.Serializable]
    public struct ColliderInfo
    {
        public string Name;
        [Tooltip("If true, the hit collider will despawn after the amount of active frames have been surpassed.")]
        public bool DespawnAfterTimeLimit;
        [Tooltip("How long the hitbox will be active for.")]
        public float TimeActive;
        [Tooltip("Whether or not this collider will be destroyed if it hits a valid object.")]
        public bool DestroyOnHit;
        [Tooltip("If true, the hit collider will call the onHit event multiple times")]
        public bool IsMultiHit;
        [Tooltip("Whether or not this collider will ignore other ability colliders.")]
        public bool IgnoreColliders;
        [Tooltip("The collision layers to ignore when checking for valid collisions.")]
        public List<string> LayersToIgnore;
        [Tooltip("If this collider can hit multiple times, this is how many frames the object will have to wait before being able to register a collision with the same object.")]
        public float HitFrames;
        [Tooltip("The amount of damage this attack will deal.")]
        public float Damage;
        [Tooltip("How far back this attack will knock an object back.")]
        public float BaseKnockBack;
        [Tooltip("How much the knock back of this ability will scale based on the health of the object hit.")]
        public float KnockBackScale;
        [Tooltip("The angle (in radians) that the object in knock back will be launched at.")]
        public float HitAngle;
        [Tooltip("If true, the angle the force is applied at will change based on where it hit the target")]
        public bool AdjustAngleBasedOnCollision;
        [Tooltip("The type of damage this collider will be read as")]
        public DamageType TypeOfDamage;
        public bool CanSpike;
        [Tooltip("The amount of time a character can't perform any actions after being hit")]
        public float HitStunTime;
        [Tooltip("The priority level of the collider. Colliders with higher levels destroy colliders with lower levels.")]
        public float Priority;


        /// <summary>
        /// Get a copy of the hit collider info with the attack stats (damage, base knock back, knock back scale, hit stun time) scaled
        /// </summary>
        /// <param name="scale">The amount to scale the stats by</param>
        /// <returns>A new copy of the hit collider info</returns>
        public ColliderInfo ScaleStats(float scale)
        {
            ColliderInfo ColliderInfo = (ColliderInfo)MemberwiseClone();

            ColliderInfo.Damage *= scale;
            ColliderInfo.BaseKnockBack *= scale;
            ColliderInfo.KnockBackScale *= scale;
            ColliderInfo.HitStunTime *= scale;

            return ColliderInfo;
        }
    }
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
        protected float CurrentTimeActive;
        protected float StartTime;
        protected Dictionary<GameObject, int> Collisions;
        /// <summary>
        /// Collision event called when this collider hits another. 
        /// First argument is game object it collided with.
        /// </summary>
        public CollisionEvent OnHit;
        protected float _lastHitFrame;
        public ColliderInfo ColliderInfo = new ColliderInfo();
        [Tooltip("The game object spawned this collider.")]
        public GameObject Owner;

        public ColliderBehaviour() { }

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
            Collisions = new Dictionary<GameObject, int>();
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
            collider2.Init(collider1.ColliderInfo, collider1.Owner);
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

        public bool CheckIfLayerShouldBeIgnored(int layer)
        {
            if (ColliderInfo.LayersToIgnore == null)
                return false;

            if (ColliderInfo.LayersToIgnore.Count == 0)
                return false;

            int mask = LayerMask.GetMask(ColliderInfo.LayersToIgnore.ToArray());
            if (mask != (mask | 1 << layer))
                return true;

            return false;
        }

        /// <summary>
        /// Initializes this colliders stats
        /// </summary>
        /// <param name="damage">The amount of damage this attack will do</param>
        /// <param name="baseKnockBack">How far back this attack will knock an object back</param>
        /// <param name="hitAngle">The angle (in radians) that the object in knock back will be launched at</param>
        /// <param name="timeActive">If true, the hit collider will damage objects that enter it multiple times</param>
        public void Init(bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false)
        {
            ColliderInfo = new ColliderInfo { DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, DestroyOnHit = destroyOnHit, IsMultiHit = isMultiHit };
        }

        public virtual void Init(ColliderInfo info, GameObject owner)
        {
            ColliderInfo = info;
            Owner = owner;
        }


        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(other.gameObject) || ColliderInfo.IsMultiHit || other.gameObject == Owner)
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

            if (otherCollider && ColliderInfo.IgnoreColliders || CheckIfLayerShouldBeIgnored(otherGameObject.layer))
                    return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject, Time.frameCount);

            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;

            OnHit?.Invoke(otherGameObject, otherCollider, collisionDirection);

            if (ColliderInfo.DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!ColliderInfo.IsMultiHit || other.gameObject == Owner || !CheckHitTime(other.gameObject))
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

            if (otherCollider && ColliderInfo.IgnoreColliders || CheckIfLayerShouldBeIgnored(otherGameObject.layer))
                return;

            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;

            OnHit?.Invoke(otherGameObject, otherCollider, collisionDirection);

            if (ColliderInfo.DestroyOnHit)
                Destroy(gameObject);
        }

        /// <summary>
        /// Checks if this collider can register a collision again.
        /// Useful for multihit colliders
        /// </summary>
        /// <returns>Whether or not enough time has passed since the last hit</returns>
        protected bool CheckHitTime(GameObject gameObject)
        {
            int lastHitFrame = 0;
            if (!Collisions.TryGetValue(gameObject, out lastHitFrame))
            {
                Collisions.Add(gameObject, Time.frameCount);
                return false;
            }

            if (Time.frameCount - lastHitFrame >= ColliderInfo.HitFrames)
            {
                Collisions[gameObject] = Time.frameCount;
                return true;
            }

            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(collision.gameObject) || ColliderInfo.IsMultiHit || collision.gameObject == Owner)
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


            if (otherCollider && ColliderInfo.IgnoreColliders || CheckIfLayerShouldBeIgnored(otherGameObject.layer))
                return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(collision.gameObject, Time.frameCount);

            Vector3 collisionDirection = (otherGameObject.transform.position - transform.position).normalized;

            OnHit?.Invoke(collision.gameObject, collision, collisionDirection);

            if (ColliderInfo.DestroyOnHit)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            //Update the amount of current frames
            CurrentTimeActive = Time.time - StartTime;

            //Destroy the hit collider if it has exceeded or reach its maximum time active
            if (CurrentTimeActive >= ColliderInfo.TimeActive && ColliderInfo.DespawnAfterTimeLimit)
                Destroy(gameObject);
        }
    }
}
