using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SB_FlareLauncher : ProjectileAbility
    {
	    //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            ScaleStats = true;
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //The base activate func fires a single instance of the projectile when called
            base.OnActivate(args);
        }
    }
}