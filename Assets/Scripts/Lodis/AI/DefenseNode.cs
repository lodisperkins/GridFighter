using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public enum DefenseDecisionType
    {
        EVADE,
        COUNTER,
        PARRY
    }

    [System.Serializable]
    public class DefenseNode : TreeNode
    {
        public List<Vector3> VelocityOfAttacks;
        public List<HitColliderBehaviour> AttacksInRange;
        public DefenseDecisionType DefenseDecision;

        public DefenseNode(List<Vector3> velocityOfAttacks, List<HitColliderBehaviour> attacksInRange, TreeNode left, TreeNode right) : base(left, right)
        {
            VelocityOfAttacks = velocityOfAttacks;
        }

        public Vector3 GetAverageVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;

            for (int i = 0; i < VelocityOfAttacks.Count; i++)
                averageVelocity += VelocityOfAttacks[i];

            return averageVelocity /= VelocityOfAttacks.Count;
        }

        public Vector3 GetAveragePosition()
        {
            Vector3 averagePosition = Vector3.zero;

            for (int i = 0; i < AttacksInRange.Count; i++)
                averagePosition += AttacksInRange[i].gameObject.transform.position;

            return averagePosition /= AttacksInRange.Count;
        }

        public override float Compare(TreeNode node)
        {
            DefenseNode defenseNode = node as DefenseNode;

            if (defenseNode == null) return 0;

            Vector3 averageVelocity = GetAverageVelocity();

            float velocityAccuracy = Vector3.Dot(defenseNode.GetAverageVelocity().normalized, GetAverageVelocity().normalized);
            float positionAccuracy = Vector3.Dot(defenseNode.GetAveragePosition().normalized, GetAveragePosition().normalized);

            if (positionAccuracy > 1)
                positionAccuracy -= positionAccuracy - 1;

            float totalAccuracy = (velocityAccuracy + positionAccuracy) / 2;

            return totalAccuracy;
        }
    }
}
