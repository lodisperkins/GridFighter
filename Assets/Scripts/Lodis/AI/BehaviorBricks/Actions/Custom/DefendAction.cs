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

[Action("CustomAction/ChooseBestDefense")]
public class DefendAction : GOAction
{
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;
    private DefenseNode _decision;
    private DefenseNode _situation;
    private GridMovementBehaviour _opponentMoveBehaviour;
    private GridBehaviour _grid;
    private KnockbackBehaviour _dummyHealth;
    [InParam("RandomDecisionChosen")]
    private bool _randomDecisionChosen;

    public override void OnStart()
    {
        base.OnStart();
        _grid = BlackBoardBehaviour.Instance.Grid;
        //Return if the dummy can't currently defend itself
        if (_dummy.StateMachine.CurrentState != "Idle")
            return;

        //Grab the health component attached to know when it takes damage
        _dummyHealth = _dummy.GetComponent<KnockbackBehaviour>();

        //Store the current environment data
        _situation = new DefenseNode(GetPhysicsComponents(), null, null);

        //if the decision isn't null then it must be set to the last decision made
        if (_dummy.LastDefenseDecision != null)
        {
            //Increment the win count for the decision
            _dummy.LastDefenseDecision.Wins += 2;

            if (_dummy.LastDefenseDecision.DefenseDecision == DefenseDecisionType.COUNTER)
                _dummy.LastDefenseDecision.CounterAbilityName = _dummy.Moveset.LastAbilityInUse.abilityData.abilityName;

            //If the decision was random and didn't recieve negative points...
            if (_randomDecisionChosen && _dummy.LastDefenseDecision.Wins > 0)
            {
                //...add the decision to the tree
                _dummy.DefenseDecisions.AddDecision(_dummy.LastDefenseDecision);
            }
        }

        _dummy.Executor.blackboard.boolParams[4] = false;

        //Grab a decision that closely matches the current environment 
        _decision = (DefenseNode)_dummy.DefenseDecisions.GetDecision(_situation);
        DefenseDecisionType choice = DefenseDecisionType.EVADE;

        //If a decision was found...
        if (_decision != null)
        {
            //Mark it as visited an store its defense choice
            choice = _decision.DefenseDecision;
            _decision.VisitCount++;

            //If the decision has failed too many times remove it
            if (_decision.Wins <= -1)
            {
                _dummy.DefenseDecisions.RemoveDecision(_decision);
                _decision = null;
            }
        }

        //If a valid decision couldn't be found...
        if (_decision == null)
        {
            //Create a new random decision
            choice = (DefenseDecisionType)Random.Range(0, 4);
            _decision = new DefenseNode(GetPhysicsComponents(), null, null);
            _dummy.Executor.blackboard.boolParams[4] = true;
        }

        //Punish the decision if the dummy was damaged
        _dummyHealth.AddOnTakeDamageTempAction(() => _decision.Wins--);

        //Perform an action based on choice
        switch (choice)
        {
            case DefenseDecisionType.EVADE:
                //Gets a direction for the dummy to run to
                Vector3 fleeDirection = _dummy.transform.position - (_decision.AveragePosition + _decision.AverageVelocity);
                fleeDirection.Normalize();
                fleeDirection.Scale(new Vector3(_grid.PanelScale.x + _grid.PanelSpacingX, 0, _grid.PanelScale.z + _grid.PanelSpacingZ));
                PanelBehaviour panel = null;

                //If a panel is found at the new destination...
                if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(_dummy.transform.position + fleeDirection, out panel, false, _dummy.AIMovement.MovementBehaviour.Alignment))
                    //...move the dummy
                    _dummy.AIMovement.MoveToLocation(panel);
                break;
            case DefenseDecisionType.COUNTER:
                //Set the counter attacking parameter to be true so that the dummy attacks during the next iteration
                _dummy.Executor.blackboard.boolParams[3] = true;
                break;
            case DefenseDecisionType.PARRY:
                _dummy.GetComponent<CharacterDefenseBehaviour>().ActivateParry();
                break;
            case DefenseDecisionType.BURST:
                if (_dummy.GetComponent<KnockbackBehaviour>().LastTimeInKnockBack >= _dummy.TimeNeededToBurst)
                    _dummy.Moveset.UseBasicAbility(AbilityType.BURST);
                break;
        }

        _decision.DefenseDecision = choice;
        _dummy.LastDefenseDecision = _decision;
    }

    /// <summary>
    /// Gets a list of physics components from all attacks in range
    /// </summary>
    /// <returns></returns>
    private List<GridPhysicsBehaviour> GetPhysicsComponents()
    {
        List<GridPhysicsBehaviour> physics = new List<GridPhysicsBehaviour>();

        for (int i = 0; i < _dummy.GetAttacksInRange().Count; i++)
        {
            physics.Add(_dummy.GetAttacksInRange()[i].GetComponent<GridPhysicsBehaviour>());
        }

        return physics;
    }

    

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}
