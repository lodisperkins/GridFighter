using System.Collections;
using System.Collections.Generic;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Sound;
using Lodis.Utility;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// An ability shared by all that can be used to break combos.
    /// </summary>
    public class B_DefensiveBurst : Ability
    {
        private GameObject _barrier;
        private GameObject _burstEffect;
        private float _defaultRestTime;
        private bool _makeFreeFall;
        private TimedAction _zoomAction;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
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
            OnHit += collision =>
            {
                GameObject objectHit = collision.OtherEntity.UnityObject;

                if (objectHit != BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner.gameObject))
                    return;

                CameraBehaviour.Instance.ZoomAmount = 3;
                _zoomAction = RoutineBehaviour.Instance.StartNewTimedAction(parameter => CameraBehaviour.Instance.ZoomAmount = 0, TimedActionCountType.SCALEDTIME, 0.7f);
                AnnouncerBehaviour.Instance.MakeAnnouncement(BlackBoardBehaviour.Instance.GetIDFromPlayer(Owner.gameObject), "Burst Counter");
                if (OwnerKnockBackScript.CurrentAirState == AirState.NONE)
                    return;

                OwnerKnockBackScript.LandingScript.CancelLanding();

                OwnerKnockBackScript.DisableInvincibility();
                OwnerKnockBackScript.Physics.CancelFreeze();
                PanelBehaviour panel;

                bool validPanel = BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(Owner.transform.position, out panel);

                if (validPanel)
                {
                    OwnerMoveScript.TeleportToPanel(panel);
                    OwnerMoveScript.EnableMovement();
                    OwnerKnockBackScript.Physics.StopAllForces();
                    OwnerKnockBackScript.CurrentAirState = AirState.NONE;
                }

            };
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            HitColliderData hitColliderData = GetColliderData(0);

            //Try to get a barrier from the pool to use as the hit box
            _barrier = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, Owner.transform.position, Owner.transform.rotation);

            HitColliderBehaviour instantiatedCollider = null;

            //Add a hitcollider if there isn't one attached in order to deal damage
            if (!_barrier.TryGetComponent(out instantiatedCollider))
                instantiatedCollider = _barrier.AddComponent<HitColliderBehaviour>();

            //Update the new colliders data
            instantiatedCollider.InitCollider(1, 1, _barrier.GetComponent<SimulationBehaviour>().Entity.Data);
            instantiatedCollider.ColliderInfo = hitColliderData;

            //Spawns a new particle effect at this player's position
            Object.Instantiate(_burstEffect, Owner.transform.position, Camera.main.transform.rotation);

            //If the player isn't resting on the ground...
            if (OwnerKnockBackScript.CurrentAirState != AirState.NONE && !OwnerKnockBackScript.Physics.IsGrounded)
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

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            ResetState();

            //If the player isn't resting on the ground...
            if (OwnerKnockBackScript.CurrentAirState != AirState.NONE && !OwnerKnockBackScript.Physics.IsGrounded)
                //...put them in freefall
                OwnerKnockBackScript.CurrentAirState = AirState.FREEFALL;
            else
                OwnerKnockBackScript.CurrentAirState = AirState.NONE;
        }

        protected override void OnEnd()
        {
            ResetState();
            OwnerKnockBackScript.Physics.IgnoreForces = false;
            CameraBehaviour.Instance.ZoomAmount = 0;
            RoutineBehaviour.Instance.StopAction(_zoomAction);
        }

        public override void Update()
        {
            base.Update();
            if (CurrentAbilityPhase == AbilityPhase.RECOVER && !OwnerKnockBackScript.Physics.IsGrounded && InUse)
                EndAbility();
        }
    }
}