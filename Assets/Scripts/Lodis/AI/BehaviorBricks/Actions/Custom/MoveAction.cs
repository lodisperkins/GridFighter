using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pada1.BBCore.Tasks;
using Pada1.BBCore;
using BBUnity.Actions;
using Lodis.AI;
using Lodis.Movement;

[Action("CustomAction/MoveTowardsEnemy")]
public class MoveAction : GOAction
{
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;
    private AIDummyMovementBehaviour _movementBehaviour;
    private GridMovementBehaviour _opponentMovement;

    public override void OnStart()
    {
        base.OnStart();
        _movementBehaviour = _dummy.GetComponent<AIDummyMovementBehaviour>();
        _opponentMovement = _dummy.Opponent.GetComponent<GridMovementBehaviour>();
    }

    public override TaskStatus OnUpdate()
    {
        _movementBehaviour.MoveToLocation(_opponentMovement.CurrentPanel);
        return TaskStatus.COMPLETED;
    }
}
