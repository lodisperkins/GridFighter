using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public class ReloadStep : QuestStep
    {
        private MovesetBehaviour _ownerMoveset;

        public ReloadStep(QuestStepData data, GameObject owner) : base(data, owner)
        {
            _ownerMoveset = owner.GetComponent<MovesetBehaviour>();

            _ownerMoveset.AddOnManualShuffleAction(CheckComplete);
        }

        private void CheckComplete()
        {
            if (Status == QuestStatus.ACTIVE)
                Complete();
        }
    }
}
