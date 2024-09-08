using Lodis.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using Lodis.Sound;
using Newtonsoft.Json;
using FixedPoints;
using Types;
using UnityGGPO;

namespace Lodis.Gameplay
{
    [System.Serializable]
    public struct HitColliderData
    {
        public string Name;

        [Header("Collision Settings")]
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
        [Tooltip("Whether or not this move can knock opponents out of the ring.")]
        public bool ClampForceWithinRing;
        [Tooltip("Whether or not the force added will override the velocity of the object.")]
        public bool IgnoreMomentum;
        [Tooltip("The angle (in radians) that the object in knock back will be launched at.")]
        public float HitAngle;
        [Tooltip("If true, the angle the force is applied at will change based on where it hit the target")]
        public bool AdjustAngleBasedOnAlignment;
        [Tooltip("The type of damage this collider will be read as")]
        public DamageType TypeOfDamage;
        [Tooltip("The amount of time a character can't perform any actions after being hit")]
        public float HitStunTime;
        [Tooltip("The priority level of the collider. Colliders with higher levels destroy colliders with lower levels.")]
        public float Priority;
        [HideInInspector]
        public GridAlignment OwnerAlignement;
        [HideInInspector]
        public AbilityType AbilityType;
        [HideInInspector]
        public float AbilityID;

        [Header("Collision Effects")]
        [Tooltip("The effect that will spawn when the hit box is spawned.")]
        [JsonIgnore]
        public GameObject SpawnEffect;
        [Tooltip("The spark effect that will spawn on hit.")]
        [JsonIgnore]
        public GameObject HitSpark;
        [Tooltip("The size of the effect that will play on successful hit")]
        [Range(0,3)]
        [JsonIgnore]
        public int HitEffectLevel;
        [Tooltip("The strength of the shake on the character being hit.")]
        [JsonIgnore]
        public float HitStopShakeStrength;
        [Tooltip("The strength of the camera shake after hitting opponent.")]
        [JsonIgnore]
        public float CameraShakeStrength;
        [Tooltip("The duration of the camera shake after hitting opponent.")]
        [JsonIgnore]
        public float CameraShakeDuration;
        [Tooltip("The frequency of the camera shake after hitting opponent.")]
        [JsonIgnore]
        public int CameraShakeFrequency;
        [Tooltip("If true, the camera will shake when this collider hits.")]
        [JsonIgnore]
        public bool ShakesCamera;
        [Tooltip("Event called when this collider htis a valid object.")]
        [JsonIgnore]
        public CollisionEvent OnHit;
        [JsonIgnore]
        public AudioClip SpawnSound;
        [JsonIgnore]
        public AudioClip HitSound;
        [JsonIgnore]
        public AudioClip DespawnSound;
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

        public static bool operator ==(HitColliderData lhs, HitColliderData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(HitColliderData lhs, HitColliderData rhs)
        {
            return !lhs.Equals(rhs);
        }
    }


    public class HitColliderBehaviour : ColliderBehaviour
    {
        public HitColliderData ColliderInfo;
        private bool _addedToActiveList;
        private bool _playedSpawnEffects;

        public float StartTime { get; private set; }
        public float CurrentTimeActive { get; private set; }

        public override void InitCollider(Fixed32 width, Fixed32 height, EntityData owner, GridPhysicsBehaviour ownerPhysicsComponent = null)
        {
            base.InitCollider(width, height, owner, ownerPhysicsComponent);
            ColliderInfo.OwnerAlignement = owner.UnityObject.GetComponent<GridMovementBehaviour>().Alignment;
        }

        protected override void Awake()
        {
            base.Awake();
            gameObject.layer = LayerMask.NameToLayer("Ability");
            ReturnToPoolListener.AddAction(
                () =>
                {
                    SoundManagerBehaviour.Instance.PlaySound(ColliderInfo.DespawnSound);
                    _playedSpawnEffects = false;
                });
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
        }

        public override void AddCollisionEvent(CollisionEvent collisionEvent)
        {
            ColliderInfo.AddOnHitEvent(collisionEvent);
        }

