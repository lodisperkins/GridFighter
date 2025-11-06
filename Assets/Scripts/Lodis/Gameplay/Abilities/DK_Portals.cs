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

            FVector3 forwardOffset = Owner.FixedTransform.Forward * offset;
            FVector3 offsetPosition = Owner.FixedTransform.WorldPosition + forwardOffset + FVector3.Up / 2;

            entity = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), offsetPosition, Owner.FixedTransform.WorldRotation);

            if (currentActivationAmount == 1)
            {
                _teleporter1 = entity.GetComponent<TeleporterBehaviour>();
                _teleport1Pos = offsetPosition;
            }
            else if (currentActivationAmount == 2)
            {
                _teleporter2 = entity.GetComponent<TeleporterBehaviour>();

                _teleporter1.InitTeleporter(_teleporter2, Owner);
                _teleporter2.InitTeleporter(_teleporter1, Owner);

                if (_teleport1Pos == offsetPosition)
                {
                    FVector3 panelOffset = Owner.FixedTransform.Forward * (GridBehaviour.Instance.FixedPanelScale.X + GridBehaviour.Instance.FixedPanelSpacingX);

                    if (!GridBehaviour.Instance.CheckIfPositionInRange(offsetPosition + panelOffset))
                    {
                        offsetPosition -= panelOffset;
                    }
                    else
                    {
                        offsetPosition += panelOffset;
                    }

                    entity.FixedTransform.WorldPosition = offsetPosition;
                }
            }
        }

        private void OnKnockback()
        {
            if (CurrentAbilityPhase == AbilityPhase.STARTUP && _teleporter1 != null)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(_teleporter1.Entity);
            }
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