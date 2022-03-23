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

    public override TaskStatus OnUpdate()
    {
        PanelBehaviour panel = null;
        int xPos = (int)_movementBehaviour.MovementBehaviour.CurrentPanel.Position.x;
        for (int i = 0; i < BlackBoardBehaviour.Instance.Grid.Width; i++)
        {
            if (BlackBoardBehaviour.Instance.Grid.GetPanel(xPos, (int)_opponentMovement.CurrentPanel.Position.y, out panel, false))
            {
                if (panel.Alignment == _movementBehaviour.MovementBehaviour.Alignment)
                    _movementBehaviour.MoveToLocation(panel);
                
                break;
            }

            xPos += (int)_movementBehaviour.transform.forward.x;
        }

        return TaskStatus.COMPLETED;
    }
}
