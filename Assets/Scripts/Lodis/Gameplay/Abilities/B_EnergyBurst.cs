using System.Collections;
using System.Collections.Generic;
using Lodis.GridScripts;
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

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _makeFreeFall = OwnerKnockBackScript.CurrentAirState != AirState.NONE;

            //Freezes all forces in the knockback and physics components
            OwnerKnockBackScript.Physics.CancelFreeze();
            OwnerKnockBackScript.Physics.IgnoreForces = true;
            OwnerKnockBackScript.Physics.FreezeInPlaceByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);
            OwnerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);
            OwnerKnockBackScript.CancelHitStun();
            OwnerKnockBackScript.CancelStun();

            //Disable ability benefits if the player is hit out of burst
            OnHit += arguments =>
            {
                if (OwnerKnockBackScript.CurrentAirState == AirState.NONE)
                    return;

                GameObject objectHit = (GameObject)arguments[0];

                if (objectHit != BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner))
                    return;

                OwnerKnockBackScript.DisableInvincibility();
                OwnerKnockBackScript.Physics.CancelFreeze();

                PanelBehaviour panel;

                bool validPanel = BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(owner.transform.position, out panel);

                if (validPanel)
                {
                    OwnerMoveScript.TeleportToPanel(panel);
                    OwnerMoveScript.EnableMovement();
                    OwnerKnockBackScript.Physics.RB.isKinematic = true;
                    OwnerKnockBackScript.CurrentAirState = AirState.NONE;
                }
            };
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
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

            //If the player isn't resting on the ground...
            if (OwnerKnockBackScript.CurrentAirState != AirState.NONE)
                //...put them in freefall
                OwnerKnockBackScript.CurrentAirState = AirState.FREEFALL;

            OwnerKnockBackScript.Physics.IgnoreForces = false;
        }

        private void ResetState()
        {
            if (OwnerKnockBackScript.Physics.IsGrounded)
                OwnerMoveScript.EnableMovement();

            ObjectPoolBehaviour.Instance.ReturnGameObject(_barrier);
            OwnerKnockBackScript.DisableInvincibility();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
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
            if (CurrentAbilityPhase == AbilityPhase.RECOVER && !OwnerKnockBackScript.Physics.IsGrounded && InUse)
                EndAbility();
        }
    }
}