using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    

    public abstract class BasicAbility : Ability
    {
        //The type describes the strength and input value for the ability
        public BasicAbilityType abilityType;
        private Vector2 _moveDirection;

        public override void Init(GameObject owner)
        {
            base.Init(owner);

            switch (abilityType)
            {
                case BasicAbilityType.WEAKNEUTRAL or BasicAbilityType.STRONGNEUTRAL:
                    _moveDirection = Vector2.zero;
                    break;

                    case BasicAbilityType.Side or basi
            }

        }
    }
}

