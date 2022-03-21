using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BBCore.Actions;
using BBUnity.Actions;
using Lodis.AI;
using Pada1.BBCore;
using Lodis.Gameplay;
using Lodis.Movement;

[Action("CustomAction/Attack")]
public class AttackAction : GOAction
{
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;
    private AttackDecisionTree _attackDecisions;
    private AttackNode _decision;
    private AttackNode _situation;

    public override void OnStart()
    {
        base.OnStart();
        _attackDecisions = new AttackDecisionTree();
        _attackDecisions.Load();

        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.transform.position;
        float targetHealth = _dummy.Opponent.GetComponent<HealthBehaviour>().Health;
        _situation = new AttackNode(displacement, targetHealth, 0, 0, "", 0, null, null);
        _decision = _attackDecisions.GetDecision(_situation, _dummy.Opponent.GetComponent<GridMovementBehaviour>(), Need to find if behind barrier);
        
    }

}
