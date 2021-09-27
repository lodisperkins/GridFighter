using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class DK_DoubleStrike : Ability
    {
        private Input.InputBehaviour _ownerInput;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            _ownerInput = owner.GetComponent<Input.InputBehaviour>();
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            Vector2 attackPosition;
            if (_ownerInput.AttackDirection == Vector2.zero)
                attackPosition = _ownerMoveScript.Position + (Vector2)(owner.transform.forward * abilityData.GetCustomStatValue("TravelDistance"));
            else
                attackPosition = _ownerMoveScript.Position + (_ownerInput.AttackDirection * abilityData.GetCustomStatValue("TravelDistance"));

            _ownerMoveScript.canCancelMovement = true;
            _ownerMoveScript.Speed = abilityData.GetCustomStatValue("MoveSpeed");

            _ownerMoveScript.MoveToPanel(attackPosition, false, GridScripts.GridAlignment.ANY);
        }
    }
}