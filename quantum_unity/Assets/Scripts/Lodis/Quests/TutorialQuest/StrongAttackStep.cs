using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public class StrongAttackStep : QuestStep
    {
        public StrongAttackStep(QuestStepData data, GameObject owner) : base(data, owner)
        {
            MatchManagerBehaviour.Instance.AddOnP2RingoutAction(CheckComplete);
        }

        private void CheckComplete()
        {
            if (Status == QuestStatus.ACTIVE)
                Complete();
        }
    }
}
