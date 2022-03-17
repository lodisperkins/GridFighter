using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pada1.BBCore;           // Code attributes
using Pada1.BBCore.Framework; // ConditionBase
using BBUnity.Conditions;
using Lodis.Movement;
using Lodis.AI;
using Lodis.Gameplay;

[Condition("CustomConditions/IsSafe")]
public class IsSafeCondition : GOCondition
{
    private GameObject _opponent = null;

    private List<HitColliderBehaviour> FindAttacksInRange(AttackDummyBehaviour dummy)
    {
        if (dummy.MovementBehaviour.Alignment == Lodis.GridScripts.GridAlignment.LEFT)
        {
            _opponent = BlackBoardBehaviour.Instance.Player2;
            return BlackBoardBehaviour.Instance.GetRHSActiveColliders().FindAll(collider => Vector3.Distance(collider.gameObject.transform.position, gameObject.transform.position) <= dummy.SenseRadius);
        }
        else if (dummy.MovementBehaviour.Alignment == Lodis.GridScripts.GridAlignment.RIGHT)
        {
            _opponent = BlackBoardBehaviour.Instance.Player1;
            return BlackBoardBehaviour.Instance.GetLHSActiveColliders().FindAll(collider => Vector3.Distance(collider.gameObject.transform.position, gameObject.transform.position) <= dummy.SenseRadius);
        }

        return new List<HitColliderBehaviour>();
    }

    /// <summary>
    /// Considered unsafe if hit boxes are in range, in the tumbling state, or an attack has been started on the same row
    /// </summary>
    /// <returns></returns>
    public override bool Check()
    {
        AttackDummyBehaviour dummy = gameObject.GetComponent<AttackDummyBehaviour>();

        dummy.GetAttacksInRange().AddRange(FindAttacksInRange(dummy));

        if (dummy.GetAttacksInRange().Count > 0)
            return false;

        if (dummy.StateMachine.CurrentState == "Tumbling" || dummy.StateMachine.CurrentState == "Flinching")
            return false;


        if (_opponent.GetComponent<GridMovementBehaviour>().Position.y == dummy.MovementBehaviour.Position.y && _opponent.GetComponent<CharacterStateMachineBehaviour>().StateMachine.CurrentState == "Attack")
            return false;

        return true;
    }
}
