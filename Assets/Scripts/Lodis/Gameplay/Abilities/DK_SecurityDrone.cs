using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_SecurityDrone : Ability
    {
        private Secur_TBehaviour secur_T;

	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            if (secur_T == null)
            {
                GameObject botInstance = MonoBehaviour.Instantiate(abilityData.visualPrefab, Owner.transform.position, Quaternion.identity);
                secur_T = botInstance.GetComponent<Secur_TBehaviour>();
            }
            else
            {
                secur_T.Entity.AddToGame();
                secur_T.ResetTimer();
            }

            GameObject opp = BlackBoardBehaviour.Instance.GetOpponentForPlayer(Owner);

            Fixed32 speed = abilityData.GetCustomStatValue("ProjectileSpeed");

            secur_T.Initialize(Owner, opp.GetComponent<EntityDataBehaviour>(), GetColliderData(0), speed);
        }

        protected override void OnMatchRestart()
        {
            base.OnMatchRestart();

            if (secur_T != null)
            {
                secur_T.Deactivate();
            }
        }
    }
}