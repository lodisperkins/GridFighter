//using FixedPoints;
//using Lodis.Gameplay;
//using Newtonsoft.Json;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Lodis.AI
//{
//    public class PredictionNode : TreeNode
//    {
//        private FVector3 _opponentVelocity;
//        private FVector3 _ownerToTarget;
//        private float _opponentHealth;
//        [JsonIgnore]
//        private List<HitColliderBehaviour> _attacksInRange;
//        private FVector3 _averagePosition;
//        private FVector3 _averageVelocity;
//        private TreeNode _futureSituation;
//        private float _targetY;

//        [JsonIgnore]
//        public List<HitColliderBehaviour> AttacksInRange { get => _attacksInRange; private set => _attacksInRange = value; }
//        public TreeNode FutureSituation { get => _futureSituation; private set => _futureSituation = value; }
//        public float TargetY { get => _targetY; set => _targetY = value; }

//        public PredictionNode(TreeNode left, TreeNode right, FVector3 opponentVelocity,
//            FVector3 ownerToOpponent, float opponentHealth, List<HitColliderBehaviour> attacksInRange, TreeNode futureSituation) : base(left, right)
//        {
//            _opponentVelocity = opponentVelocity;
//            _ownerToTarget = ownerToOpponent;
//            _opponentHealth = opponentHealth;
//            AttacksInRange = attacksInRange;
//            FutureSituation = futureSituation;
//        }

//        private FVector3 GetAverageVelocity()
//        {
//            _averageVelocity = FVector3.Zero;

//            if (AttacksInRange == null) return FVector3.Zero;

//            if (AttacksInRange.Count == 0)
//                return FVector3.Zero;

//            for (int i = 0; i < AttacksInRange.Count; i++)
//                if (AttacksInRange[i].GridPhysics)
//                    _averageVelocity += AttacksInRange[i].GridPhysics.Velocity;

//            return _averageVelocity /= AttacksInRange.Count;
//        }

//        private FVector3 GetAveragePosition()
//        {
//            _averagePosition = FVector3.Zero;

//            if (AttacksInRange == null) return FVector3.Zero;

//            if (AttacksInRange.Count == 0)
//                return FVector3.Zero;

//            AttacksInRange.RemoveAll(physics =>
//            {
//                if ((object)physics != null)
//                    return physics == null;

//                return true;
//            });

//            for (int i = 0; i < AttacksInRange.Count; i++)
//                _averagePosition += AttacksInRange[i].Owner.Transform.Position;

//            return _averagePosition /= AttacksInRange.Count;
//        }

//        public override float Compare(TreeNode node)
//        {
//            PredictionNode predictNode = node as PredictionNode;

//            if (predictNode == null)
//                return 0;

//            float directionAccuracy = 1;
//            if (predictNode._ownerToTarget.Magnitude!= 0 || _ownerToTarget.Magnitude != 0)
//            {
//               directionAccuracy = FVector3.Dot(predictNode._ownerToTarget.GetNormalized(), _ownerToTarget.GetNormalized());
//            }

//            float velocityAccuracy = 1;
//            if (predictNode._opponentVelocity.Magnitude != 0 || _opponentVelocity.Magnitude != 0)
//            {
//               velocityAccuracy = FVector3.Dot(predictNode._opponentVelocity.GetNormalized(), _opponentVelocity.GetNormalized());
//            }

//            float distanceAccuracy = _ownerToTarget.Magnitude / predictNode._ownerToTarget.Magnitude;

//            FVector3 averagePosition = GetAveragePosition();
//            FVector3 averageVelocity = GetAverageVelocity();

//            float positionAccuracy = 1;
//            if (predictNode.GetAveragePosition().Magnitude != 0 || averagePosition.Magnitude != 0)
//            {
//               positionAccuracy = FVector3.Dot(predictNode.GetAveragePosition().GetNormalized(), averagePosition.GetNormalized());
//            }

//            if (float.IsNaN(positionAccuracy))
//                positionAccuracy = 0;

//            float attackVelocityAccuracy = 1;
            
//            if (predictNode.GetAverageVelocity().Magnitude != 0 || averageVelocity.Magnitude != 0)
//            {
//               attackVelocityAccuracy =  FVector3.Dot(predictNode.GetAverageVelocity().GetNormalized(), averageVelocity.GetNormalized());
//            }

//            if (float.IsNaN(attackVelocityAccuracy))
//                attackVelocityAccuracy = 0;

//            if (positionAccuracy > 1)
//                positionAccuracy -= positionAccuracy - 1;

//            if (velocityAccuracy > 1)
//                velocityAccuracy -= velocityAccuracy - 1;

//            if (distanceAccuracy > 1)
//                distanceAccuracy -= distanceAccuracy - 1;

//            float totalAccuracy = (directionAccuracy + distanceAccuracy + velocityAccuracy + positionAccuracy + attackVelocityAccuracy) / 5;

//            return totalAccuracy;
//        }
//    }
//}