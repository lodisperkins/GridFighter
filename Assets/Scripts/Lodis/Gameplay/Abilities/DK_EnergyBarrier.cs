﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Creates a shield in front
    /// that reflects projectiles and pushes enemies away.
    /// </summary>
    public class DK_EnergyBarrier : Ability
    {
        private HitColliderData _barrierCollider;
        private GameObject _visualPrefabInstance;
        private Collider _prefabeInstanceCollider;
        private HealthBehaviour _ownerHealth;
        private bool _reflected;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            //Get owner health
            _ownerHealth = owner.GetComponent<HealthBehaviour>();
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Create barrier collider
            _barrierCollider = GetColliderData(0);

            //Set the position of the barrier in relation to the character
            Vector3 offset = new Vector3(abilityData.GetCustomStatValue("XOffset") * owner.transform.forward.x, abilityData.GetCustomStatValue("YOffset"), 0);

            //Create barrier
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position + offset, new Quaternion());

            //Attach hit box to barrier
            HitColliderBehaviour instanceBehaviour = _visualPrefabInstance.AddComponent<HitColliderBehaviour>();
            instanceBehaviour.ColliderInfo = _barrierCollider;

            //Store barrier collider
            _prefabeInstanceCollider = _visualPrefabInstance.GetComponent<BoxCollider>();

            //Give the player invinicibility while using the barrier to prevent easy over-head hits
            _ownerHealth.SetInvincibilityByCondition(condition => !InUse || CurrentAbilityPhase == AbilityPhase.RECOVER);

        }

        /// <summary>
        /// Checks if the object it collided with is an enemy projectile.
        /// If so, reverses velocity
        /// </summary>
        /// <param name="gameObject"></param>
        public void TryReflectProjectile(GameObject gameObject)
        {
            //Get collider and rigidbody to check the owner and add the force
            HitColliderBehaviour otherHitCollider = gameObject.GetComponentInParent<HitColliderBehaviour>();
            Rigidbody otherRigidbody = gameObject.GetComponentInParent<Rigidbody>();

            //If the object collided with is an enemy projectile...
            if (otherHitCollider && otherRigidbody && !otherHitCollider.CompareTag("Player") && !otherHitCollider.CompareTag("Entity"))
            {
                //...reset the active time and reverse its velocity
                otherHitCollider.Owner = owner;
                otherHitCollider.ResetActiveTime();
                otherRigidbody.AddForce(-otherRigidbody.velocity * 2, ForceMode.Impulse);

            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Return if the barrier isn't active
            if (CurrentAbilityPhase != AbilityPhase.ACTIVE || !_visualPrefabInstance)
                return;

            //Create collision volume for barrier
            Collider[] collisions = Physics.OverlapBox(_visualPrefabInstance.transform.position, _prefabeInstanceCollider.bounds.extents / 2);

            //Reflect each projectile that hit the barrier
            foreach (Collider collider in collisions)
            {
                if (collider.gameObject != _visualPrefabInstance)
                    TryReflectProjectile(collider.gameObject);
            }
                
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            //Destroy the barrier
            Object.Destroy(_visualPrefabInstance);
        }
    }
}