        /// <summary>
        /// Checks if this collider can register a collision again.
        /// Useful for multihit colliders
        /// </summary>
        /// <returns>Whether or not enough time has passed since the last hit</returns>
        protected bool CheckHitTime(GameObject gameObject)
        {
            Fixed32 lastHitTime = 0;
            if (!Collisions.TryGetValue(gameObject, out lastHitTime))
            {
                Collisions.Add(gameObject, Utils.TimeGetTime() * RoutineBehaviour.Instance.CharacterTimeScale);
                return true;
            }

            if (Utils.TimeGetTime() * RoutineBehaviour.Instance.CharacterTimeScale - lastHitTime >= ColliderInfo.MultiHitWaitTime)
            {
                Collisions[gameObject] = Utils.TimeGetTime() * RoutineBehaviour.Instance.CharacterTimeScale;
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

        private void ResolveCollision(GameObject attachedGameObject, Collision collision)
        {
            ColliderBehaviour otherCollider = attachedGameObject.GetComponent<ColliderBehaviour>();

            Vector3 hitEffectPosition = transform.position;

            if (attachedGameObject.CompareTag("Player"))
                hitEffectPosition = attachedGameObject.transform.position + (.5f * Vector3.up);

            //Return if its attached to this object or this object wants to ignore collider
            if (otherCollider)
            {
                //If it is a hit collider...
                if (otherCollider is HitColliderBehaviour hitCollider)
                {
                    //...destroy it if it has a lower priority
                    ResolvePriority(attachedGameObject, hitCollider);


                    //Ignore the collider otherwise
                    return;
                }
            }

            if (ColliderInfo.HitSpark)
                Instantiate(ColliderInfo.HitSpark, hitEffectPosition, Camera.main.transform.rotation);

            Fixed32 newHitAngle = ColliderInfo.HitAngle;
            Fixed32 defaultAngle = newHitAngle;

            //Calculates new angle if this object should change trajectory based on direction of hit
            if (ColliderInfo.AdjustAngleBasedOnAlignment)
            {
                //Find the direction this collider was going to apply force originally
                FVector3 currentForceDirection = new FVector3(Fixed32.Cos(newHitAngle), Fixed32.Sin(newHitAngle), 0);

                //Find a new direction based the alignment
                int direction = ColliderInfo.OwnerAlignement == GridAlignment.LEFT ? 1 : -1;
                currentForceDirection.X *= direction;

                //Find the new angle based on the direction of the attack on the x axis
                Fixed32 dotProduct = FVector3.Dot(currentForceDirection, FVector3.Right);
                newHitAngle = Mathf.Acos(dotProduct);

                //Find if the angle should be negative or positive
                if (FVector3.Dot(currentForceDirection, FVector3.Up) < 0)
                    newHitAngle *= -1;
            }

            //Add the game object to the list of collisions so it is not collided with again
            if (!Collisions.ContainsKey(attachedGameObject))
                Collisions.Add(attachedGameObject, Utils.TimeGetTime());


            //Grab whatever health script is attached to this object
            HealthBehaviour damageScript = attachedGameObject.GetComponent<HealthBehaviour>();

            if (damageScript?.DamageableAbilityID != ColliderInfo.AbilityID && damageScript?.DamageableAbilityID != -1)
                return;

            ColliderInfo.HitAngle = newHitAngle;

            bool damageDealt = false;

            //If the damage script wasn't null damage the object
            if (damageScript != null && !damageScript.IsInvincible)
            {
                damageScript.LastCollider = this;
                KnockbackBehaviour knockback;

                if (ColliderInfo.Damage > 0 && damageScript.TakeDamage(ColliderInfo, Spawner) > 0)
                {
                    if (ColliderInfo.OwnerAlignement == GridAlignment.LEFT)
                        BlackBoardBehaviour.Instance.LHSTotalDamage += ColliderInfo.Damage;
                    else if (ColliderInfo.OwnerAlignement == GridAlignment.RIGHT)
                        BlackBoardBehaviour.Instance.RHSTotalDamage += ColliderInfo.Damage;

                    damageDealt = true;
                }
                else if (knockback = damageScript as KnockbackBehaviour)
                {
                    Fixed32 totalKnockback = KnockbackBehaviour.GetTotalKnockback(ColliderInfo.BaseKnockBack, ColliderInfo.KnockBackScale, knockback.Health);
                    FVector3 force = knockback.Physics.CalculatGridForce(totalKnockback, newHitAngle, true);
                    knockback.Physics.ApplyImpulseForce(force);
                    damageDealt = true;
                }

                if (ColliderInfo.HitEffectLevel > 0 && damageDealt)
                {
                    Instantiate(BlackBoardBehaviour.Instance.HitEffects[ColliderInfo.HitEffectLevel - 1], attachedGameObject.transform.position + (.5f * Vector3.up), transform.rotation);
                    SoundManagerBehaviour.Instance.PlayHitSound(ColliderInfo.HitEffectLevel);
                }
            }

            SoundManagerBehaviour.Instance.PlaySound(ColliderInfo.HitSound);
            ColliderInfo.OnHit?.Invoke(collision);

            ColliderInfo.HitAngle = defaultAngle;
            if (ColliderInfo.DestroyOnHit)
                ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
        }

        private void ResolvePriority(GameObject attachedGameObject, HitColliderBehaviour hitCollider)
        {
            if (ComparePriority(hitCollider))
            {
                return;
            }

            if (ColliderInfo.Priority >= hitCollider.ColliderInfo.Priority)
                ObjectPoolBehaviour.Instance.ReturnGameObject(attachedGameObject);

            if (hitCollider.ColliderInfo.HitSpark)
                Instantiate(hitCollider.ColliderInfo.HitSpark, transform.position, Camera.main.transform.rotation);

            if (ColliderInfo.Priority == hitCollider.ColliderInfo.Priority)
            {
                Instantiate(BlackBoardBehaviour.Instance.ClashEffect, transform.position, Camera.main.transform.rotation);
                SoundManagerBehaviour.Instance.PlayClashSound();
                //MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0.2f, 0.1f);
                CameraBehaviour.ShakeBehaviour.ShakeRotation();
            }
        }

        private bool ComparePriority(HitColliderBehaviour hitCollider)
        {
            return (ColliderInfo.Priority < hitCollider.ColliderInfo.Priority || hitCollider.ColliderInfo.Priority == -1
                            || ColliderInfo.Priority == -1) && ColliderInfo.AbilityType != AbilityType.BURST;
        }

        public override void OnOverlapEnter(Collision collision)
        {
            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = collision.OtherCollider.OwnerPhysicsComponent.gameObject;

            if (Collisions.ContainsKey(otherGameObject) || ColliderInfo.IsMultiHit || collision.OtherEntity == Spawner || (otherGameObject.CompareTag("Reflector") && ColliderInfo.AbilityType != AbilityType.UNBLOCKABLE))
                return;

            if (Collisions.Count > 0 && ColliderInfo.DestroyOnHit) return;
            ResolveCollision(otherGameObject, collision);
        }

        public override void OnOverlapStay(Collision collision)
        {
            GameObject otherGameObject = collision.OtherCollider.OwnerPhysicsComponent.gameObject;
            //Only allow damage to be applied this way if the collider is a multi-hit collider
            if (!ColliderInfo.IsMultiHit || !CheckHitTime(otherGameObject))
                return;

            if (collision.OtherEntity == Spawner || (otherGameObject.CompareTag("Reflector") && ColliderInfo.AbilityType != AbilityType.UNBLOCKABLE))
                return;

            ResolveCollision(otherGameObject, collision);
        }

        public override void OnHitEnter(Collision collision)
        {
            //If the other object has a rigid body attached grab the game object attached to the rigid body and collider script.
            GameObject otherGameObject = collision.OtherCollider.OwnerPhysicsComponent.gameObject;

            if (Collisions.ContainsKey(otherGameObject) || ColliderInfo.IsMultiHit || collision.OtherEntity == Spawner || (otherGameObject.CompareTag("Reflector") && ColliderInfo.AbilityType != AbilityType.UNBLOCKABLE))
                return;

            if (Collisions.Count > 0 && ColliderInfo.DestroyOnHit) return;

            ResolveCollision(otherGameObject, collision);
        }

        private void Update()
        {
            if (gameObject == null)
                return;

            if (!_playedSpawnEffects)
            {
                if (ColliderInfo.SpawnEffect)
                    Instantiate(ColliderInfo.SpawnEffect, transform.position, Camera.main.transform.rotation);

                SoundManagerBehaviour.Instance.PlaySound(ColliderInfo.SpawnSound);
                _playedSpawnEffects = true;
            }

            _addedToActiveList = true;
            //Update the amount of current frames
            CurrentTimeActive = Time.time * RoutineBehaviour.Instance.CharacterTimeScale - StartTime;

            //Destroy the hit collider if it has exceeded or reach its maximum time active
            if (CurrentTimeActive >= ColliderInfo.TimeActive && ColliderInfo.DespawnAfterTimeLimit)
            {
                if (ColliderInfo.HitSpark)
                    Instantiate(ColliderInfo.HitSpark, transform.position, Camera.main.transform.rotation);

                ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
            }
        }
    }
}


