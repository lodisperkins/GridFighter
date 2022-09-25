﻿using Lodis.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;

namespace Lodis.Gameplay
{
    [System.Serializable]
    public struct HitColliderData
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
        [Tooltip("The collision layers to ignore when checking for valid collisions.")]
        public LayerMask LayersToIgnore;
        [Tooltip("If this collider can hit multiple times, this is how many seconds the object will have to wait before being able to register a collision with the same object.")]
        public float MultiHitWaitTime;
        [Tooltip("The amount of damage this attack will deal.")]
        public float Damage;
        [Tooltip("How far back this attack will knock an object back.")]
        public float BaseKnockBack;
        [Tooltip("How much the knock back of this ability will scale based on the health of the object hit.")]
        public float KnockBackScale;
        [Tooltip("The angle (in radians) that the object in knock back will be launched at.")]
        public float HitAngle;
        [Tooltip("If true, the angle the force is applied at will change based on where it hit the target")]
        public bool AdjustAngleBasedOnAlignment;
        [Tooltip("The type of damage this collider will be read as")]
        public DamageType TypeOfDamage;
        public bool CanSpike;
        [Tooltip("The amount of time a character can't perform any actions after being hit")]
        public float HitStunTime;
        [Tooltip("The priority level of the collider. Colliders with higher levels destroy colliders with lower levels.")]
        public float Priority;
        public GridAlignment OwnerAlignement;
        [HideInInspector]
        public AbilityType AbilityType;
        [Tooltip("The spark effect that will spawn on hit.")]
        public GameObject HitSpark;
        [Tooltip("The size of the effect that will play on successful hit")]
        [Range(0,3)]
        public int HitEffectLevel;

        [Tooltip("How long the hit stop will be scaled on hit.")]
        public float HitStopTimeModifier;

        [Tooltip("Event called when this collider htis a valid object.")]
        public CollisionEvent OnHit;

        public void AddOnHitEvent(CollisionEvent collisionEvent)
        {
            OnHit += collisionEvent;
        }

