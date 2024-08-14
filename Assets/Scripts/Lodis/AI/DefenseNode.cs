//using FixedPoints;
//using Lodis.Gameplay;
//using Lodis.Movement;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Lodis.AI
//{
//    public enum DefenseDecisionType
//    {
//        EVADE,
//        COUNTER,
//        PARRY,
//        PHASESHIFT,
//        BURST,
//        BREAKFALLJUMP,
//        BREAKFALLNEUTRAL,
//        NONE
//    }

//    [System.Serializable]
//    public class DefenseNode : TreeNode
//    {
//        private List<HitColliderBehaviour> _attacksInRange;
//        public DefenseDecisionType DefenseDecision;
//        public string CounterAbilityName;
//        public FVector3 AveragePosition;
//        public FVector3 AverageVelocity;

//        public DefenseNode(List<HitColliderBehaviour> attacksInRange, TreeNode left, TreeNode right) : base(left, right)
//        {
//            _attacksInRange = attacksInRange;
//        }

//        private FVector3 GetAverageVelocity()
//        {
//            FVector3 averageVelocity = FVector3.Zero;

//            if (_attacksInRange == null) return FVector3.Zero;

//            if (_attacksInRange.Count == 0)
//                return FVector3.Zero;

//            for (int i = 0; i < _attacksInRange.Count; i++)
//                if (_attacksInRange[i].GridPhysics)
//                    averageVelocity += _attacksInRange[i].GridPhysics.Velocity;

//            return averageVelocity /= _attacksInRange.Count;
//        }

//        private FVector3 GetAveragePosition()
//        {
//            FVector3 averagePosition = FVector3.Zero;

//            if (_attacksInRange == null) return FVector3.Zero;

//            if (_attacksInRange.Count == 0)
//                return FVector3.Zero;

//            _attacksInRange.RemoveAll(physics =>
//            {
//                if ((object)physics != null)
//                    return physics == null;

//                return true;
//            });

//            for (int i = 0; i < _attacksInRange.Count; i++)
//                averagePosition += _attacksInRange[i].OwnerTransform.Position;

//            return averagePosition /= _attacksInRange.Count;
//        }

//        public int GetCountOfActiveHitBoxes()
//        {
//            if (_attacksInRange == null)
//                return -1;

//            return _attacksInRange.Count;
//        }

//        public override TreeNode CopyData(TreeNode other)
//        {
//            DefenseNode otherNode = (DefenseNode)other;
//            DefenseNode node = (DefenseNode)otherNode.MemberwiseClone();

//            node.Left = Left;
//            node.Right = Right;
//            node.Parent = Parent;

//            return node;
//        }

//        public override float Compare(TreeNode node)
//        {
//            AveragePosition = GetAveragePosition();
//            AverageVelocity = GetAverageVelocity();
//            DefenseNode defenseNode = node as DefenseNode;

//            if (defenseNode == null) return 0;

//            float positionAccuracy = FVector3.Dot(defenseNode.AveragePosition.GetNormalized(), AveragePosition.GetNormalized());
//            if (float.IsNaN(positionAccuracy))
//                positionAccuracy = 0;

//            float velocityAccuracy = FVector3.Dot(defenseNode.AverageVelocity.GetNormalized(), AverageVelocity.GetNormalized());
//            if (float.IsNaN(velocityAccuracy))
//                velocityAccuracy = 0;

//            if (positionAccuracy > 1)
//                positionAccuracy -= positionAccuracy - 1;

//            if (velocityAccuracy > 1)
//                velocityAccuracy -= velocityAccuracy - 1;

//            float totalAccuracy = (velocityAccuracy + positionAccuracy) / 2;

//            return totalAccuracy;
//        }
//    }
//}
