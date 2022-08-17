using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pada1.BBCore.Tasks;
using Pada1.BBCore;
using BBUnity.Actions;
using Lodis.AI;
using Lodis.Movement;
using Lodis.Gameplay;
using Lodis.GridScripts;

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

    private bool CheckPanelInRange(params object[] args)
    {
        PanelBehaviour panel = (PanelBehaviour)args[0];
        return Mathf.Abs(panel.Position.x - _movementBehaviour.MovementBehaviour.Position.x) < _dummy.MaxRange && panel.Position.y == _opponentMovement.Position.y;
    }

    public override TaskStatus OnUpdate()
    {
        PanelBehaviour panel = null;
        int xPos = (int)_movementBehaviour.MovementBehaviour.CurrentPanel.Position.x;

        if (_dummy.StateMachine.CurrentState != "Idle")
            return TaskStatus.ABORTED;

        if (BlackBoardBehaviour.Instance.Grid.GetPanel(CheckPanelInRange, out panel, _dummy.Character))
            _movementBehaviour.MoveToLocation(panel);

        return TaskStatus.COMPLETED;
    }
}
