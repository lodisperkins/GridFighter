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
    [InParam("RandomDecisionChosen")]
    private bool _randomDecisionChosen;
    [InParam("CanMakeNewDecision")]
    private bool _canMakeNewDecision = true;
    [InParam("LastAttackCount")]
    private int _lastAttackCount;

    public override void OnStart()
    {
        base.OnStart();
        _grid = BlackBoardBehaviour.Instance.Grid;
        //Return if the dummy can't currently defend itself
        //if (_dummy.StateMachine.CurrentState != "Idle")
        //    return;

        List<HitColliderBehaviour> attacks = _dummy.GetAttacksInRange();
        int currentAttackCount = attacks.Count;

        //Store the current environment data
        _situation = new DefenseNode(attacks, null, null);

        //if the decision isn't null then it must be set to the last decision made
        if (_dummy.LastDefenseDecision != null)
        {
            //Increment the win count for the decision
            _canMakeNewDecision = true;
            _dummy.LastDefenseDecision.Wins++;

            if (_dummy.LastDefenseDecision.DefenseDecision == DefenseDecisionType.COUNTER)
                _dummy.LastDefenseDecision.CounterAbilityName = _dummy.Moveset.LastAbilityInUse?.abilityData.abilityName;

            //If the decision was random and didn't recieve negative points...
            if (_randomDecisionChosen && _dummy.LastDefenseDecision.Wins > 0)
            {
                //...add the decision to the tree
                _dummy.DefenseDecisions.AddDecision(_dummy.LastDefenseDecision);
            }
        }

        _dummy.Executor.blackboard.boolParams[4] = false;

        if (!_canMakeNewDecision)
            return;

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
            choice = (DefenseDecisionType)Random.Range(0, 5);
            _decision = new DefenseNode(_dummy.GetAttacksInRange(), null, null);
            _dummy.Executor.blackboard.boolParams[4] = true;
        }

        if (_dummy.Knockback.CurrentAirState == AirState.TUMBLING && _dummy.Moveset.CanBurst && choice > DefenseDecisionType.COUNTER)
            choice = DefenseDecisionType.BURST;

        _dummy.LastDefenseDecision = _decision;
        //Punish the decision if the dummy was damaged
        _dummy.Knockback.AddOnTakeDamageTempAction(() => { _dummy.LastDefenseDecision.Wins -= 2; _canMakeNewDecision = true; });

        //Perform an action based on choice
        switch (choice)
        {
            case DefenseDecisionType.EVADE:
                //Gets a direction for the dummy to run to
                Vector3 fleeDirection = _dummy.Character.transform.position - (_decision.AveragePosition + _decision.AverageVelocity);
                fleeDirection.Normalize();
                fleeDirection.Scale(new Vector3(_grid.PanelScale.x + _grid.PanelSpacingX, 0, _grid.PanelScale.z + _grid.PanelSpacingZ));
                fleeDirection = new Vector3(fleeDirection.z, 0, -fleeDirection.x);
                PanelBehaviour panel = null;

                //If a panel is found at the new destination...
                if (BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(_dummy.Character.transform.position + fleeDirection, out panel, false, _dummy.AIMovement.MovementBehaviour.Alignment))
                    //...move the dummy
                    _dummy.AIMovement.MoveToLocation(panel);
                break;
            case DefenseDecisionType.COUNTER:
                //Set the counter attacking parameter to be true so that the dummy attacks during the next iteration
                _dummy.Executor.blackboard.boolParams[3] = true;
                break;
            case DefenseDecisionType.PARRY:
                RoutineBehaviour.Instance.StartNewConditionAction(args => _dummy.Defense.BeginParry(), condition => _dummy.StateMachine.CurrentState == "Idle");
                break;
            case DefenseDecisionType.BURST:
                if (_dummy.Knockback.LastTimeInKnockBack >= _dummy.TimeNeededToBurst && _dummy.Moveset.CanBurst)
                    _dummy.Moveset.UseBasicAbility(AbilityType.BURST);
                else if (_dummy.Knockback.Physics.IsGrounded)
                    RoutineBehaviour.Instance.StartNewConditionAction(args => _dummy.Defense.BeginParry(), condition => _dummy.StateMachine.CurrentState == "Idle");
                break;
            case DefenseDecisionType.PHASESHIFT:
                //Gets a direction for the dummy to run to
                Vector3 phaseDirection = (_decision.AveragePosition + _decision.AverageVelocity) - _dummy.Character.transform.position;
                phaseDirection.Normalize();

                if (_dummy.StateMachine.CurrentState == "Idle" || _dummy.StateMachine.CurrentState == "Moving")
                    _dummy.Defense.ActivatePhaseShift(new Vector2(phaseDirection.x, phaseDirection.z));

                break;
        }

        _decision.DefenseDecision = choice;
        _canMakeNewDecision = false;
        _lastAttackCount = attacks.Count;
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
