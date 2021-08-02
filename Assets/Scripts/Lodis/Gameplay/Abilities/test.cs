using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class test : Ability
    {

	public test()
	{
		Debug.Log("Constructor Called");
	}
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {

        }
    }
}