using BBUnity.Conditions;
using FixedPoints;
using Lodis.AI;
using Lodis.Movement;
using Pada1.BBCore;
using System.Collections;
using System.Collections.Generic;
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
        float dot = Vector3.Dot(_dummy.Character.transform.forward, (Vector3)directionToOpponent);

        return Mathf.Abs(dummyPos.X - enemyPos.Y) > _dummy.MaxRange || dot < 0 || dummyPos.Y != _opponentMovement.Position.Y + _opponentMovement.MoveDirection.Y;
    }
}
