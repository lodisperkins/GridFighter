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

        if (_dummy.StateMachine.CurrentState != "Idle" && _dummy.StateMachine.CurrentState != "Attack")
            return;

        _opponentMoveBehaviour = _dummy.Opponent.GetComponent<GridMovementBehaviour>();
        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.transform.position;
        float targetHealth = _dummy.Opponent.GetComponent<HealthBehaviour>().Health;
        _situation = new AttackNode(displacement, targetHealth, 0, 0, "", 0, null, null);
        _decision = (AttackNode)_dummy.AttackDecisions.GetDecision(_situation, _opponentMoveBehaviour, _opponentMoveBehaviour.IsBehindBarrier, targetHealth, _dummy);

        if (_decision != null)
        {
            if (!_dummy.Moveset.SpecialDeckContains(_decision.AbilityName) && !_dummy.Moveset.NormalDeckContains(_decision.AbilityName))
                return;

            UseDecisionAbility(_dummy.Moveset.GetAbilityByName(_decision.AbilityName));
            _decision.VisitCount++;
        }
        else
        {
            UseRandomDecisionAbility();
        }  
    }

    void UseDecisionAbility(Ability ability)
    {
        if (_dummy.Moveset.GetAbilityNamesInCurrentSlots()[0] == ability.abilityData.name)
            _dummy.Moveset.UseSpecialAbility(0, _decision.AttackStrength, _decision.AttackDirection);
        else if (_dummy.Moveset.GetAbilityNamesInCurrentSlots()[1] == ability.abilityData.name)
            _dummy.Moveset.UseSpecialAbility(1, _decision.AttackStrength, _decision.AttackDirection);
        else if (ability.abilityData.AbilityType != AbilityType.SPECIAL)
            _dummy.Moveset.UseBasicAbility(_decision.AbilityName, _decision.AttackStrength, _decision.AttackDirection);

        ability.OnHit += IncreaseDecisionScore;
    }

    void UseRandomDecisionAbility()
    {
        AbilityType attackType = (AbilityType)UnityEngine.Random.Range(0, 9);

        Vector2 attackDirection = new Vector2(UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2));
        float attackStrength = UnityEngine.Random.Range(0, 1.1f);
        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.transform.position;
        float targetHealth = _dummy.Opponent.GetComponent<HealthBehaviour>().Health;
        Ability ability = null;

        if (((int)attackType) > 3 && ((int)attackType) < 8)
        {
            ability = _dummy.Moveset.GetAbilityByType(attackType);
            ability.OnHit += args => CreateNewDecision(targetHealth, 0, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
            _dummy.StartCoroutine(_dummy.ChargeRoutine((attackStrength - 1) / 0.1f, attackType));
            return;
        }

        if (attackType == AbilityType.SPECIAL)
        {
            int slot = Random.Range(0, 2);
            ability = _dummy.Moveset.SpecialAbilitySlots[slot];

            if (ability == null)
                return;

            ability.OnHit += args => CreateNewDecision(targetHealth, 0, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
            _dummy.Moveset.UseSpecialAbility(slot, attackStrength, attackDirection);
        }
        else
        {
            ability = _dummy.Moveset.GetAbilityByType(attackType);
            ability.OnHit += args => CreateNewDecision(targetHealth, 0, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
            ability = _dummy.Moveset.UseBasicAbility(attackType, new object[] { attackStrength, attackDirection });
        }
    }

    void CreateNewDecision(float targetHealth, int barrierEffectiveness, float startUpTime, string name, float attackStrength, params object[] args)
    {
        GameObject collisionObject = (GameObject)args[0];

        if (!collisionObject.CompareTag("Player") || collisionObject == _dummy.gameObject)
            return;

        Vector3 displacement = collisionObject.transform.position - _dummy.transform.position;
        _decision = (AttackNode)_dummy.AttackDecisions.AddDecision(new AttackNode(displacement, targetHealth, 0, startUpTime, name, attackStrength, null, null));

        if (_decision == null)
            return;

        _decision.VisitCount++;
        IncreaseDecisionScore(args);
    }

    void IncreaseDecisionScore(params object[] args)
    {
        GameObject collisionObject = (GameObject)args[0];

        if (!collisionObject.CompareTag("Player") && !collisionObject.CompareTag("Structure"))
            return;

        if (_opponentMoveBehaviour.IsBehindBarrier && collisionObject.CompareTag("Player"))
            _decision.BarrierEffectiveness = 2;
        else if (collisionObject.CompareTag("Structure"))
        {
            _decision.BarrierEffectiveness = 1;
            return;
        }

        _decision.Wins++;

        KnockbackBehaviour knockback = collisionObject.GetComponent<KnockbackBehaviour>();
        _decision.KnockBackDealt = knockback.LastTotalKnockBack;
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}
