using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_PhaseDash : Ability
    {
        private float _travleDistance;
        private FVector2 _moveDirection;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(Owner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _travleDistance = abilityData.GetCustomStatValue("TravelDistance");
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {

            OwnerMoveScript.CancelMovement();
            OwnerMoveScript.Position = OwnerMoveScript.CurrentPanel.Position;

            OwnerKnockBackScript.SetIntagibilityByCondition(condition => !InUse || CurrentAbilityPhase != AbilityPhase.ACTIVE);
            OwnerMoveScript.MoveToPanel(OwnerMoveScript.Position + (_moveDirection * _travleDistance), false, OwnerMoveScript.Alignment, false, true, true);
            OwnerAnimationScript.PlayMovementAnimation();
        }

        public override void Update()
        {
            base.Update();
            _moveDirection = (FVector2)OwnerInput.AttackDirection;
        }
    }
}