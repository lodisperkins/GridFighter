using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Quest
{
    public class BurstStep : QuestStep
    {
        private MovesetBehaviour _ownerMoveset;

        public BurstStep(QuestStepData data, GameObject owner) : base(data, owner)
        {
            _ownerMoveset = owner.GetComponent<MovesetBehaviour>();

            _ownerMoveset.OnBurst += CheckComplete;
        }

        public override void OnStart()
        {
            base.OnStart();
            MatchManagerBehaviour.Instance.InfiniteBurst = true;
        }

        private void CheckComplete()
        {
            if (Status == QuestStatus.ACTIVE && BlackBoardBehaviour.Instance.Player1State == "Tumbling")
                Complete();

            MatchManagerBehaviour.Instance.InfiniteBurst = false;
        }
    }
}
