using BBUnity.Conditions;
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
    private AttackDummyBehaviour _dummy;
    private GridMovementBehaviour _opponentMovement;

    /// <summary>
    /// Considered out of range if the enemy is too far from, behind, or not in directly in front of the dummy
    /// </summary>
    /// <returns></returns>
    public override bool Check()
    {
        _opponentMovement = _dummy.Opponent.GetComponent<GridMovementBehaviour>();
        Vector2 dummyPos = _dummy.AIMovement.MovementBehaviour.CurrentPanel.Position;
        Vector2 enemyPos = _opponentMovement.CurrentPanel.Position;
        Vector3 directionToOpponent = (enemyPos - dummyPos);
        float dot = Vector3.Dot(_dummy.Character.transform.forward, directionToOpponent);

        return Mathf.Abs(dummyPos.x - enemyPos.x) > _dummy.MaxRange || dot < 0 || dummyPos.y != enemyPos.y;
    }
}
