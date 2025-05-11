using BBCore.Actions;
using BBUnity.Actions;
using Lodis.AI;
using Pada1.BBCore;
using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.Utility;
using Pada1.BBCore.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.GridScripts;
using System;
using Lodis.ScriptableObjects;

[Action("CustomAction/ChooseBestDefense")]
public class DefendAction : GOAction
{
    [InParam("Owner")]
    private AIControllerBehaviour _dummy;
    private GridMovementBehaviour _opponentMoveBehaviour;
    private GridBehaviour _grid;
    [InParam("RandomDecisionChosen")]
    private bool _randomDecisionChosen;
    [InParam("CanMakeNewDecision")]
    private bool _canMakeNewDecision = true;
    [InParam("LastAttackCount")]
    private int _lastAttackCount;

    public override void OnStart()
    {
        base.OnStart();

    }
  
    public override TaskStatus OnUpdate()
    {
        if (_dummy.StateMachine.CurrentState == "Idle")
            _dummy.Defense.DeactivateBrace();

        return TaskStatus.COMPLETED;
    }
}
