using BBUnity.Conditions;
using FixedPoints;
using Lodis.AI;
using Lodis.Movement;
using Pada1.BBCore;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

[Condition("CustomConditions/OutOfRange")]
public class OutOfRangeCondition : GOCondition
{
    [InParam("Owner")]
    private AIControllerBehaviour _dummy;
    private GridMovementBehaviour _opponentMovement;

    /// <summary>
    /// Considered out of range if the enemy is too far from, behind, or not in directly in front of the dummy
    /// </summary>
    /// <returns></returns>
    public override bool Check()
    {
        _opponentMovement = _dummy.Opponent.GetComponent<GridMovementBehaviour>();
        FVector2 dummyPos = _dummy.AIMovement.MovementBehaviour.CurrentPanel.Position;
        FVector2 enemyPos = _opponentMovement.CurrentPanel.Position;
        FVector3 directionToOpponent = (enemyPos - dummyPos);
        Fixed32 dot = FVector3.Dot(_dummy.FixedTransform.Forward, directionToOpponent);

        return _dummy.AIMovement.MovementBehaviour.Position.X != _dummy.MaxRange || dot < 0 || dummyPos.Y != _opponentMovement.Position.Y + _opponentMovement.MoveDirection.Y;
    }
}
