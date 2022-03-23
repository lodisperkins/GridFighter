using BBUnity.Conditions;
using Lodis.AI;
using Lodis.Movement;
using Pada1.BBCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Condition("CustomConditions/OutOfRange")]
public class InRangeCondition : GOCondition
{
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;
    private GridMovementBehaviour _opponentMovement;

    /// <summary>
    /// Considered unsafe if hit boxes are in range, in the tumbling state, or an attack has been started on the same row
    /// </summary>
    /// <returns></returns>
    public override bool Check()
    {
        _opponentMovement = _dummy.Opponent.GetComponent<GridMovementBehaviour>();
        Vector2 dummyPos = _dummy.AIMovement.MovementBehaviour.CurrentPanel.Position;
        Vector2 enemyPos = _opponentMovement.CurrentPanel.Position;

        return Mathf.Abs(dummyPos.x - enemyPos.x) > 7 && Mathf.Abs(dummyPos.y - enemyPos.y) > 2;
    }
}
