using FixedPoints;
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
        private List<PanelBehaviour> _targetPanels = new List<PanelBehaviour>();

	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(Owner);
            OnMoveEndAction += DisableAllEntities;
            SmoothMovement = true;
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            Alignement = OwnerMoveScript.Alignment;
            GridBehaviour grid = BlackBoardBehaviour.Instance.Grid;
            for (int i = 0; i < grid.Dimensions.y; i++)
            {
                PanelPositions[i] = new FVector2(grid.TempMaxColumns + OwnerMoveScript.GetAlignmentX(), i);
            }
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Call row stealing func
            BlackBoardBehaviour.Instance.Grid.ExchangeRowsByTimer((int)abilityData.GetCustomStatValue("AmountOfRows"), OwnerMoveScript.Alignment, abilityData.GetCustomStatValue("OwnershipTime"));
            base.OnActivate();
        }
    }
}