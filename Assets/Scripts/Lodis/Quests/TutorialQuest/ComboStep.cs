using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public class ComboStep : QuestStep
    {

        public ComboStep(QuestStepData data, GameObject owner) : base(data, owner)
        {
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (BlackBoardBehaviour.Instance.Player1ComboCounter.HitCount >= 10)
                Complete();
        }
    }
}
