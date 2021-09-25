using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_EnergyBarrier : Ability
    {
        private HitColliderBehaviour _barrierCollider;
        private GameObject _visualPrefabInstance;
        private Collider _prefabeInstanceCollider;
        private HealthBehaviour _ownerHealth;
        private bool _reflected;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            abilityData.canCancelActive = false; abilityData.canCancelRecover = false;
            _ownerHealth = owner.GetComponent<HealthBehaviour>();
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _barrierCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("KnockBackScale"),
                abilityData.GetCustomStatValue("HitAngle"), true, abilityData.timeActive, owner, true, false);
            _barrierCollider.onHit += arguments => { abilityData.canCancelActive = true; abilityData.canCancelRecover = true; _reflected = true; };

            Vector3 offset = new Vector3(abilityData.GetCustomStatValue("XOffset"), abilityData.GetCustomStatValue("YOffset"), 0) * owner.transform.forward.x;
            _visualPrefabInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position + offset, new Quaternion());
            HitColliderBehaviour instanceBehaviour = _visualPrefabInstance.AddComponent<HitColliderBehaviour>();
            HitColliderBehaviour.Copy(_barrierCollider, instanceBehaviour);

            _prefabeInstanceCollider = _visualPrefabInstance.GetComponent<BoxCollider>();

            _ownerHealth.SetInvincibilityByCondition(condition => !InUse || CurrentAbilityPhase == AbilityPhase.RECOVER);

        }

        public void TryReflectProjectile(GameObject gameObject)
        {
            ColliderBehaviour otherHitCollider = gameObject.GetComponentInParent<ColliderBehaviour>();
            Rigidbody otherRigidbody = gameObject.GetComponentInParent<Rigidbody>();

            if (otherHitCollider && otherRigidbody && !otherHitCollider.CompareTag("Player") && !otherHitCollider.CompareTag("Entity"))
            {
                otherHitCollider.ColliderOwner = owner;
                otherHitCollider.ResetActiveTime();
                otherRigidbody.AddForce(-otherRigidbody.velocity * 2, ForceMode.Impulse);

                EndAbility();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (CurrentAbilityPhase != AbilityPhase.ACTIVE || !_visualPrefabInstance)
                return;

            Collider[] collisions = Physics.OverlapBox(_visualPrefabInstance.transform.position, _prefabeInstanceCollider.bounds.extents / 2);

            foreach (Collider collider in collisions)
            {
                if (collider.gameObject != _visualPrefabInstance)
                    TryReflectProjectile(collider.gameObject);
            }
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            MonoBehaviour.Destroy(_visualPrefabInstance);
        }
    }
}