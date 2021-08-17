using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_ElectricTraps : ProjectileAbility
    {
        private float travelDistance;
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            travelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");
        }

        private void FireLink()
        {
            GameObject visualPrefab = MonoBehaviour.Instantiate(abilityData.visualPrefab, spawnTransform);
            Movement.GridMovementBehaviour gridMovement = visualPrefab.GetComponent<Movement.GridMovementBehaviour>();
            
            Vector2 moveOffset = new Vector2(travelDistance, 0);

            for (int i = 0; i < travelDistance; i++)
            {
                if (gridMovement.MoveToPanel(_ownerMoveScript.CurrentPanel.Position + moveOffset * owner.transform.forward, false, GridScripts.GridAlignment.ANY))
                    break;
            }
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            
            switch (abilityData.currentActivationAmount)
            {
                case 1:
                    base.Activate(args);
                    break;
            }
        }
    }
}