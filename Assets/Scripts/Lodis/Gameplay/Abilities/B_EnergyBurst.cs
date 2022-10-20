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
        private GameObject _barrier;
        private GameObject _burstEffect;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            if (!_burstEffect) _burstEffect = Resources.Load<GameObject>("AbilityData/Effects/EnergyBurst");
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            HitColliderData hitColliderData = GetColliderData(0);

            _barrier = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);

            HitColliderBehaviour instantiatedCollider = null;

            if (!_barrier.TryGetComponent(out instantiatedCollider))
                instantiatedCollider = _barrier.AddComponent<HitColliderBehaviour>();
           
            instantiatedCollider.ColliderInfo = hitColliderData;

            Object.Instantiate(_burstEffect, owner.transform.position, Camera.main.transform.rotation);

            _ownerKnockBackScript.Physics.FreezeInPlaceByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);

            _ownerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);
            _ownerKnockBackScript.CancelHitStun();
            _ownerKnockBackScript.CancelStun();
        }

        protected override void Deactivate()
        {
            base.Deactivate();

            if (!_ownerKnockBackScript.Physics.IsGrounded)
                _ownerKnockBackScript.CurrentAirState = AirState.FREEFALL;
            else
            {
                _ownerKnockBackScript.CurrentAirState = AirState.NONE;
                _ownerKnockBackScript.Physics.RB.isKinematic = true;
            }

            ObjectPoolBehaviour.Instance.ReturnGameObject(_barrier);
        }

        protected override void End()
        {
            base.End();
        }

        public override void StopAbility()
        {
            base.StopAbility();
            if (!_ownerKnockBackScript.Physics.IsGrounded)
                _ownerKnockBackScript.CurrentAirState = AirState.TUMBLING;

            _ownerKnockBackScript.DisableInvincibility();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_barrier);
        }
    }
}