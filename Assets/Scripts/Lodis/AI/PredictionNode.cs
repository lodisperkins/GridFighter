using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    public class PredictionNode : TreeNode
    {
        private Vector3 _opponentVelocity;
        private Vector3 _ownerToTarget;
        private float _opponentHealth;
        private List<HitColliderBehaviour> _attacksInRange;
        private TreeNode _futureSituation;

        public List<HitColliderBehaviour> AttacksInRange { get => _attacksInRange; private set => _attacksInRange = value; }
        public TreeNode FutureSituation { get => _futureSituation; private set => _futureSituation = value; }

        public PredictionNode(TreeNode left, TreeNode right, Vector3 opponentVelocity,
            Vector3 ownerToOpponent, float opponentHealth, List<HitColliderBehaviour> attacksInRange, TreeNode futureSituation) : base(left, right)
        {
            _opponentVelocity = opponentVelocity;
            _ownerToTarget = ownerToOpponent;
            _opponentHealth = opponentHealth;
            AttacksInRange = attacksInRange;
            FutureSituation = futureSituation;
        }

        private Vector3 GetAverageVelocity()
        {
            Vector3 averageVelocity = Vector3.zero;

            if (AttacksInRange == null) return Vector3.zero;

            if (AttacksInRange.Count == 0)
                return Vector3.zero;

            for (int i = 0; i < AttacksInRange.Count; i++)
                if (AttacksInRange[i].RB)
                    averageVelocity += AttacksInRange[i].RB.velocity;

            return averageVelocity /= AttacksInRange.Count;
        }

        private Vector3 GetAveragePosition()
        {
            Vector3 averagePosition = Vector3.zero;

            if (AttacksInRange == null) return Vector3.zero;

            if (AttacksInRange.Count == 0)
                return Vector3.zero;

            AttacksInRange.RemoveAll(physics =>
            {
                if ((object)physics != null)
                    return physics == null;

                return true;
            });

            for (int i = 0; i < AttacksInRange.Count; i++)
                averagePosition += AttacksInRange[i].gameObject.transform.position;

            return averagePosition /= AttacksInRange.Count;
        }

        public override float Compare(TreeNode node)
        {
            PredictionNode predictNode = node as PredictionNode;

            if (predictNode == null)
                return 0;

            float directionAccuracy = Vector3.Dot(predictNode._ownerToTarget.normalized, _ownerToTarget.normalized);
            float velocityAccuracy = Vector3.Dot(predictNode._opponentVelocity.normalized, _opponentVelocity.normalized);
            float distanceAccuracy = _ownerToTarget.magnitude / predictNode._ownerToTarget.magnitude;

            Vector3 averagePosition = GetAveragePosition();
            Vector3 averageVelocity = GetAverageVelocity();

            float positionAccuracy = Vector3.Dot(predictNode.GetAveragePosition().normalized, averagePosition.normalized);
            if (float.IsNaN(positionAccuracy))
                positionAccuracy = 0;

            float attackVelocityAccuracy = Vector3.Dot(predictNode.GetAveragePosition().normalized, averageVelocity.normalized);
            if (float.IsNaN(attackVelocityAccuracy))
                attackVelocityAccuracy = 0;

            if (positionAccuracy > 1)
                positionAccuracy -= positionAccuracy - 1;

            if (velocityAccuracy > 1)
                velocityAccuracy -= velocityAccuracy - 1;

            if (distanceAccuracy > 1)
                distanceAccuracy -= distanceAccuracy - 1;

            float totalAccuracy = (directionAccuracy + distanceAccuracy + velocityAccuracy + positionAccuracy + velocityAccuracy) / 5;

            return totalAccuracy;
        }
    }
}