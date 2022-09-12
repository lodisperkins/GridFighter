using System.Collections;
using System.Collections.Generic;
using Lodis.Movement;
using Lodis.Utility;
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
            HitColliderData hitColliderData = GetColliderData(0);

            GameObject barrier = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);

            HitColliderBehaviour instantiatedCollider = null;

            if (!barrier.TryGetComponent(out instantiatedCollider))
                instantiatedCollider = barrier.AddComponent<HitColliderBehaviour>();
           
            instantiatedCollider.ColliderInfo = hitColliderData;


            _ownerKnockBackScript.Physics.FreezeInPlaceByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);

            _ownerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            if (!_ownerKnockBackScript.Physics.IsGrounded)
                _ownerKnockBackScript.CurrentAirState = AirState.FREEFALL;
        }

        protected override void End()
        {
            base.End();
        }
    }
}