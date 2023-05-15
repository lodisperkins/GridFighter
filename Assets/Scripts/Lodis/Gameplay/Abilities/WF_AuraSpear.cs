using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class WF_AuraSpear : ProjectileAbility
    {
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

        private void SpawnSword()
        {
            DisableAccessory();

            RoutineBehaviour.Instance.StartNewConditionAction(context => EnableAccessory(), condition => !Projectile.activeInHierarchy);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            CleanProjectileList();

            //Only fire if there aren't two many instances of this object active
            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            if (OwnerMoveScript.IsMoving)
            {
                OwnerMoveScript.AddOnMoveEndTempAction(() =>
                {
                    base.OnActivate(args);
                    SpawnSword();
                });
            }
            else
            {
                base.OnActivate(args);
                SpawnSword();
            }
            

        }
    }
}