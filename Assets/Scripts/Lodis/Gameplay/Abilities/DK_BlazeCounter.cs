using FixedPoints;
using Lodis.GridScripts;
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
    public class DK_BlazeCounter : Ability
    {
        private Fixed32 _counterTime;
        private HitColliderBehaviour _hitCollider;
        private FixedTimeAction _dashTimer;
        private FixedTimeAction _counterTimer;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);

            _counterTime = abilityData.GetCustomStatValue("CounterTime");
        }

        protected void OnArmorBroken()
        {
            _hitCollider = HitColliderSpawner.SpawnCollider(Owner.FixedTransform, 1, 1, GetColliderData(0), Owner);
            Fixed32 travelDistance = abilityData.GetCustomStatValue("DashDistance");

            FVector2 velocity = new FVector2(travelDistance * OwnerMoveScript.GetAlignmentX(), 0);

            OwnerMoveScript.Move(velocity, tempAlignment: GridAlignment.ANY, canBeOccupied: true, reservePanel: false, clampPosition: true);

            AnimationClip dashClip;

            abilityData.GetAdditionalAnimation(0, out dashClip);

            OwnerAnimationScript.PlayAnimation(OwnerMoveScript.TravelTime / 2, dashClip);

            Fixed32 dashRestTime = abilityData.GetCustomStatValue("DashRestTime");

            _dashTimer = FixedPointTimer.StartNewTimedAction(EndAbility, OwnerMoveScript.TravelTime + dashRestTime);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            PauseAbilityTimer();
            OwnerKnockBackScript.EnableCounterStance(_counterTime, OnArmorBroken);
            OwnerKnockBackScript.AddOnCounterStanceInactiveAction(DisableCounterStance);

            OwnerMoveScript.MoveToAlignedSideWhenStuck = false;
        }

        protected void DisableCounterStance()
        {
            _counterTimer = FixedPointTimer.StartNewTimedAction(UnpauseAbilityTimer, abilityData.GetCustomStatValue("DashRestTime"));
        }

        protected void ReturnHitCollider()
        {
            if (_hitCollider != null)
            {
                _hitCollider.FixedTransform.Parent = null;
                ObjectPoolBehaviour.Instance.ReturnGameObject(_hitCollider.Entity);
            }
        }

        protected override void OnRecover(params object[] args)
        {
            base.OnRecover(args);

            ReturnHitCollider();

            OwnerKnockBackScript.DisableSuperArmor();
        }

        protected override void OnEnd()
        {
            base.OnEnd();

            _dashTimer?.Stop();
            _counterTimer?.Stop();

            ReturnHitCollider();

            OwnerKnockBackScript.DisableSuperArmor();
            OwnerKnockBackScript.RemoveOnArmorBrokenAction(OnArmorBroken);
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            _dashTimer?.Stop();
            _counterTimer?.Stop();

            ReturnHitCollider();
            OwnerKnockBackScript.DisableSuperArmor();
            OwnerKnockBackScript.RemoveOnArmorBrokenAction(OnArmorBroken);
            OwnerMoveScript.MoveToAlignedSideWhenStuck = true;
        }
    }
}