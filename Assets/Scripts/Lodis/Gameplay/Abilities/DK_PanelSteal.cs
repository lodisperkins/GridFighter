using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Steals a row of panels in front
    /// </summary>
    public class DK_PanelSteal : Ability
    {
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Call row stealing func
            BlackBoardBehaviour.Instance.Grid.ExchangeRowsByTimer((int)abilityData.GetCustomStatValue("AmountOfRows"), _ownerMoveScript.Alignment, abilityData.GetCustomStatValue("OwnershipTime"));
        }

        public override void EndAbility()
        {
            base.EndAbility();
        }

        public override void StopAbility()
        {
            base.StopAbility();
            BlackBoardBehaviour.Instance.Grid.CancelRowExchange();
        }
    }
}