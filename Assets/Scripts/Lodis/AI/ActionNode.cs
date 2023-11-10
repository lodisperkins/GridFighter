using Lodis.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Lodis.AI
{
    class ActionNode : TreeNode
    {
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

        public int CurrentAbilityID = -1;
        public int Ability1ID = -1;
        public int Ability2ID = -1;
        public int NextAbilityID = -1;

        public float Health;
        public float BarrierHealth;
        public float OpponentHealth;
        public float OpponentBarrierHealth;

        public string CurrentState;

        public Vector3 AveragePosition;
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

        public bool IsAttacking;

        public ActionNode(TreeNode left, TreeNode right) : base(left, right) { }

        public override float Compare(TreeNode node)
        {
            ActionNode actionNode = node as ActionNode;

            if (actionNode == null)
                return 0;

            if (IsGrounded != actionNode.IsGrounded)
                return 0;

            if ((CurrentState == "Flinching" && actionNode.CurrentState != "Flinching") || (CurrentState == "Tumbling" && actionNode.CurrentState != "Tumbling"))
                return 0;

            if (CurrentAbilityID != -1 && CurrentAbilityID != actionNode.CurrentAbilityID)
                return 0;

            //Check direction to enemy accuracy
            float directionAccuracy = 1;
            if (actionNode.OwnerToTarget.magnitude != 0 || OwnerToTarget.magnitude != 0)
            {
                OwnerToTarget.x *= AlignmentX;
                actionNode.OwnerToTarget.x *= AlignmentX;

                directionAccuracy = Vector3.Dot(actionNode.OwnerToTarget.normalized, OwnerToTarget.normalized);
            }

            //Check owner move direction
            float moveDirectionAccuracy = 1;
            if (actionNode.MoveDirection.magnitude != 0 || MoveDirection.magnitude != 0)
            {
                MoveDirection.x *= AlignmentX;
                actionNode.MoveDirection.x *= AlignmentX;

                moveDirectionAccuracy = Vector3.Dot(actionNode.MoveDirection.normalized, MoveDirection.normalized);
            }

            //Check opponent velocity
            float velocityAccuracy = 1;
            if (actionNode.OpponentVelocity.magnitude != 0 || OpponentVelocity.magnitude != 0)
            {
                OpponentVelocity.x *= AlignmentX;
                actionNode.OpponentVelocity.x *= AlignmentX;

                velocityAccuracy = Vector3.Dot(actionNode.OpponentVelocity.normalized, OpponentVelocity.normalized);
            }

            //Check energy
            float oppEnergy = actionNode.Energy;
            if (oppEnergy == 0)
                oppEnergy = 1;

            float energyAccuracy = Energy / oppEnergy;
            float opponentEnergyAccuracy = OpponentEnergy / actionNode.OpponentEnergy;

            //Check distance to opponent
            float distanceAccuracy = OwnerToTarget.magnitude / actionNode.OwnerToTarget.magnitude;

            //Check hit box distance
            float positionAccuracy = 1;
            if (actionNode.AveragePosition.magnitude != 0 || AveragePosition.magnitude != 0)
            {
                positionAccuracy = Vector3.Dot(actionNode.AveragePosition.normalized, AveragePosition.normalized);
            }

            //Check health

            float healthAccuracy = Health / (actionNode.Health + 1);
            float barrierHealthAccuracy = BarrierHealth / (actionNode.BarrierHealth + 1);
            float opponentHealthAccuracy = OpponentHealth / (actionNode.OpponentHealth + 1);
            float opponentBarrierHealthAccuracy = OpponentBarrierHealth / (actionNode.OpponentBarrierHealth + 1);

            if (float.IsNaN(positionAccuracy))
                positionAccuracy = 0;

            float attackVelocityAccuracy = 1;

            if (actionNode.AverageVelocity.magnitude != 0 || AverageVelocity.magnitude != 0)
            {
                AverageVelocity.x *= AlignmentX;
                actionNode.AverageVelocity.x *= AlignmentX;

                attackVelocityAccuracy = Vector3.Dot(actionNode.AverageVelocity.normalized, AverageVelocity.normalized);
            }

            if (float.IsNaN(attackVelocityAccuracy))
                attackVelocityAccuracy = 0;

            if (positionAccuracy > 1)
                positionAccuracy -= positionAccuracy - 1;

            if (velocityAccuracy > 1)
                velocityAccuracy -= velocityAccuracy - 1;

            if (distanceAccuracy > 1)
                distanceAccuracy -= distanceAccuracy - 1;

            //Calculate average value from comparision
            float totalAccuracy = (directionAccuracy + distanceAccuracy + velocityAccuracy + positionAccuracy + attackVelocityAccuracy
                + moveDirectionAccuracy + energyAccuracy + opponentEnergyAccuracy + healthAccuracy + barrierHealthAccuracy + opponentHealthAccuracy
                + opponentBarrierHealthAccuracy) / 12;

            return totalAccuracy;
        }
    }
}
