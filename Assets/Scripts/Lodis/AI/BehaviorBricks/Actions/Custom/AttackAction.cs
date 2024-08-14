//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using BBCore.Actions;
//using BBUnity.Actions;
//using Lodis.AI;
//using Pada1.BBCore;
//using Lodis.Gameplay;
//using Lodis.Movement;
//using Lodis.Utility;
//using Pada1.BBCore.Tasks;
//using FixedPoints;

//[Action("CustomAction/ChooseBestAttack")]
//public class AttackAction : GOAction
//{
//    [InParam("Owner")]
//    private AIControllerBehaviour _dummy;
//    private AttackNode _decision;
//    private AttackNode _situation;

//    public override void OnStart()
//    {
//        base.OnStart();

//        //Return if the dummy isn't attacking or counter attacking
//        if (!_dummy.CanAttack && !_dummy.Executor.blackboard.boolParams[3])
//            return;

//        //Set counter attacking to false so the next attack isn't set to counter attack
//        _dummy.Executor.blackboard.boolParams[3] = false;

//        //If the dummy was shielding...
//        if (_dummy.StateMachine.CurrentState == "Parrying")
//        {
//            //..deactivate the shield so that they don't attack an defend at the same time
//            _dummy.Defense.DeactivateShield();
//            return;
//        }

//        //The dummy is only allowed to attack if its in the idle or the attack state
//        if (_dummy.StateMachine.CurrentState != "Idle" && _dummy.StateMachine.CurrentState != "Attack")
//            return;
//        else if (_dummy.StateMachine.CurrentState == "Attack" && !_dummy.Moveset.GetCanUseAbility())
//            return;


//        if (_dummy.HasBuffered)
//            return;


//        //Gather information about the environment
//        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.Character.transform.position;
//        float targetHealth = _dummy.OpponentKnockback.Health;

//         _situation = new AttackNode(displacement, targetHealth, 0, 0, "", 0, (Vector3)_dummy.OpponentKnockback.Physics.Velocity, null, null);

//        //Get a decision based on the current situation
//        _decision = (AttackNode)_dummy.AttackDecisions.GetDecision(_situation, _dummy.OpponentMove, _dummy.OpponentDefense.IsDefending, targetHealth, _dummy);
        
//        //If the decision has failed too many times remove it
//        if (_decision?.Wins < -1)
//        {
//            _dummy.AttackDecisions.RemoveDecision(_decision);
//            _decision = null;
//        }

//        if (_decision == null)
//        {
//            UseRandomDecisionAbility();
//            return;
//        }


//        //Mark it as visited
//        _decision.VisitCount++;

//        //Check if the either deck contains the ability that the dummy wants to use
//        if (!_dummy.Moveset.SpecialDeckContains(_decision.AbilityName) && !_dummy.Moveset.NormalDeckContains(_decision.AbilityName))
//            return;

//        //Use the ability and mark it as visited
//        UseDecisionAbility(_dummy.Moveset.GetAbilityByName(_decision.AbilityName));
//        _decision.VisitCount++;
        
//    }

//    /// <summary>
//    /// Uses the special ability if it is in the current hand or basic deck
//    /// </summary>
//    /// <param name="ability">The ability that the dummy is trying to use</param>
//    void UseDecisionAbility(Ability ability)
//    {
//        //Uses the ability based on its type
//        _dummy.BufferAction(ability, _decision.AttackStrength, _decision.AttackDirection);

//        //Decrease the wins by default. Wins only have a net positive on hit
//        _decision.Wins--;
//        ability.OnHitTemp += IncreaseDecisionScore;
//    }

//    /// <summary>
//    /// Uses some random special or basic ability
//    /// </summary>
//    void UseRandomDecisionAbility()
//    {
//        //Pick a random range and attack
//        AbilityType attackType = (AbilityType)UnityEngine.Random.Range(0, 11);
//        Vector2 attackDirection = new Vector2(UnityEngine.Random.Range(-1, 2), UnityEngine.Random.Range(-1, 2));
//        float attackStrength = UnityEngine.Random.Range(0, 1.3f);

//        //Store information about the environment in case the hit is successful
//        Vector3 displacement = _dummy.Opponent.transform.position - _dummy.Character.transform.position;
//        float targetHealth = _dummy.OpponentKnockback.Health;

//        Ability ability = null;

