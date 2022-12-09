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

        ///Stores if the x position is less than the opponents based on the direction their facing. 
        ///This is to have the dummy continue combos when the enemy falls behind them.
        bool isInFrontOpponent = xDirection * panel.Position.x < xDirection * _dummy.OpponentMove.Position.x;

        //Returns the distance between the dummy and its target and whether or not it's in front
        return Mathf.Abs(panel.Position.x - _dummy.OpponentMove.Position.x) < _dummy.MaxRange && panel.Position.y == _dummy.OpponentMove.Position.y && isInFrontOpponent;
    }

    public override TaskStatus OnUpdate()
    {
        PanelBehaviour panel = null;
        int xPos = (int)_dummy.AIMovement.MovementBehaviour.CurrentPanel.Position.x;

        //Quit trying to move if it's not possible
        if (_dummy.StateMachine.CurrentState != "Idle")
            return TaskStatus.ABORTED;

        //Move to location if there is a valid panel in range
        if (BlackBoardBehaviour.Instance.Grid.GetPanel(CheckPanelInRange, out panel, _dummy.Character))
            _dummy.AIMovement.MoveToLocation(panel);

        return TaskStatus.COMPLETED;
    }
}
