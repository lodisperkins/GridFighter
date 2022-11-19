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
        private float _defaultRestTime;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            if (!_burstEffect) _burstEffect = Resources.Load<GameObject>("AbilityData/Effects/EnergyBurst");
            _defaultRestTime = abilityData.recoverTime;
        }

        protected override void Start(params object[] args)
        {
            base.Start(args);


            _ownerKnockBackScript.Physics.FreezeInPlaceByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);

            _ownerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);
            _ownerKnockBackScript.CancelHitStun();
            _ownerKnockBackScript.CancelStun();
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
            instantiatedCollider.Owner = owner;

            Object.Instantiate(_burstEffect, owner.transform.position, Camera.main.transform.rotation);
            if (!_ownerKnockBackScript.Physics.IsGrounded)
                _ownerKnockBackScript.CurrentAirState = AirState.FREEFALL;
        }

        private void ResetState()
        {
            if (_ownerKnockBackScript.Physics.IsGrounded)
            {
                _ownerMoveScript.EnableMovement();
                _ownerKnockBackScript.CurrentAirState = AirState.NONE;
                _ownerKnockBackScript.Physics.RB.isKinematic = true;
            }

            ObjectPoolBehaviour.Instance.ReturnGameObject(_barrier);
            _ownerKnockBackScript.DisableInvincibility();
        }

        protected override void Deactivate()
        {
            base.Deactivate();
            ResetState();
        }
        public override void StopAbility()
        {
            base.StopAbility();

            ResetState();
        }

        public override void Update()
        {
            base.Update();
            if (CurrentAbilityPhase == AbilityPhase.RECOVER && !_ownerKnockBackScript.Physics.IsGrounded && InUse)
                EndAbility();
        }
    }
}