//        if (attackType == AbilityType.UNBLOCKABLE)
//            attackType = AbilityType.SPECIAL;

//        //Picks an ability in the current hand at random if it is special
//        if (attackType == AbilityType.SPECIAL)
//        {
//            int slot = Random.Range(0, 2);
//            ability = _dummy.Moveset.SpecialAbilitySlots[slot];

//            if (ability == null)
//                return;

//            //Store the decision in the tree if the hit was successful
//            ability.OnHitTemp += args => CreateNewDecision(targetHealth, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
//            _dummy.Moveset.UseSpecialAbility(slot, attackStrength, attackDirection);

//            return;
//        }

//        //Change the x direction based on dummy facing so that forward and backwards are attacks are oriented correctly
//        attackDirection.x *= Mathf.Round(_dummy.transform.forward.x);
//        //Clamp the attack direction to appropriate values
//        attackDirection.x = Mathf.Round(attackDirection.x);
//        attackDirection.y = Mathf.Round(attackDirection.y);

//        //Changes the normal attack into a charge version depending on the attack strength found
//        if ((int)attackType >= 4 && (int)attackType < 8)
//        {
//            attackStrength = UnityEngine.Random.Range(1.1f, 1.3f);
//            float timeHeld = (attackStrength - 1) / 0.1f;
//            _dummy.StartCoroutine(_dummy.ChargeRoutine(timeHeld, attackType));
//        }



//        ability = _dummy.Moveset.GetAbilityByType(attackType);

//        if (ability == null) return;

//        //Store the decision in the tree if the hit was successful
//        ability.OnHitTemp += args => CreateNewDecision(targetHealth, ability.abilityData.startUpTime, ability.abilityData.abilityName, attackStrength, args);
//        _dummy.Moveset.UseBasicAbility(attackType, attackStrength, attackDirection);
//        return;
//    }

//    /// <summary>
//    /// Tries to create and store a new decision
//    /// </summary>
//    /// <param name="targetHealth">The health of the opponent</param>
//    /// <param name="startUpTime">The amount of startup this move has</param>
//    /// <param name="name">The name of the ability</param>
//    /// <param name="attackStrength">The strength of the ability</param>
//    /// <param name="args">Any additional ability argument</param>
//    void CreateNewDecision(float targetHealth, float startUpTime, string name, float attackStrength, Collision collision)
//    {
//        GameObject collisionObject = collision.Entity.UnityObject;

//        //Don't create a new decision of the attack didn't hit a player or if it hit it's owner
//        if (!collisionObject.CompareTag("Player") || collisionObject == _dummy.Character.gameObject)
//            return;

//        //Initialize a new decision with the current situation to add to the tree
//        Vector3 displacement = collisionObject.transform.position - _dummy.Character.transform.position;
//        _decision = (AttackNode)_dummy.AttackDecisions.AddDecision(new AttackNode(displacement, targetHealth, 0, startUpTime, name, attackStrength, (Vector3)collisionObject.GetComponent<GridPhysicsBehaviour>().Velocity, null, null));

//        if (_decision == null)
//            return;

//        //Decrement the decisions effectiveness by default
//        _decision.Wins--;
//        _decision.VisitCount++;

//        //Increase the score based on what was hit
//        IncreaseDecisionScore(collision);
//    }

//    /// <summary>
//    /// Increases the score of a decision based on what it hit
//    /// </summary>
//    /// <param name="args">Additional info about the current situation</param>
//    void IncreaseDecisionScore(Collision collision)
//    {
//        GameObject collisionObject = collision.Entity.UnityObject;

//        //Exit if the attack didn't hit a player or barrier
//        if (!collisionObject.CompareTag("Player") && !collisionObject.CompareTag("Structure"))
//            return;

//        //Incresase the shield stat if the opponent was blocking
//        if (_dummy.OpponentDefense.IsDefending && collisionObject.CompareTag("Player"))
//            _decision.ShieldEffectiveness ++;

//        //Increment the decision by two to give the attack a net positive
//        if (!_dummy.OpponentKnockback.IsInvincible) _decision.Wins += (2 + (int)_dummy.OpponentKnockback.LastTotalKnockBack / 2);

//        _decision.KnockBackDealt = _dummy.OpponentKnockback.LastTotalKnockBack;
//    }

//    public override TaskStatus OnUpdate()
//    {
//        return TaskStatus.COMPLETED;
//    }
//}
