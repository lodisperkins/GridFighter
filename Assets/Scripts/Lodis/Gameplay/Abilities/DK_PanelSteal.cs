using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Steals a row of panels in front
    /// </summary>
    public class DK_PanelSteal : SummonAbility
    {
        private List<PanelBehaviour> _targetPanels;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);


        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Call row stealing func
            BlackBoardBehaviour.Instance.Grid.ExchangeRowsByTimer((int)abilityData.GetCustomStatValue("AmountOfRows"), OwnerMoveScript.Alignment, abilityData.GetCustomStatValue("OwnershipTime"));
        }
    }
}