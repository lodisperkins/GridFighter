using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;

namespace Lodis.AI
{
    [System.Serializable]
    public class AttackNode : TreeNode
    {
        private MovesetBehaviour _moveset;
        public Vector3 TargetDisplacement;
        public float TargetHealth;
        public int BarrierEffectiveness;
        public float AttackStartTime;
        public string AbilityName;
        public float AttackStrength;
        public float KnockBackDealt;
        public Vector2 AttackDirection;

        public AttackNode(Vector3 targetDisplacement,
                          float targetHealth,
                          int barrierEffectiveness,
                          float attackStartTime,
                          string abilityName,
                          float attackStrength,
                          TreeNode left,
                          TreeNode right) : base(left, right)
        {
            TargetDisplacement = targetDisplacement;
            TargetHealth = targetHealth;
            BarrierEffectiveness = barrierEffectiveness;
            AttackStartTime = attackStartTime;
            AbilityName = abilityName;
            AttackStrength = attackStrength;
        }

        public override float GetTotalWeight(TreeNode root, params object[] args)
        {
            float weight = base.GetTotalWeight(root, args);

            bool behindBarrier = (bool)args[1];
            float opponentHealth = (float)args[2];
            AttackDummyBehaviour owner = (AttackDummyBehaviour)args[3];

            if (behindBarrier) weight += BarrierEffectiveness;
            weight += KnockBackDealt;

            if (!owner.Moveset.NormalDeckContains(AbilityName) && !owner.Moveset.SpecialDeckContains(AbilityName))
                weight = 0;

            return weight;
        }

        public override float Compare(TreeNode node)
        {
            AttackNode attackNode = node as AttackNode;

            if (attackNode == null) return 0;

            float directionAccuracy = Vector3.Dot(attackNode.TargetDisplacement.normalized, TargetDisplacement.normalized);
            float distanceAccuracy = TargetDisplacement.magnitude / attackNode.TargetDisplacement.magnitude;
            if (distanceAccuracy > 1)
                distanceAccuracy -= distanceAccuracy - 1;

            float totalAccuracy = (directionAccuracy + distanceAccuracy) / 2;
            //continue here
            return totalAccuracy;
        }
    }
}