using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// An ability shared by all that can be used to break combos.
    /// </summary>
    public class B_EnergyBurst : Ability
    {
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            HitColliderBehaviour hitCollider = GetColliderBehaviourCopy(0);

            GameObject barrier = MonoBehaviour.Instantiate(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);
            HitColliderBehaviour instantiatedCollider = barrier.AddComponent<HitColliderBehaviour>();
            HitColliderBehaviour.Copy(hitCollider, instantiatedCollider);

            _ownerKnockBackScript.IsTumbling = false;
            _ownerKnockBackScript.CancelHitStun();
            _ownerKnockBackScript.CancelStun();
            _ownerKnockBackScript.Physics.StopAllForces();

            _ownerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER);
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            if (!_ownerKnockBackScript.Physics.IsGrounded || _ownerKnockBackScript.IsTumbling)
                _ownerKnockBackScript.InFreeFall = true;
        }

        protected override void End()
        {
            base.End();
            _ownerKnockBackScript.Physics.UseGravity = true;
        }
    }
}