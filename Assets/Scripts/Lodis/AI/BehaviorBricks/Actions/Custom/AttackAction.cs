using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BBCore.Actions;
using BBUnity.Actions;
using Lodis.AI;
using Pada1.BBCore;
using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.Utility;
using Pada1.BBCore.Tasks;

[Action("CustomAction/ChooseBestAttack")]
public class AttackAction : GOAction
{
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;
    private AttackNode _decision;
    private AttackNode _situation;
    private GridMovementBehaviour _opponentMoveBehaviour;

    public override void OnStart()
    {
        base.OnStart();

        _dummy.Executor.blackboard.boolParams[3] = false;

        //The dummy is only allowed to attack if its in the idle or the attack state
        if (_dummy.StateMachine.CurrentState != "Idle" && _dummy.StateMachine.CurrentState != "Attack")
            return;
        else if (_dummy.StateMachine.CurrentState == "Attack" && !_dummy.Moveset.GetCanUseAbility())
            return;

        //Gather information about the environment
        _opponentMoveBehaviour = _dummy.Opponent.GetComponent<GridMovementBehaviour>();
        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.transform.position;
        float targetHealth = _dummy.Opponent.GetComponent<HealthBehaviour>().Health;
        _situation = new AttackNode(displacement, targetHealth, 0, 0, "", 0, null, null);

        //Get a decision based on the current situation
        _decision = (AttackNode)_dummy.AttackDecisions.GetDecision(_situation, _opponentMoveBehaviour, _opponentMoveBehaviour.IsBehindBarrier, targetHealth, _dummy);

        //If a decision was found...
        if (_decision != null)
        {
            //Mark it as visited
            _decision.VisitCount++;

            //If the decision has failed too many times remove it
            if (_decision.Wins <= -1)
            {
                _dummy.AttackDecisions.RemoveDecision(_decision);
                _decision = null;
            }
        }

        //If a valid decision wasn't found...
        if (_decision != null)
        {
            //...check if the either deck contains the ability that the dummy wants to use
            if (!_dummy.Moveset.SpecialDeckContains(_decision.AbilityName) && !_dummy.Moveset.NormalDeckContains(_decision.AbilityName))
                return;

            //Use the ability and mark it as visited
            UseDecisionAbility(_dummy.Moveset.GetAbilityByName(_decision.AbilityName));
            _decision.VisitCount++;
        }
        //Otherwise pick a new random decision
        else
        {
            UseRandomDecisionAbility();
        }  
    }

    /// <summary>
    /// Uses the special ability if it is in the current hand or basic deck
    /// </summary>
    /// <param name="ability">The ability that the dummy is trying to use</param>
    void UseDecisionAbility(Ability ability)
    {
        //Uses the ability based on its type
        if (_dummy.Moveset.GetAbilityNamesInCurrentSlots()[0] == ability.abilityData.name)
            _dummy.Moveset.UseSpecialAbility(0, _decision.AttackStrength, _decision.AttackDirection);
        else if (_dummy.Moveset.GetAbilityNamesInCurrentSlots()[1] == ability.abilityData.name)
            _dummy.Moveset.UseSpecialAbility(1, _decision.AttackStrength, _decision.AttackDirection);
        else if (ability.abilityData.AbilityType != AbilityType.SPECIAL)
            _dummy.Moveset.UseBasicAbility(_decision.AbilityName, _decision.AttackStrength, _decision.AttackDirection);

        //Decrease the wins by default. Wins only have a net positive on hit
        _decision.Wins--;
        ability.OnHitTemp += IncreaseDecisionScore;
    }

    /// <summary>
    /// Uses some random special or basic ability
    /// </summary>
    void UseRandomDecisionAbility()
    {
        //Pick a random range and attack
        AbilityType attackType = (AbilityType)UnityEngine.Random.Range(0, 9);
        Vector2 attackDirection = new Vector2(UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2));
        float attackStrength = UnityEngine.Random.Range(0, 1.1f);

        //Store information about the environment in case the hit is successful
        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.transform.position;
        float targetHealth = _dummy.Opponent.GetComponent<HealthBehaviour>().Health;

        Ability ability = null;

        //Picks an ability in the current hand at random if it is special
        if (attackType == AbilityType.SPECIAL)
        {
            int slot = Random.Range(0, 2);
            ability = _dummy.Moveset.SpecialAbilitySlots[slot];

            if (ability == null)
                return;

            //Store the decision in the tree if the hit was successful
            ability.OnHitTemp += args => CreateNewDecision(targetHealth, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
            _dummy.Moveset.UseSpecialAbility(slot, attackStrength, attackDirection);

            return;
        }

        attackDirection.x *= Mathf.Round(_dummy.transform.forward.x);

        //Decide which ability type to use based on the input
        if (attackDirection.y != 0)
            attackType = AbilityType.WEAKSIDE;
        else if (attackDirection.x < 0)
            attackType = AbilityType.WEAKBACKWARD;
        else if (attackDirection.x > 0)
            attackType = AbilityType.WEAKFORWARD;
        else
            attackType = AbilityType.WEAKNEUTRAL;

        float timeHeld = (attackStrength - 1) / 0.1f;

        if (timeHeld > 0.5f)
        {
            attackType += 4;
            _dummy.StartCoroutine(_dummy.ChargeRoutine(timeHeld, attackType));
        }

        ability = _dummy.Moveset.GetAbilityByType(attackType);
        //Store the decision in the tree if the hit was successful
        ability.OnHitTemp += args => CreateNewDecision(targetHealth, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
        _dummy.Moveset.UseBasicAbility(attackType, attackStrength, attackDirection);
        return;
    }

    /// <summary>
    /// Tries to create and store a new decision
    /// </summary>
    /// <param name="targetHealth">The health of the opponent</param>
    /// <param name="startUpTime">The amount of startup this move has</param>
    /// <param name="name">The name of the ability</param>
    /// <param name="attackStrength">The strength of the ability</param>
    /// <param name="args">Any additional ability argument</param>
    void CreateNewDecision(float targetHealth, float startUpTime, string name, float attackStrength, params object[] args)
    {
        GameObject collisionObject = (GameObject)args[0];

        if (!collisionObject.CompareTag("Player") || collisionObject == _dummy.gameObject)
            return;

        Vector3 displacement = collisionObject.transform.position - _dummy.transform.position;
        _decision = (AttackNode)_dummy.AttackDecisions.AddDecision(new AttackNode(displacement, targetHealth, 0, startUpTime, name, attackStrength, null, null));

        if (_decision == null)
            return;

        _decision.Wins--;
        _decision.VisitCount++;
        IncreaseDecisionScore(args);
    }

    void IncreaseDecisionScore(params object[] args)
    {
        GameObject collisionObject = (GameObject)args[0];

        if (!collisionObject.CompareTag("Player") && !collisionObject.CompareTag("Structure"))
            return;

        if (_opponentMoveBehaviour.IsBehindBarrier && collisionObject.CompareTag("Player"))
            _decision.BarrierEffectiveness += 2;
        else if (collisionObject.CompareTag("Structure"))
        {
            _decision.BarrierEffectiveness++;
            return;
        }


        KnockbackBehaviour knockback = collisionObject.GetComponent<KnockbackBehaviour>();

        if (!knockback.IsInvincible) _decision.Wins += 2;

        _decision.KnockBackDealt = knockback.LastTotalKnockBack;
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}
