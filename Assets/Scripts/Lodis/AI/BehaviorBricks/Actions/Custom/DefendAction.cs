﻿using BBCore.Actions;
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

        List<HitColliderBehaviour> attacks = _dummy.GetAttacksInRange();
        int currentAttackCount = attacks.Count;

        //Store the current environment data
        _situation = new DefenseNode(attacks, null, null);

        //The dummy doesn't make a new decision of this situation is the same as the last and if it can't defend.
        if (_situation.Compare(_dummy.LastDefenseDecision) == 1f || (_dummy.StateMachine.CurrentState != "Idle" && !_dummy.Moveset.CanBurst))
            return;

        //if the decision isn't null then it must be set to the last decision made
        if (_dummy.LastDefenseDecision != null && _dummy.LastDefenseDecision.GetCountOfActiveHitBoxes() == 0)
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
            choice = (DefenseDecisionType)UnityEngine.Random.Range(0, 5);
            _decision = new DefenseNode(_dummy.GetAttacksInRange(), null, null);
            _dummy.Executor.blackboard.boolParams[4] = true;
        }

        _dummy.LastDefenseDecision = _decision;
        //Punish the decision if the dummy was damaged
        _dummy.Knockback.AddOnTakeDamageTempAction(PunishLastDecision);
        

        //Picks a decision based on whether or not the dummy is grounded
        if (_dummy.Knockback.Physics.IsGrounded)
            choice = ChooseGroundDefense();
        else
            choice = ChooseAirDefense();

        _decision.DefenseDecision = choice;
        _canMakeNewDecision = false;
        _lastAttackCount = attacks.Count;
    }

    private DefenseDecisionType ChooseGroundDefense()
    {
        DefenseDecisionType choice = (DefenseDecisionType)UnityEngine.Random.Range(0, 3);
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
        }

        return choice;
    }
    
    private DefenseDecisionType ChooseAirDefense()
    {
        DefenseDecisionType choice = (DefenseDecisionType)UnityEngine.Random.Range(4, 7);

        //Perform an action based on choice
        switch (choice)
        {
            case DefenseDecisionType.BURST:
                //Burst if the dummy was in knockback long enough and is allowed to burst
                if (_dummy.Knockback.LastTimeInKnockBack >= _dummy.TimeNeededToBurst && _dummy.Moveset.CanBurst)
                    _dummy.Moveset.UseBasicAbility(AbilityType.BURST);

                break;
            case DefenseDecisionType.BREAKFALLJUMP:
                //Gets a direction for the dummy to jump towards based on the barrier its touching
                float jumpDirection = -Convert.ToInt32(_dummy.TouchingOpponentBarrier) + Convert.ToInt32(_dummy.TouchingBarrier);

                if (jumpDirection == 0)
                {
                    choice = DefenseDecisionType.NONE;
                    break;
                }

                //Update attack direction and brace for impact so the dummy can jump away from the barrer
                jumpDirection *= _dummy.Character.transform.forward.x;
                _dummy.AttackDirection = new Vector2(jumpDirection, 0);
                _dummy.Defense.Brace();
                break;
        }

        return choice;
    }

    /// <summary>
    /// Decrements the last decisions wins by 2
    /// </summary>
    private void PunishLastDecision()
    {
        if (_dummy.LastDefenseDecision != null)
        {
            _dummy.LastDefenseDecision.Wins -= 2;
            //_dummy.DefenseDecisions.RemoveDecision(_dummy.LastDefenseDecision);
            _dummy.LastDefenseDecision = null;
        }
        _canMakeNewDecision = true;

        if (!_dummy.CopyAttacks)
            return;

        Ability ability = _dummy.Moveset.GetAbility(args => ((Ability)args[0]).abilityData.GetColliderInfo(0).Name == _dummy.Knockback.LastCollider.ColliderInfo.Name);

        if (ability == null)
            return;


        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.Character.transform.position;

        AbilityData abilityData = ability.abilityData;

        MovesetBehaviour opponentMoveset = _dummy.Opponent.GetComponent<MovesetBehaviour>();

        _dummy.AttackDecisions.AddDecision(new AttackNode(displacement, _dummy.Knockback.Health, 0, abilityData.startUpTime,
            abilityData.abilityName, opponentMoveset.LastAttackStrength, _dummy.Knockback.Physics.LastVelocity, null, null));
    }

    public override TaskStatus OnUpdate()
    {
        if (_dummy.StateMachine.CurrentState == "Idle")
            _dummy.Defense.DeactivateBrace();

        return TaskStatus.COMPLETED;
    }
}
