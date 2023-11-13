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
        private static float _directionWeight = 1;
        private static float _attackDirectionWeight = 1;
        private static float _moveDirectionWeight = 1;
        private static float _opponentVelocityWeight = 1;
        private static float _energyWeight = 1;
        private static float _opponentEnergyWeight = 1;
        private static float _distanceWeight = 1;
        private static float _avgPositionWeight = 1;
        private static float _avgVelocityWeight = 1;
        private static float _healthWeight = 1;
        private static float _barrierHealthWeight = 1;
        private static float _opponentHealthWeight = 1;
        private static float _opponentBarrierHealthWeight = 1;

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
        public float TimeStamp;


        public ActionNode(TreeNode left, TreeNode right) : base(left, right) { }

        private float GetPercentage(float a, float b)
        {
            if (a == b)
                return 1;

            float result = Mathf.Abs(a - b) / ((a + b) / 2);

            if (result > 1)
                result -= Mathf.Abs(result - 1);

            result = Mathf.Clamp(result, 0, 1);

            return result;
        }

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
                OwnerToTarget.x *= actionNode.AlignmentX;

                directionAccuracy = Vector3.Dot(actionNode.OwnerToTarget.normalized, OwnerToTarget.normalized);

                if (directionAccuracy < 0)
                    directionAccuracy = 0;
            }

            //Check direction to enemy accuracy
            float attackDirectionAccuracy = 1;
            if (actionNode.AttackDirection.magnitude != 0 || AttackDirection.magnitude != 0)
            {
                AttackDirection.x *= actionNode.AlignmentX; 

                attackDirectionAccuracy = Vector3.Dot(actionNode.AttackDirection.normalized, AttackDirection.normalized);

                if (attackDirectionAccuracy < 0)
                    attackDirectionAccuracy = 0;
            }

            //Check owner move direction
            float moveDirectionAccuracy = 1;
            if (actionNode.MoveDirection.magnitude != 0 || MoveDirection.magnitude != 0)
            {
                MoveDirection.x *= actionNode.AlignmentX;

                moveDirectionAccuracy = Vector3.Dot(actionNode.MoveDirection.normalized, MoveDirection.normalized);

                if (moveDirectionAccuracy < 0)
                    moveDirectionAccuracy = 0;
            }

            //Check opponent velocity
            float velocityAccuracy = 1;
            if (actionNode.OpponentVelocity.magnitude != 0 || OpponentVelocity.magnitude != 0)
            {
                OpponentVelocity.x *= actionNode.AlignmentX;

                velocityAccuracy = Vector3.Dot(actionNode.OpponentVelocity.normalized, OpponentVelocity.normalized);
                if (velocityAccuracy < 0)
                    velocityAccuracy = 0;
            }

            //Check energy
            float energy = actionNode.Energy;
            if (energy == 0)
                energy = 1;

            float energyAccuracy = GetPercentage(energy, Energy);
            float opponentEnergyAccuracy = GetPercentage(OpponentEnergy, actionNode.OpponentEnergy);
            //Check distance to opponent
            float distanceAccuracy = GetPercentage(OwnerToTarget.magnitude, actionNode.OwnerToTarget.magnitude);

            //Check hit box distance
            float positionAccuracy = 1;
            if (actionNode.AveragePosition.magnitude != 0 || AveragePosition.magnitude != 0)
            {
                positionAccuracy = Vector3.Dot(actionNode.AveragePosition.normalized, AveragePosition.normalized);
                if (positionAccuracy < 0)
                    positionAccuracy = 0;
            }

            //Check health

            float healthAccuracy = GetPercentage(Health, actionNode.Health);
            float barrierHealthAccuracy = GetPercentage(BarrierHealth, actionNode.BarrierHealth);
            float opponentHealthAccuracy = GetPercentage(OpponentHealth, actionNode.OpponentHealth);
            float opponentBarrierHealthAccuracy = GetPercentage(OpponentBarrierHealth, actionNode.OpponentBarrierHealth + 1);

            if (float.IsNaN(positionAccuracy))
                positionAccuracy = 0;

            float attackVelocityAccuracy = 1;

            if (actionNode.AverageVelocity.magnitude != 0 || AverageVelocity.magnitude != 0)
            {
                AverageVelocity.x *= actionNode.AlignmentX;

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
            float totalAccuracy = 
                (directionAccuracy *_directionWeight + distanceAccuracy * _distanceWeight +
                velocityAccuracy * _opponentVelocityWeight + positionAccuracy * _avgPositionWeight
                + attackVelocityAccuracy * _avgVelocityWeight + moveDirectionAccuracy * _moveDirectionWeight
                + energyAccuracy * _energyWeight + opponentEnergyAccuracy * _opponentEnergyWeight
                + healthAccuracy * _healthWeight + barrierHealthAccuracy * _barrierHealthWeight + opponentHealthAccuracy * _opponentHealthWeight
                + opponentBarrierHealthAccuracy * _opponentBarrierHealthWeight + attackDirectionAccuracy * _attackDirectionWeight) / 13;

            return totalAccuracy;
        }
    }
}
