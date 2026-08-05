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
    public class DK_Portals : Ability
    {
        protected TeleporterBehaviour _teleporter1;
        protected TeleporterBehaviour _teleporter2;
        protected FVector3 _teleport1Pos;

	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);

            OwnerKnockBackScript.AddOnKnockBackAction(OnKnockback);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            Fixed32 offset = abilityData.GetCustomStatValue("Offset");
            EntityDataBehaviour entity = null;

            PanelBehaviour teleportPanel1;
            PanelBehaviour teleportPanel2;

            if (!FindPortalSpawnPanels(out teleportPanel1, out teleportPanel2))
            {
                return;
            }

            FVector3 portalPosition1 = teleportPanel1.FixedWorldPosition + FVector3.Up * abilityData.GetCustomStatValue("PortalHeight");
            FVector3 portalPosition2 = teleportPanel2.FixedWorldPosition + FVector3.Up * abilityData.GetCustomStatValue("PortalHeight");

            //Spawn portal 1
            entity = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), portalPosition1, Owner.FixedTransform.WorldRotation);
            _teleporter1 = entity.GetComponent<TeleporterBehaviour>();
            _teleporter1.name = "Teleporter 1";
            entity.FixedTransform.WorldPosition = portalPosition1;

            //Spawn Portal 2
            entity = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), portalPosition2, Owner.FixedTransform.WorldRotation);
            _teleporter2 = entity.GetComponent<TeleporterBehaviour>();
            entity.FixedTransform.WorldPosition = portalPosition2;
            _teleporter2.name = "Teleporter 2";

            _teleporter1.InitTeleporter(_teleporter2, Owner);
            _teleporter2.InitTeleporter(_teleporter1, Owner);


            //if (currentActivationAmount == 1)
            //{

            //}
            //else if (currentActivationAmount == 2)
            //{

            //}
        }

        private void OnKnockback()
        {
            if (CurrentAbilityPhase == AbilityPhase.STARTUP && _teleporter1 != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_teleporter1.Entity);
            }
        }
        

        /// <summary>
        /// Finds spawn panels for both portals on the farthest row of the opponent's side.
        /// Bottom portal at y=0, top portal at y=2. Falls back to closer rows if panels are disabled.
        /// </summary>
        /// <returns>True if both panels were found, false otherwise.</returns>
        protected bool FindPortalSpawnPanels(out PanelBehaviour bottomPanel, out PanelBehaviour topPanel)
        {
            bottomPanel = null;
            topPanel = null;

            GridBehaviour grid = GridBehaviour.Instance;
            int maxX = (int)grid.Dimensions.x;

            // Determine the starting column (farthest opponent row) and search direction
            int startX;
            int step;

            if (OwnerMoveScript.Alignment == GridAlignment.LEFT)
            {
                // Opponent is on the right, start from the farthest right column
                startX = maxX - 1;
                step = -1;
            }
            else
            {
                // Opponent is on the left, start from column 0
                startX = 0;
                step = 1;
            }

            // Search backwards from the farthest row until we find enabled panels
            for (int x = startX; x >= 0 && x < maxX; x += step)
            {
                PanelBehaviour candidateBottom = null;
                PanelBehaviour candidateTop = null;

                if (grid.GetPanel(x, 0, out candidateBottom) && candidateBottom.PanelEnabled)
                {
                    bottomPanel = candidateBottom;
                }

                if (grid.GetPanel(x, 2, out candidateTop) && candidateTop.PanelEnabled)
                {
                    topPanel = candidateTop;
                }

                // Only return once both are found on the same column
                if (bottomPanel != null && topPanel != null)
                    return true;

                // Reset if only one was found on this column
                bottomPanel = null;
                topPanel = null;
            }

            return false;
        }

        public override void OnDeckReshuffle()
        {
            if (_teleporter1 != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_teleporter1.Entity);
            }

            if (_teleporter2 != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_teleporter2.Entity);
            }
        }
    }
}