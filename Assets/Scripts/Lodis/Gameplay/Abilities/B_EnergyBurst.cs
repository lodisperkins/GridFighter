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
        private bool _makeFreeFall;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            if (!_burstEffect) _burstEffect = Resources.Load<GameObject>("Effects/EnergyBurst");
            _defaultRestTime = abilityData.recoverTime;
        }

        protected override void Start(params object[] args)
        {
            base.Start(args);

            _makeFreeFall = _ownerKnockBackScript.CurrentAirState != AirState.NONE;

            //Freezes all forces in the knockback and physics components
            _ownerKnockBackScript.Physics.CancelFreeze();
            _ownerKnockBackScript.Physics.IgnoreForces = true;
            _ownerKnockBackScript.Physics.FreezeInPlaceByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);
            _ownerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);
            _ownerKnockBackScript.CancelHitStun();
            _ownerKnockBackScript.CancelStun();

            //Disable ability benefits if the player is hit out of burst
            OnHitTemp += arguments =>
            {
                if (_ownerKnockBackScript.Physics.IsGrounded)
                    return;

                GameObject objectHit = (GameObject)arguments[0];

                if (objectHit != BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner))
                    return;

                _ownerKnockBackScript.DisableInvincibility();
                _ownerKnockBackScript.Physics.CancelFreeze();
            };
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            HitColliderData hitColliderData = GetColliderData(0);

            //Try to get a barrier from the pool to use as the hit box
            _barrier = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, owner.transform.position, owner.transform.rotation);

            HitColliderBehaviour instantiatedCollider = null;

            //Add a hitcollider if there isn't one attached in order to deal damage
            if (!_barrier.TryGetComponent(out instantiatedCollider))
                instantiatedCollider = _barrier.AddComponent<HitColliderBehaviour>();
           
            //Update the new colliders data
            instantiatedCollider.ColliderInfo = hitColliderData;
            instantiatedCollider.Owner = owner;

            //Spawns a new particle effect at this player's position
            Object.Instantiate(_burstEffect, owner.transform.position, Camera.main.transform.rotation);

            //If the player is resting on the ground...
            if (_ownerKnockBackScript.CurrentAirState != AirState.NONE)
                //...put them in freefall
                _ownerKnockBackScript.CurrentAirState = AirState.FREEFALL;

            _ownerKnockBackScript.Physics.IgnoreForces = false;
        }

        private void ResetState()
        {
            if (_ownerKnockBackScript.Physics.IsGrounded)
                _ownerMoveScript.EnableMovement();

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

        public override void EndAbility()
        {
            base.EndAbility();
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