﻿using System.Collections;
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
        private Vector2 _moveDirection;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
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
            _moveDirection = OwnerInput.AttackDirection;
        }
    }
}