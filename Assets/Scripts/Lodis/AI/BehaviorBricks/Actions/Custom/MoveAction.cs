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

    public override void OnStart()
    {
        base.OnStart();
    }

    private bool CheckPanelInRange(params object[] args)
    {
        PanelBehaviour panel = (PanelBehaviour)args[0];
        float xDirection = _dummy.Character.transform.forward.x;

        bool isInFrontOpponent = xDirection * panel.Position.x < xDirection * _dummy.OpponentMove.Position.x;

        return Mathf.Abs(panel.Position.x - _dummy.OpponentMove.Position.x) < _dummy.MaxRange && panel.Position.y == _dummy.OpponentMove.Position.y && isInFrontOpponent;
    }

    public override TaskStatus OnUpdate()
    {
        PanelBehaviour panel = null;
        int xPos = (int)_dummy.AIMovement.MovementBehaviour.CurrentPanel.Position.x;

        if (_dummy.StateMachine.CurrentState != "Idle")
            return TaskStatus.ABORTED;

        if (BlackBoardBehaviour.Instance.Grid.GetPanel(CheckPanelInRange, out panel, _dummy.Character))
            _dummy.AIMovement.MoveToLocation(panel);

        return TaskStatus.COMPLETED;
    }
}
