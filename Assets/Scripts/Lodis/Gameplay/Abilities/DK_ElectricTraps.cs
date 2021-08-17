using Lodis.GridScripts;
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
        private float _maxTravelDistance;
        private List<Movement.GridMovementBehaviour> _linkMoveScripts;
        private GameObject _attackLinkVisual;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _maxTravelDistance = abilityData.GetCustomStatValue("PanelTravelDistance");
            _attackLinkVisual = (GameObject)Resources.Load("Effects/LobShot");
        }

        private void FireLink()
        {
            GameObject visualPrefab = MonoBehaviour.Instantiate(abilityData.visualPrefab, spawnTransform);
            Movement.GridMovementBehaviour gridMovement = visualPrefab.GetComponent<Movement.GridMovementBehaviour>();
            _linkMoveScripts.Add(gridMovement);

            for (int i = (int)_maxTravelDistance; i >= 0; i--)
            {
                Vector2 moveOffset = new Vector2(i, 0);
                if (gridMovement.MoveToPanel(_ownerMoveScript.CurrentPanel.Position + moveOffset * owner.transform.forward, false, GridScripts.GridAlignment.ANY))
                    break;
            }
        }

        private void ActivateStunPath()
        {
            List<PanelBehaviour> panels = AI.AIUtilities.Instance.GetPath(_linkMoveScripts[0].CurrentPanel, _linkMoveScripts[1].CurrentPanel, true);

            for (int i = 0; i < panels.Count; i++)
            {
                if (i + 1 != panels.Count)
                {

                }
            }

        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            
            switch (abilityData.currentActivationAmount)
            {
                case 1:
                case 2:
                    FireLink();
                    break;
                case 3:
                    break;
            }
        }
    }
}