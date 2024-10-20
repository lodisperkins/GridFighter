using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Sound;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class B_OffensiveBurst : Ability
    {
        private EntityDataBehaviour _barrier;
        private GameObject _burstEffect;
        private float _defaultRestTime;
        private bool _makeFreeFall;
        private TimedAction _zoomAction;
        private HitStopBehaviour _ownerHitStop;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
            if (!_burstEffect) _burstEffect = abilityData.visualPrefab;
            _defaultRestTime = abilityData.recoverTime;
            _ownerHitStop = Owner.GetComponent<HitStopBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _makeFreeFall = OwnerKnockBackScript.CurrentAirState != AirState.NONE;

            //Freezes all forces in the knockback and physics components
            OwnerKnockBackScript.Physics.CancelFreeze();
            OwnerKnockBackScript.Physics.IsKinematic = true;
            OwnerKnockBackScript.Physics.FreezeInPlaceByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);
            OwnerKnockBackScript.SetInvincibilityByCondition(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse);
            OwnerKnockBackScript.CancelHitStun();
            OwnerKnockBackScript.CancelStun();

            //Disable ability benefits if the player is hit out of burst
            OnHit += collision =>
            {

                GameObject objectHit = collision.OtherEntity.UnityObject;

                if (objectHit != BlackBoardBehaviour.Instance.GetOpponentForPlayer(collision.OtherEntity.UnityObject))
                    return;

                CameraBehaviour.Instance.ZoomAmount = 3;
                _zoomAction = RoutineBehaviour.Instance.StartNewTimedAction(parameter => CameraBehaviour.Instance.ZoomAmount = 0, TimedActionCountType.SCALEDTIME, 0.7f);
                AnnouncerBehaviour.Instance.MakeAnnouncement(BlackBoardBehaviour.Instance.GetIDFromPlayer(collision.OtherEntity.UnityObject), "Burst Drive");
                if (OwnerKnockBackScript.CurrentAirState == AirState.NONE)
                    return;

                OwnerKnockBackScript.DisableInvincibility();
                OwnerKnockBackScript.Physics.CancelFreeze();
                OwnerKnockBackScript.LandingScript.CancelLanding();

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
            _barrier = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), Owner.FixedTransform.WorldPosition, Owner.FixedTransform.WorldRotation);

            HitColliderBehaviour instantiatedCollider = null;

            //Add a hitcollider if there isn't one attached in order to deal damage
            if (!_barrier.TryGetComponent(out instantiatedCollider))
                instantiatedCollider = _barrier.Data.AddComponent<HitColliderBehaviour>();

            //Update the new colliders data
            instantiatedCollider.InitCollider(5, 5, Owner);
            instantiatedCollider.ColliderInfo = hitColliderData;

            //Spawns a new particle effect at this player's position
            //Object.Instantiate(_burstEffect, Owner.transform.position, Camera.main.transform.rotation);

            //If the player isn't resting on the ground...
            if (OwnerKnockBackScript.CurrentAirState != AirState.NONE && !OwnerKnockBackScript.Physics.IsGrounded)
                //...put them in freefall
                OwnerKnockBackScript.CurrentAirState = AirState.FREEFALL;

            OwnerKnockBackScript.Physics.IsKinematic = false;
        }

        private void ResetState()
        {
            if (OwnerKnockBackScript.Physics.IsGrounded)
            {
                OwnerMoveScript.EnableMovement();
                OwnerKnockBackScript.LandingScript.CancelLanding();
                OwnerKnockBackScript.CurrentAirState = AirState.NONE;
            }

            ObjectPoolBehaviour.Instance.ReturnGameObject(_barrier);
            OwnerKnockBackScript.DisableInvincibility();
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);
            ResetState();
        }

        protected override void OnEnd()
        {
            ResetState();
            OwnerKnockBackScript.Physics.IsKinematic = false;
            CameraBehaviour.Instance.ZoomAmount = 0;
            RoutineBehaviour.Instance.StopAction(_zoomAction);
        }

        public override void Tick(Fixed32 dt)
        {
            if (CurrentAbilityPhase == AbilityPhase.RECOVER && !OwnerKnockBackScript.Physics.IsGrounded && InUse)
                EndAbility();
        }
    }
}