        /// <summary>
        /// Get a copy of the hit collider info with the attack stats (damage, base knock back, knock back scale, hit stun time) scaled
        /// </summary>
        /// <param name="scale">The amount to scale the stats by</param>
        /// <returns>A new copy of the hit collider info</returns>
        public HitColliderData ScaleStats(float scale)
        {
            HitColliderData ColliderInfo = (HitColliderData)MemberwiseClone();

            ColliderInfo.Damage *= scale;
            ColliderInfo.BaseKnockBack *= scale;
            ColliderInfo.KnockBackScale *= scale;
            ColliderInfo.HitStunTime *= scale;

            return ColliderInfo;
        }
    }


    public class HitColliderBehaviour : ColliderBehaviour
    {
        /// <summary>
        /// If enabled, draws the collider in the editor
        /// </summary>
        public bool DebuggingEnabled;
        public HitColliderData ColliderInfo;
        private bool _addedToActiveList;

        public float StartTime { get; private set; }
        public float CurrentTimeActive { get; private set; }

        public HitColliderBehaviour(float damage, float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false, bool angleChangeOnCollision = true, float hitStunTimer = 0)
           : base()
        {
            HitColliderData info = new HitColliderData { Damage = damage, BaseKnockBack = baseKnockBack, HitAngle = hitAngle, DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, DestroyOnHit = destroyOnHit, AdjustAngleBasedOnAlignment = angleChangeOnCollision, HitStunTime = hitStunTimer };
            Init(info, owner);
        }

        public HitColliderBehaviour(HitColliderData info, GameObject owner)
        {
            Init(info, owner);
        }

        public HitColliderBehaviour()
        {
        }

        public  void Init(HitColliderData info, GameObject owner)
        {

            ColliderInfo = info;
            ColliderInfo.OwnerAlignement = owner.GetComponent<GridMovementBehaviour>().Alignment;
            Owner = owner;
        }

        protected override void Awake()
        {
            base.Awake();
            gameObject.layer = LayerMask.NameToLayer("Ability");
        }

        protected override void Start()
        {
            base.Start();

            //ReturnToPoolListener.AddAction(RemoveFromActiveList);
            AddToActiveList();
            LayersToIgnore = ColliderInfo.LayersToIgnore;
            LayersToIgnore |= (1 << LayerMask.NameToLayer("IgnoreHitColliders"));
            StartTime = Time.time;
        }

        private void OnEnable()
        {
            AddToActiveList();
            ResetActiveTime();
            Collisions.Clear();
        }

        private void AddToActiveList()
        {
            if (ColliderInfo.OwnerAlignement == GridAlignment.LEFT)
                BlackBoardBehaviour.Instance.GetLHSActiveColliders().Add(this);
            else if (ColliderInfo.OwnerAlignement == GridAlignment.RIGHT)
                BlackBoardBehaviour.Instance.GetRHSActiveColliders().Add(this);

        }

        private void RemoveFromActiveList()
        {

            if (ColliderInfo.OwnerAlignement == GridAlignment.LEFT)
                BlackBoardBehaviour.Instance.GetLHSActiveColliders().Remove(this);
            else if (ColliderInfo.OwnerAlignement == GridAlignment.RIGHT)
                BlackBoardBehaviour.Instance.GetRHSActiveColliders().Remove(this);
        }

        /// <summary>
        /// Copies the values in collider 1 to collider 2
        /// </summary>
        /// <param name="collider1"></param>
        /// <param name="collider2"></param>
        public static void Copy(HitColliderBehaviour collider1, HitColliderBehaviour collider2)
        {
            collider2.ColliderInfo = collider1.ColliderInfo;
            collider2.AddCollisionEvent(collider1.ColliderInfo.OnHit);
            collider2.Owner = collider1.Owner;
        }

        public override void AddCollisionEvent(CollisionEvent collisionEvent)
        {
            ColliderInfo.AddOnHitEvent(collisionEvent);
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
            HitColliderData info = new HitColliderData { Damage = damage, BaseKnockBack = baseKnockBack, HitAngle = hitAngle, DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, DestroyOnHit = destroyOnHit, AdjustAngleBasedOnAlignment = angleChangeOnCollision, HitStunTime = hitStunTimer };
            Init(info, owner);
        }

        /// <summary>
        /// Checks if this collider can register a collision again.
        /// Useful for multihit colliders
        /// </summary>
        /// <returns>Whether or not enough time has passed since the last hit</returns>
        protected bool CheckHitTime(GameObject gameObject)
        {
            float lastHitTime = 0;
            if (!Collisions.TryGetValue(gameObject, out lastHitTime))
            {
                Collisions.Add(gameObject, Time.time);
                return true;
            }

            if (Time.time - lastHitTime >= ColliderInfo.MultiHitWaitTime)
            {
                Collisions[gameObject] = Time.time;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the timer keeping track of how long this hit box will be active.
        /// Can be used to prevent an object from being destroyed after it exceeds
        /// its lifetime.
        /// </summary>
        public void ResetActiveTime()
        {
            StartTime = Time.time;
        }


        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(other.gameObject) || ColliderInfo.IsMultiHit || other.gameObject == Owner || other.CompareTag("Reflector"))
                return;

            if (Collisions.Count > 0 && ColliderInfo.DestroyOnHit) return;

            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

            ColliderBehaviour otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();

            if (CheckIfLayerShouldBeIgnored(otherGameObject.layer)) return;

            //Return if its attached to this object or this object wants to ignore collider
            if (otherCollider)
            {
                //Return if either objects want to ignore the other.
                if (otherCollider.CheckIfLayerShouldBeIgnored(gameObject.layer) || otherCollider.Owner == Owner)
                    return;

                //If it is a hit collider...
                if (otherCollider is HitColliderBehaviour hitCollider)
                {
                    //...destroy it if it has a lower priority
                    if (ColliderInfo.Priority >= hitCollider.ColliderInfo.Priority)
                    {
                        ObjectPoolBehaviour.Instance.ReturnGameObject(otherGameObject);
                        if (hitCollider.ColliderInfo.HitSpark)
                            Instantiate(hitCollider.ColliderInfo.HitSpark, transform.position, Camera.main.transform.rotation);
                    }


                    //Ignore the collider otherwise
                    return;
                }
            }

            if (ColliderInfo.HitSpark)
                Instantiate(ColliderInfo.HitSpark, transform.position, Camera.main.transform.rotation);

            float newHitAngle = ColliderInfo.HitAngle;
            float defaultAngle = newHitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnAlignment)
            {
                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);

                //Find a new direction based the alignment
                int direction = ColliderInfo.OwnerAlignement == GridAlignment.LEFT ? 1 : -1;
                currentForceDirection.x *= direction;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject, Time.frameCount);
            ColliderInfo.HitAngle = newHitAngle;

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();
            //If the damage script wasn't null damage the object
            if (damageScript != null)
            {
                damageScript.LastCollider = this;
                damageScript.TakeDamage(ColliderInfo, Owner);
                if (ColliderInfo.HitEffectLevel > 0 && !damageScript.IsInvincible)
                    Instantiate(BlackBoardBehaviour.Instance.HitEffects[ColliderInfo.HitEffectLevel - 1], other.transform.position + (.5f * Vector3.up), transform.rotation);
            }
            
            ColliderInfo.OnHit?.Invoke(other.gameObject, otherCollider, other, this, damageScript);

            ColliderInfo.HitAngle = defaultAngle;
            if (ColliderInfo.DestroyOnHit)
                ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!ColliderInfo.IsMultiHit || other.gameObject == Owner || !CheckHitTime(gameObject) || other.CompareTag("Reflector"))
                return;

            if (!Collisions.ContainsKey(other.gameObject))
                Collisions.Add(other.gameObject, Time.frameCount);

            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

            ColliderBehaviour otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();
            if (CheckIfLayerShouldBeIgnored(otherGameObject.layer)) return;

            //Return if its attached to this object or this object wants to ignore collider
            if (otherCollider)
            {
                //Return if either objects want to ignore the other.
                if (otherCollider.CheckIfLayerShouldBeIgnored(gameObject.layer) || otherCollider.Owner == Owner)
                    return;

                //If it is a hit collider...
                if (otherCollider is HitColliderBehaviour hitCollider)
                {
                    //...destroy it if it has a lower priority
                    if (hitCollider.ColliderInfo.Priority >= ColliderInfo.Priority)
                        ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);

                    //Ignore the collider otherwise
                    return;
                }
            }

            if (ColliderInfo.HitSpark)
                Instantiate(ColliderInfo.HitSpark, transform.position, Camera.main.transform.rotation);

            float newHitAngle = ColliderInfo.HitAngle;
            float defaultAngle = newHitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnAlignment)
            {
                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);

                //Find a new direction based the alignment
                int direction = ColliderInfo.OwnerAlignement == GridAlignment.LEFT ? 1 : -1;
                currentForceDirection.x *= direction;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            //Add the game object to the list of collisions so it is not collided with again
            ColliderInfo.HitAngle = newHitAngle;

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();
            //If the damage script wasn't null damage the object
            if (damageScript != null)
            {
                damageScript.LastCollider = this;
                damageScript.TakeDamage(ColliderInfo, Owner);
                if (ColliderInfo.HitEffectLevel > 0 && !damageScript.IsInvincible)
                    Instantiate(BlackBoardBehaviour.Instance.HitEffects[ColliderInfo.HitEffectLevel - 1], other.transform.position + (.5f * Vector3.up), transform.rotation);
            }

            ColliderInfo.HitAngle = defaultAngle;
            ColliderInfo.OnHit?.Invoke(other.gameObject, otherCollider, other, this, damageScript);

            if (ColliderInfo.DestroyOnHit)
                ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            GameObject other = collision.gameObject;

            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(other.gameObject) || ColliderInfo.IsMultiHit || other.gameObject == Owner || other.CompareTag("Reflector"))
                return;

            if (Collisions.Count > 0 && ColliderInfo.DestroyOnHit) return;

            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = collision.collider.attachedRigidbody ? collision.collider.attachedRigidbody.gameObject : other.gameObject;

            ColliderBehaviour otherCollider = otherGameObject.GetComponent<ColliderBehaviour>();
            if (CheckIfLayerShouldBeIgnored(otherGameObject.layer)) return;

            //Return if its attached to this object or this object wants to ignore collider
            if (otherCollider)
            {
                //Return if either objects want to ignore the other.
                if (otherCollider.CheckIfLayerShouldBeIgnored(gameObject.layer) || otherCollider.Owner == Owner)
                    return;

                //If it is a hit collider...
                if (otherCollider is HitColliderBehaviour hitCollider)
                {
                    //...destroy it if it has a lower priority
                    if (hitCollider.ColliderInfo.Priority >= ColliderInfo.Priority)
                        ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);

                    //Ignore the collider otherwise
                    return;
                }
            }

            if (ColliderInfo.HitSpark)
                Instantiate(ColliderInfo.HitSpark, transform.position, Camera.main.transform.rotation);

            float newHitAngle = ColliderInfo.HitAngle;
            float defaultAngle = newHitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnAlignment)
            {
                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);

                //Find a new direction based the alignment
                int direction = ColliderInfo.OwnerAlignement == GridAlignment.LEFT ? 1 : -1;
                currentForceDirection.x *= direction;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(other.gameObject, Time.frameCount);
            ColliderInfo.HitAngle = newHitAngle;

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.GetComponent<HealthBehaviour>();
            //If the damage script wasn't null damage the object
            if (damageScript != null)
            {
                damageScript.LastCollider = this;
                damageScript.TakeDamage(ColliderInfo, Owner);
                if (ColliderInfo.HitEffectLevel > 0 && !damageScript.IsInvincible)
                    Instantiate(BlackBoardBehaviour.Instance.HitEffects[ColliderInfo.HitEffectLevel - 1], other.transform.position + (.5f * Vector3.up), transform.rotation);
            }


            ColliderInfo.OnHit?.Invoke(other.gameObject, otherCollider, other, this, damageScript);
            ColliderInfo.HitAngle = defaultAngle;
            if (ColliderInfo.DestroyOnHit)
                ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
        }


        private void OnDrawGizmos()
        {
            if (!DebuggingEnabled)
                return;

            BoxCollider boxCollider = GetComponent<BoxCollider>();
            SphereCollider sphereCollider = GetComponent<SphereCollider>();

            if (boxCollider)
                Gizmos.DrawCube(transform.position, boxCollider.size);
            else if (sphereCollider)
                Gizmos.DrawSphere(transform.position, sphereCollider.radius);
        }

        private void Update()
        {
            if (gameObject == null)
                return;


            _addedToActiveList = true;
            //Update the amount of current frames
            CurrentTimeActive = Time.time - StartTime;

            //Destroy the hit collider if it has exceeded or reach its maximum time active
            if (CurrentTimeActive >= ColliderInfo.TimeActive && ColliderInfo.DespawnAfterTimeLimit)
            {
                if (ColliderInfo.HitSpark)
                    Instantiate(ColliderInfo.HitSpark, transform.position, transform.rotation);

                ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
            }
        }
    }
}


