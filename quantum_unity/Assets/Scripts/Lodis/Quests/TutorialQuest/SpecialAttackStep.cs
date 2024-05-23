using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public class SpecialAttackStep : QuestStep
    {
        private MovesetBehaviour _ownerMoveset;

        public SpecialAttackStep(QuestStepData data, GameObject owner) : base(data, owner)
        {
            _ownerMoveset = owner.GetComponent<MovesetBehaviour>();

            _ownerMoveset.AddOnAutoShuffleAction(CheckComplete);
        }

        private void CheckComplete()
        {
            if (Status == QuestStatus.ACTIVE)
                Complete();
        }
    }
}
