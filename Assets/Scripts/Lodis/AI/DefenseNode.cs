using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public enum DefenseDecisionType
    {
        EVADE,
        COUNTER,
        PARRY,
        BURST
    }

    [System.Serializable]
    public class DefenseNode : TreeNode
    {
        private List<GridPhysicsBehaviour> _attacksInRange = new List<GridPhysicsBehaviour>();
        public DefenseDecisionType DefenseDecision;
        public string CounterAbilityName;
        public Vector3 AveragePosition;
        public Vector3 AverageVelocity;

        public DefenseNode(List<GridPhysicsBehaviour> attacksInRange, TreeNode left, TreeNode right) : base(left, right)
        {
            _attacksInRange = attacksInRange;
            AveragePosition = GetAveragePosition();
            AverageVelocity = GetAverageVelocity();
        }

        private Vector3 GetAverageVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            _attacksInRange.RemoveAll(physics =>
            {
                if ((object)physics != null)
                    return physics == null;

                return true;
            });

            for (int i = 0; i < _attacksInRange.Count; i++)
                averageVelocity += _attacksInRange[i].LastVelocity;

            return averageVelocity /= _attacksInRange.Count;
        }

        private Vector3 GetAveragePosition()
        {
            Vector3 averagePosition = Vector3.zero;

            if (_attacksInRange == null) return Vector3.zero;

            if (_attacksInRange.Count == 0)
                return Vector3.zero;

            _attacksInRange.RemoveAll(physics =>
            {
                if ((object)physics != null)
                    return physics == null;

                return true;
            });

            for (int i = 0; i < _attacksInRange.Count; i++)
                averagePosition += _attacksInRange[i].gameObject.transform.position;

            return averagePosition /= _attacksInRange.Count;
        }

        public override TreeNode CopyData(TreeNode other)
        {
            DefenseNode otherNode = (DefenseNode)other;
            DefenseNode node = (DefenseNode)otherNode.MemberwiseClone();

            node.Left = Left;
            node.Right = Right;
            node.Parent = Parent;

            return node;
        }

        public override float Compare(TreeNode node)
        {
            DefenseNode defenseNode = node as DefenseNode;

            if (defenseNode == null) return 0;

            float velocityAccuracy = Vector3.Dot(defenseNode.AverageVelocity.normalized, AverageVelocity.normalized);
            if (float.IsNaN(velocityAccuracy))
                velocityAccuracy = 0;

            float positionAccuracy = Vector3.Dot(defenseNode.AveragePosition.normalized, AveragePosition.normalized);
            if (float.IsNaN(positionAccuracy))
                positionAccuracy = 0;

            if (positionAccuracy > 1)
                positionAccuracy -= positionAccuracy - 1;

            float totalAccuracy = (velocityAccuracy + positionAccuracy) / 2;

            return totalAccuracy;
        }
    }
}
