using Lodis.AI;
using Lodis.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Lodis.AI
{
    public class ActionNode : TreeNode
    {
        private static float _directionWeight = 0.5f;
        private static float _attackDirectionWeight = 1;
        private static float _moveDirectionWeight = 1;
        private static float _opponentVelocityWeight = 0.8f;
        private static float _energyWeight = 1;
        private static float _opponentEnergyWeight = 1;
        private static float _distanceWeight = 0.7f;
        private static float _avgHitBoxOffsetWeight = 1.5f;
        private static float _avgVelocityWeight = 1.5f;
        private static float _healthWeight = 2;
        private static float _barrierHealthWeight = 1;
        private static float _opponentHealthWeight = 1;
        private static float _opponentBarrierHealthWeight = 1;
        private static float _matchTimeRemainingWeight = 1;
        private static float _opponentStateWeight = 1;


        public Vector3 OwnerToTarget;
        public bool CanMove;
        public bool CanCancelMovement;
        public float MoveSpeed;
        public Vector2 MoveDirection;
        public bool IsGrounded;
        public int AlignmentX = 1;

        public bool AbilityInUse;
        public float Energy;
        public bool CanBurst;
        public bool CanUseAbility;
        public Vector2 AttackDirection;

        public int CurrentAbilityID = -1;
        public int Ability1ID = -1;
        public int Ability2ID = -1;
        public int NextAbilityID = -1;

        public float Health;
        public float BarrierHealth;
        public float OpponentHealth;
        public float OpponentBarrierHealth;

        public string CurrentState;

        public Vector2 PanelPosition;
        public Vector3 AverageHitBoxOffset;
        public Vector3 AverageVelocity;

        public Vector2 OpponentMoveDirection;
        public Vector3 OpponentVelocity;

        public bool OpponentAbilityInUse;
        public float OpponentEnergy;
        public bool OpponentCanBurst;
        public bool OpponentCanUseAbility;

        public int OpponentAbility1ID = -1;
        public int OpponentAbility2ID = -1;
        public int OpponentNextAbilityID = -1;

        public string OpponentState;
        public float TimeStamp;
        public float TimeDelay;
        public float MatchTimeRemaining;

        public static float DirectionWeight { get => _directionWeight; set => _directionWeight = value; }
        public static float OpponentVelocityWeight { get => _opponentVelocityWeight; set => _opponentVelocityWeight = value; }
        public static float DistanceWeight { get => _distanceWeight; set => _distanceWeight = value; }
        public static float AvgHitBoxOffsetWeight { get => _avgHitBoxOffsetWeight; set => _avgHitBoxOffsetWeight = value; }
        public static float AvgVelocityWeight { get => _avgVelocityWeight; set => _avgVelocityWeight = value; }
        public static float MatchTimeRemainingWeight { get => _matchTimeRemainingWeight; set => _matchTimeRemainingWeight = value; }
        public static float OpponentStateWeight { get => _opponentStateWeight; set => _opponentStateWeight = value; }
        public static float OpponentHealthWeight { get => _opponentHealthWeight; set => _opponentHealthWeight = value; }

        public ActionNode(TreeNode left, TreeNode right) : base(left, right) { }

        private float GetPercentage(float a, float b)
        {
            if (Mathf.Abs(a - b) < 0.01f)
                return 1;

            float result = Mathf.Abs(a - b) / ((a + b) / 2);

            if (result > 1)
                result -= Mathf.Abs(result - 1);

            result = Mathf.Clamp(result, 0, 1);

            return result;
        }

        public ActionNode GetShallowCopy()
        {
            return (ActionNode)MemberwiseClone();
        }

        public override float Compare(TreeNode node)
        {
            ActionNode situationNode = (node as ActionNode).GetShallowCopy();

            if (AlignmentX != situationNode.AlignmentX)
            {
                situationNode.OwnerToTarget.x *= -1;
                situationNode.AttackDirection.x *= -1;
                situationNode.MoveDirection.x *= -1;
                situationNode.OpponentVelocity.x *= -1;
                situationNode.AverageVelocity.x *= -1;
                situationNode.AverageHitBoxOffset.x *= -1;
            }

            if (situationNode == null)
                return 400;

            if (IsGrounded != situationNode.IsGrounded)
                return 400;

            if (CurrentState != situationNode.CurrentState)
                return 400;

            if (PanelPosition.y != situationNode.PanelPosition.y)
                return 400;

            //Vector2 position = PanelPosition;

            //if (situationNode.AlignmentX == -1 && CurrentAbilityID == -1)
            //{
            //    int fp = BlackBoardBehaviour.Instance.Grid.P1MaxColumns + 1;
            //    int cp = (int)position.x;

            //    int tp = fp + (fp - cp) - 1;

            //   position.x = tp;

            //}

            if (!BlackBoardBehaviour.Instance.Grid.CheckIfPositionInRange(situationNode.PanelPosition + MoveDirection) && CurrentAbilityID == -1)
                return 400;


            //if (CurrentAbilityID != actionNode.CurrentAbilityID)
            //    return 4;

            //Check direction to enemy accuracy
            float directionAccuracy = 1;
            if (situationNode.OwnerToTarget.magnitude != 0 || OwnerToTarget.magnitude != 0)
            {
                Vector3 ownerToTarget = OwnerToTarget;

                directionAccuracy = Vector3.Dot(situationNode.OwnerToTarget.normalized, ownerToTarget.normalized);

                if (directionAccuracy < 0)
                    directionAccuracy = 0;
            }

            directionAccuracy = 1 - directionAccuracy;

            //Check direction to enemy accuracy
            float attackDirectionAccuracy = 1;
            if (situationNode.AttackDirection.magnitude != 0 || AttackDirection.magnitude != 0)
            {
                //AttackDirection.x = Mathf.Abs(AttackDirection.x);
                attackDirectionAccuracy = 1 - Vector3.Dot(situationNode.AttackDirection.normalized, AttackDirection.normalized);

                if (attackDirectionAccuracy < 0)
                    attackDirectionAccuracy = 0;
            }

            //Check owner move direction
            float moveDirectionAccuracy = 1;
            //if (actionNode.MoveDirection.magnitude != 0 || MoveDirection.magnitude != 0)
            //{

            //    if (BlackBoardBehaviour.Instance.Grid.CheckIfPositionInRange(MoveDirection + actionNode.PanelPosition))
            //        return 4;

            //    MoveDirection.x = Mathf.Abs(MoveDirection.x);
            //    moveDirectionAccuracy = Vector3.Dot(actionNode.MoveDirection.normalized, MoveDirection.normalized);

            //    if (moveDirectionAccuracy < 0)
            //        moveDirectionAccuracy = 0;
            //}

            //Check opponent velocity
            float velocityAccuracy = 1;
            if (situationNode.OpponentVelocity.magnitude != 0 || OpponentVelocity.magnitude != 0)
            {
                Vector3 opponentVelocity = OpponentVelocity;

                velocityAccuracy = Vector3.Dot(situationNode.OpponentVelocity.normalized, opponentVelocity.normalized);
                if (velocityAccuracy < 0)
                    velocityAccuracy = 0;
            }

            velocityAccuracy = 1 - velocityAccuracy;

            //Check energy
            float energy = situationNode.Energy;

            float energyAccuracy = GetPercentage(energy, Energy);
            float opponentEnergyAccuracy = GetPercentage(OpponentEnergy, situationNode.OpponentEnergy);
            //Check distance to opponent
            float distanceAccuracy = Mathf.Abs(OwnerToTarget.magnitude - situationNode.OwnerToTarget.magnitude);

            //Check hit box distance
            float hitBoxPositionAccuracy = 1;
            if (situationNode.AverageHitBoxOffset.magnitude != 0 && AverageHitBoxOffset.magnitude != 0)
            {
                hitBoxPositionAccuracy = 1 - Vector3.Dot(situationNode.AverageHitBoxOffset.normalized, AverageHitBoxOffset.normalized);
                if (hitBoxPositionAccuracy < 0)
                    hitBoxPositionAccuracy = 0;
            }

            //Check health

            float healthAccuracy = GetPercentage(Health, situationNode.Health);
            float barrierHealthAccuracy = GetPercentage(BarrierHealth, situationNode.BarrierHealth);
            float opponentHealthAccuracy = GetPercentage(OpponentHealth, situationNode.OpponentHealth);
            float opponentBarrierHealthAccuracy = GetPercentage(OpponentBarrierHealth, situationNode.OpponentBarrierHealth + 1);
            float matchTimeAccuracy = GetPercentage(MatchTimeRemaining, situationNode.MatchTimeRemaining);
            float opponentState = OpponentState == situationNode.OpponentState ? 0 : 1;

            if (float.IsNaN(hitBoxPositionAccuracy))
                hitBoxPositionAccuracy = 0;

            float attackVelocityAccuracy = 1;

            if (situationNode.AverageVelocity.magnitude != 0 && AverageVelocity.magnitude != 0)
            {

                attackVelocityAccuracy = Vector3.Dot(situationNode.AverageVelocity.normalized, AverageVelocity.normalized);
            }

            if (float.IsNaN(attackVelocityAccuracy))
                attackVelocityAccuracy = 0;

            attackVelocityAccuracy = 1 - attackVelocityAccuracy;

            if (hitBoxPositionAccuracy > 1)
                hitBoxPositionAccuracy -= hitBoxPositionAccuracy - 1;

            if (velocityAccuracy > 1)
                velocityAccuracy -= velocityAccuracy - 1;

            if (distanceAccuracy > 1)
                distanceAccuracy -= distanceAccuracy - 1;

            //Calculate average value from comparision
            float totalAccuracy = 
                (directionAccuracy *DirectionWeight + distanceAccuracy * DistanceWeight +
                velocityAccuracy * OpponentVelocityWeight + hitBoxPositionAccuracy * AvgHitBoxOffsetWeight
                + attackVelocityAccuracy * AvgVelocityWeight /*+ moveDirectionAccuracy * _moveDirectionWeight*/
                //+ energyAccuracy * _energyWeight + opponentEnergyAccuracy * _opponentEnergyWeight
                /*+ healthAccuracy * _healthWeight + barrierHealthAccuracy * _barrierHealthWeight*/ + opponentHealthAccuracy * OpponentHealthWeight
                /*+ opponentBarrierHealthAccuracy * _opponentBarrierHealthWeight*/ /*+ attackDirectionAccuracy * _attackDirectionWeight*/
                + matchTimeAccuracy * MatchTimeRemainingWeight + opponentState * OpponentStateWeight);

            return totalAccuracy;
        }
    }
}
