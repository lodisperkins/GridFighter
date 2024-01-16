using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public class NormalAttackStep : QuestStep
    {
        private MovesetBehaviour _ownerMoveset;

        public NormalAttackStep(QuestStepData data, GameObject owner) : base(data, owner) 
        {
            _ownerMoveset = owner.GetComponent<MovesetBehaviour>();

            _ownerMoveset.AddOnHitTempAction(CheckNormalHit);
        }

        private void CheckNormalHit(Ability ability, params object[] collisionArgs)
        {
            if ((int)ability.abilityData.AbilityType <= 3 && Status == QuestStatus.ACTIVE)
                Complete();
        }
    }
}