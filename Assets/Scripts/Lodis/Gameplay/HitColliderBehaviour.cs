using Lodis.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    [Serializable]
    public class HitColliderInfo : ColliderInfo
    {
        public float Damage;
        [Tooltip("How far back this attack will knock an object back.")]
        public float BaseKnockBack;
        [Tooltip("The angle (in radians) that the object in knock back will be launched at.")]
        public float HitAngle;
        [Tooltip("If true, the angle the force is applied at will change based on where it hit the target")]
        public bool AdjustAngleBasedOnCollision;
        public DamageType TypeOfDamage = DamageType.DEFAULT;
        public bool CanSpike;
        public float HitStunTime;
    }

    public class HitColliderBehaviour : ColliderBehaviour
    {
        public HitColliderInfo ColliderInfo;

        /// <summary>
        /// Collision event called when this collider hits another. 
        /// First argument is game object it collided with.
        /// </summary>
        public CollisionEvent onHit;
        /// <summary>
        /// If enabled, draws the collider in the editor
        /// </summary>
        public bool debuggingEnabled;
        [Tooltip("The priority level of the collider. Colliders with higher levels destroy colliders with lower levels.")]
        public float Priority = 0.0f;

        public HitColliderBehaviour(float damage, float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false, bool angleChangeOnCollision = true, float hitStunTimer = 0)
            : base()
        {
            HitColliderInfo info = new HitColliderInfo { Damage = damage, BaseKnockBack = baseKnockBack, HitAngle = hitAngle, DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, Owner = owner, DestroyOnHit = destroyOnHit, AdjustAngleBasedOnCollision = angleChangeOnCollision, HitStunTime = hitStunTimer };
            Init(info);
        }

        private void Awake()
        {
            if (LayersToIgnore == null)
                LayersToIgnore = new List<string>();

            LayersToIgnore.Add("ParryBox");
            Collisions = new Dictionary<GameObject, int>();
        }

        private void Start()
        {
            StartTime = Time.time;
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1"></param>
        /// <param name="collider2"></param>
        public static void Copy(HitColliderBehaviour collider1, HitColliderBehaviour collider2)
        {
            collider2.ColliderInfo = collider1.ColliderInfo;
            collider2.onHit = collider1.onHit;
            collider2.IgnoreColliders = collider1.IgnoreColliders;
            collider2.Priority = collider1.Priority;
            collider2.LayersToIgnore = collider1.LayersToIgnore;
        }

        
        public void Init(HitColliderInfo info)
        {
            ColliderInfo = info;
        }

///     <summary>
        /// Initializes this colliders stats
        /// </summary>
        /// <param name="damage">The amount of damage this attack will do</param>
        /// <param name="baseKnockBack">How far back this attack will knock an object back</param>
        /// <param name="hitAngle">The angle (in radians) that the object in knock back will be launched at</param>
        /// <param name="timeActive">If true, the hit collider will damage objects that enter it multiple times</param>
        public void Init(float damage, float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false, bool angleChangeOnCollision = true, float hitStunTimer = 0)
        {
            HitColliderInfo info = new HitColliderInfo { Damage = damage, BaseKnockBack = baseKnockBack, HitAngle = hitAngle, DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, Owner = owner, DestroyOnHit = destroyOnHit, AdjustAngleBasedOnCollision = angleChangeOnCollision, HitStunTime = hitStunTimer };
            Init(info);
        }

        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(other.gameObject) || IsMultiHit)
                return;

            ColliderBehaviour otherCollider = null;

            if (other.attachedRigidbody)
            {
                if (other.attachedRigidbody.gameObject != Owner)
                    otherCollider = other.attachedRigidbody.gameObject.GetComponentInChildren<ColliderBehaviour>();
                else
                    return;
            }

            if (other.CompareTag("ParryBox"))
                return;

            if (otherCollider && !other.CompareTag("Player") && !other.CompareTag("Entity"))
            {
                if (IgnoreColliders || otherCollider.IgnoreColliders || otherCollider.ColliderOwner == Owner)
                    return;
                else if (otherCollider is HitColliderBehaviour)
                {
                    if (((HitColliderBehaviour)otherCollider).Priority >= Priority && otherCollider.ColliderOwner != ColliderOwner)
                    {
                        Destroy(gameObject);
                        return;
                    }

                    return;
                }
            } 
            
            CharacterDefenseBehaviour characterDefenseBehaviour = other.GetComponentInParent<CharacterDefenseBehaviour>();
            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();

            if (characterDefenseBehaviour?.IsParrying == true && damageScript?.IsInvincible == true)
                return;

            float newHitAngle = ColliderInfo.HitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnCollision)
            {
                //Find a vector that point from the collider to the object hit
                Vector3 directionOfImpact = other.transform.position - transform.position;
                directionOfImpact.Normalize();
                directionOfImpact.x = Mathf.Round(directionOfImpact.x);

                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);
                currentForceDirection.x *= directionOfImpact.x;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject, Time.frameCount);

            if (Owner)
                OwnerName = Owner.name;

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(OwnerName, ColliderInfo.Damage, ColliderInfo.BaseKnockBack, newHitAngle, ColliderInfo.TypeOfDamage, ColliderInfo.HitStunTime);

            onHit?.Invoke(other.gameObject, otherCollider);

            if (DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!IsMultiHit || other.gameObject == Owner || !CheckHitTime(gameObject))
                return;

            if (!Collisions.ContainsKey(other.gameObject))
                Collisions.Add(other.gameObject, Time.frameCount);

            ColliderBehaviour otherCollider = null;

            if (other.attachedRigidbody)
                otherCollider = other.attachedRigidbody.gameObject.GetComponent<ColliderBehaviour>();


            //Grab whatever health script is attached to this object. If none return
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();

            if (other.CompareTag("ParryBox"))
                return;

            CharacterDefenseBehaviour characterDefenseBehaviour = other.GetComponentInParent<CharacterDefenseBehaviour>();

            if (characterDefenseBehaviour?.IsParrying == true && damageScript?.IsInvincible == true)
                return;

            if (otherCollider && IgnoreColliders)
                return;

            else if (otherCollider is HitColliderBehaviour)
            {
                if (((HitColliderBehaviour)otherCollider).Priority >= Priority && otherCollider.ColliderOwner != ColliderOwner)
                {
                    Destroy(gameObject);
                    return;
                }
                return;
            }


            float newHitAngle = ColliderInfo.HitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnCollision)
            {
                //Find a vector that point from the collider to the object hit
                Vector3 directionOfImpact = other.transform.position - transform.position;
                directionOfImpact.Normalize();
                directionOfImpact.x = Mathf.Round(directionOfImpact.x);

                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);
                currentForceDirection.x *= directionOfImpact.x;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            if (Owner)
                OwnerName = Owner.name;

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(OwnerName, ColliderInfo.Damage, ColliderInfo.BaseKnockBack, newHitAngle, ColliderInfo.TypeOfDamage, ColliderInfo.HitStunTime);

            onHit?.Invoke(other.gameObject);

            if (DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(collision.gameObject) || IsMultiHit || collision.gameObject == Owner)
                return;

            ColliderBehaviour otherCollider = null;

            if (collision.collider.attachedRigidbody)
                otherCollider = collision.collider.attachedRigidbody.gameObject.GetComponent<ColliderBehaviour>();

            if (collision.gameObject.CompareTag("ParryBox"))
                return;

            CharacterDefenseBehaviour characterDefenseBehaviour = collision.gameObject.GetComponentInParent<CharacterDefenseBehaviour>();

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (characterDefenseBehaviour?.IsParrying == true && damageScript?.IsInvincible == true)
                return;

            if (otherCollider && IgnoreColliders)
                return;
            else if (otherCollider is HitColliderBehaviour)
            {
                if (((HitColliderBehaviour)otherCollider).Priority >= Priority && otherCollider.ColliderOwner != ColliderOwner)
                {
                    Destroy(gameObject);
                    return;
                }
                return;
            }

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(collision.gameObject, Time.frameCount);

            float newHitAngle = ColliderInfo.HitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnCollision)
            {
                //Find a vector that point from the collider to the object hit
                Vector3 directionOfImpact = collision.gameObject.transform.position - transform.position;
                directionOfImpact.Normalize();
                directionOfImpact.x = Mathf.Round(directionOfImpact.x);

                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);
                currentForceDirection.x *= directionOfImpact.x;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            if (Owner)
                OwnerName = Owner.name;

            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(OwnerName, ColliderInfo.HitAngle, ColliderInfo.BaseKnockBack, newHitAngle, ColliderInfo.TypeOfDamage, ColliderInfo.HitStunTime);

            onHit?.Invoke(collision.gameObject);

            if (DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            if (!debuggingEnabled)
                return;

            BoxCollider boxCollider = GetComponent<BoxCollider>();
            SphereCollider sphereCollider = GetComponent<SphereCollider>();

            if (boxCollider)
                Gizmos.DrawCube(transform.position, boxCollider.size);
            else if (sphereCollider)
                Gizmos.DrawSphere(transform.position, sphereCollider.radius);
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


