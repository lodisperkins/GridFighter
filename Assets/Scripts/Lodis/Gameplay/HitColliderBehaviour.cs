using Lodis.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{

    public class HitColliderBehaviour : ColliderBehaviour
    {
        /// <summary>
        /// If enabled, draws the collider in the editor
        /// </summary>
        public bool DebuggingEnabled;
        private bool _addedToActiveList;

        public HitColliderBehaviour(float damage, float baseKnockBack, float hitAngle, bool despawnAfterTimeLimit, float timeActive = 0, GameObject owner = null, bool destroyOnHit = false, bool isMultiHit = false, bool angleChangeOnCollision = true, float hitStunTimer = 0)
           : base()
        {
            ColliderInfo info = new ColliderInfo { Damage = damage, BaseKnockBack = baseKnockBack, HitAngle = hitAngle, DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, DestroyOnHit = destroyOnHit, AdjustAngleBasedOnCollision = angleChangeOnCollision, HitStunTime = hitStunTimer };
            Init(info, owner);
        }

        public HitColliderBehaviour(ColliderInfo info, GameObject owner)
        {
            Init(info, owner);
        }

        public  void Init(ColliderInfo info, GameObject owner)
        {

            ColliderInfo = info;
            Owner = owner;
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
        /// <param name="collider1"></param>
        /// <param name="collider2"></param>
        public static void Copy(HitColliderBehaviour collider1, HitColliderBehaviour collider2)
        {
            collider2.ColliderInfo = collider1.ColliderInfo;
            collider2.OnHit = collider1.OnHit;
            collider2.Owner = collider1.Owner;
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
            ColliderInfo info = new ColliderInfo { Damage = damage, BaseKnockBack = baseKnockBack, HitAngle = hitAngle, DespawnAfterTimeLimit = despawnAfterTimeLimit, TimeActive = timeActive, DestroyOnHit = destroyOnHit, AdjustAngleBasedOnCollision = angleChangeOnCollision, HitStunTime = hitStunTimer };
            Init(info, owner);
        }

        private void OnTriggerEnter(Collider other)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(other.gameObject) || ColliderInfo.IsMultiHit || CheckIfLayerShouldBeIgnored(other.gameObject.layer))
                return;

            //Get the collider behaviour attached to the rigidbody
            ColliderBehaviour otherCollider = null;
            if (other.attachedRigidbody)
            {
                if (other.attachedRigidbody.gameObject != Owner)
                    otherCollider = other.attachedRigidbody.gameObject.GetComponentInChildren<ColliderBehaviour>();
                else
                    return;
            }

            bool otherIgnoresThisLayer = otherCollider?.CheckIfLayerShouldBeIgnored(gameObject.layer) == true;

            //If it has a collider behaviour and doesn't want to ignore this object's layer...
            if (otherCollider && !otherIgnoresThisLayer)
            {
                //...destroy it if it has a lower priority
                if (otherCollider.ColliderInfo.Priority >= ColliderInfo.Priority)
                {
                    Destroy(gameObject);
                    return;
                }
                //Ignore the collider otherwise
                return;
            }
            else if (otherIgnoresThisLayer) return;
            
            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript?.IsInvincible == true)
                return;

            float newHitAngle = ColliderInfo.HitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnCollision)
            {
                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);
                currentForceDirection.x *= Owner.transform.forward.x;

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
            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(ColliderInfo, Owner);

            OnHit?.Invoke(other.gameObject, otherCollider);

            if (ColliderInfo.DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!ColliderInfo.IsMultiHit || other.gameObject == Owner || !CheckHitTime(gameObject))
                return;

            if (!Collisions.ContainsKey(other.gameObject))
                Collisions.Add(other.gameObject, Time.frameCount);

            //Get the collider behaviour attached to the rigidbody
            ColliderBehaviour otherCollider = null;
            if (other.attachedRigidbody)
            {
                if (other.attachedRigidbody.gameObject != Owner)
                    otherCollider = other.attachedRigidbody.gameObject.GetComponentInChildren<ColliderBehaviour>();
                else
                    return;
            }

            bool otherIgnoresThisLayer = otherCollider?.CheckIfLayerShouldBeIgnored(gameObject.layer) == true;

            //If it has a collider behaviour and doesn't want to ignore this object's layer...
            if (otherCollider && !otherIgnoresThisLayer)
            {
                //...destroy it if it has a lower priority
                if (otherCollider.ColliderInfo.Priority >= ColliderInfo.Priority)
                {
                    Destroy(gameObject);
                    return;
                }
                //Ignore the collider otherwise
                return;
            }
            else if (otherIgnoresThisLayer) return;


            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = other.gameObject.GetComponent<HealthBehaviour>();

            if ( damageScript?.IsInvincible == true)
                return;

            float newHitAngle = ColliderInfo.HitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnCollision)
            {
                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);
                currentForceDirection.x *= Owner.transform.forward.x;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            ColliderInfo.HitAngle = newHitAngle;
            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(ColliderInfo, Owner);

            OnHit?.Invoke(other.gameObject);

            if (ColliderInfo.DestroyOnHit)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            //If the object has already been hit or if the collider is multihit return
            if (Collisions.ContainsKey(collision.gameObject) || ColliderInfo.IsMultiHit || collision.gameObject == Owner)
                return;

            ColliderBehaviour otherCollider = null;

            if (collision.collider.attachedRigidbody)
                otherCollider = collision.collider.attachedRigidbody.gameObject.GetComponent<ColliderBehaviour>();

            //If the object has a collider and isn't a character...
            bool otherIgnoresThisLayer = otherCollider?.CheckIfLayerShouldBeIgnored(gameObject.layer) == true;

            //If it has a collider behaviour and doesn't want to ignore this object's layer...
            if (otherCollider && !otherIgnoresThisLayer)
            {
                //...destroy it if it has a lower priority
                if (otherCollider.ColliderInfo.Priority >= ColliderInfo.Priority)
                {
                    Destroy(gameObject);
                    return;
                }
                //Ignore the collider otherwise
                return;
            }
            else if (otherIgnoresThisLayer) return;

            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript?.IsInvincible == true)
                return;

            //Add the game object to the list of collisions so it is not collided with again
            Collisions.Add(collision.gameObject, Time.frameCount);

            float newHitAngle = ColliderInfo.HitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnCollision)
            {
                //Find the direction this collider was going to apply force originally
                Vector3 currentForceDirection = new Vector3(Mathf.Cos(newHitAngle), Mathf.Sin(newHitAngle), 0);
                currentForceDirection.x *= Owner.transform.forward.x;

                //Find the new angle based on the direction of the attack on the x axis
                float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (Vector3.Dot(currentForceDirection, Vector3.up) < 0)
                    newHitAngle *= -1;
            }

            ColliderInfo.HitAngle = newHitAngle;
            //If the damage script wasn't null damage the object
            if (damageScript != null)
                damageScript.TakeDamage(ColliderInfo, Owner);

            OnHit?.Invoke(collision.gameObject);

            if (ColliderInfo.DestroyOnHit)
                Destroy(gameObject);
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
            if (gameObject != null && !_addedToActiveList)
            {
                if (Owner.CompareTag("Player") && Owner.name.Contains("(P1)"))
                    BlackBoardBehaviour.Instance.GetLHSActiveColliders().Add(this);
                else if (Owner.CompareTag("Player") && Owner.name.Contains("(P2)"))
                    BlackBoardBehaviour.Instance.GetRHSActiveColliders().Add(this);

                _addedToActiveList = true;
            }
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


