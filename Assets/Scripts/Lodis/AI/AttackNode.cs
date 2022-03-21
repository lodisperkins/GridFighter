using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;

namespace Lodis.AI
{
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

            _moveset = (MovesetBehaviour)args[0];
            bool behindBarrier = (bool)args[1];
            float opponentHealth = (float)args[2];

            if (behindBarrier) weight += BarrierEffectiveness;
            if (opponentHealth > 150) weight += KnockBackDealt;

            return weight;
        }

        public override float Compare(TreeNode node)
        {
            AttackNode attackNode = (AttackNode)node;

            float directionAccuracy = Vector3.Dot(attackNode.TargetDisplacement.normalized, TargetDisplacement.normalized);

            //continue here
            return directionAccuracy;
        }
    }
}