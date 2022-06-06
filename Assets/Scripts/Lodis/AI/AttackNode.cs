﻿using System.Collections;
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
        public Vector3 TargetVelocity;
        public float TargetHealth;
        public int ShieldEffectiveness;
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
                          Vector3 targetVelocity,
                          TreeNode left,
                          TreeNode right) : base(left, right)
        {
            TargetDisplacement = targetDisplacement;
            TargetHealth = targetHealth;
            ShieldEffectiveness = barrierEffectiveness;
            AttackStartTime = attackStartTime;
            AbilityName = abilityName;
            AttackStrength = attackStrength;
            TargetVelocity = targetVelocity;
        }

        public override float GetTotalWeight(TreeNode root, params object[] args)
        {
            float weight = base.GetTotalWeight(root, args);

            bool behindBarrier = (bool)args[1];
            float opponentHealth = (float)args[2];
            AttackDummyBehaviour owner = (AttackDummyBehaviour)args[3];

            if (behindBarrier) weight += ShieldEffectiveness;

            if (!owner.Moveset.NormalDeckContains(AbilityName) && !owner.Moveset.SpecialDeckContains(AbilityName))
                return 0;

            if (owner.Moveset.Energy < owner.Moveset.GetAbilityByName(AbilityName).abilityData.EnergyCost)
                return 0;

            if (opponentHealth > 150)
                weight += KnockBackDealt;

            return weight;
        }

        public override TreeNode CopyData(TreeNode other)
        {
            AttackNode otherNode = (AttackNode)other;
            AttackNode node = (AttackNode)otherNode.MemberwiseClone();

            node.Left = Left;
            node.Right = Right;
            node.Parent = Parent;

            return node;
        }

        public override float Compare(TreeNode node)
        {
            AttackNode attackNode = node as AttackNode;

            if (attackNode == null) return 0;

            float directionAccuracy = Vector3.Dot(attackNode.TargetDisplacement.normalized, TargetDisplacement.normalized);
            float velocityAccuracy = Vector3.Dot(attackNode.TargetVelocity.normalized, TargetVelocity.normalized);
            float attackDirectionAccuracy = Vector3.Dot(attackNode.AttackDirection.normalized, AttackDirection.normalized);
            float distanceAccuracy = TargetDisplacement.magnitude / attackNode.TargetDisplacement.magnitude;
            float attackStrengthAccuracy = 0;

            if (attackNode.AttackStrength > 0) 
                attackStrengthAccuracy = AttackStrength / attackNode.AttackStrength;

            if (distanceAccuracy > 1)
                distanceAccuracy -= distanceAccuracy - 1;

            float totalAccuracy = (directionAccuracy + distanceAccuracy + attackDirectionAccuracy + attackStrengthAccuracy + velocityAccuracy) / 5;

            return totalAccuracy;
        }
    }
}