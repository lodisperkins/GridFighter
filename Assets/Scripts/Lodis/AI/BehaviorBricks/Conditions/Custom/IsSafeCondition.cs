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
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;

    //private List<HitColliderBehaviour> FindAttacksInRange(AttackDummyBehaviour dummy)
    //{
    //    if (dummy.AIMovement.MovementBehaviour.Alignment == Lodis.GridScripts.GridAlignment.LEFT)
    //    {
    //        _opponent = BlackBoardBehaviour.Instance.Player2;
    //        return BlackBoardBehaviour.Instance.GetRHSActiveColliders().FindAll(collider => Vector3.Distance(collider.gameObject.transform.position, gameObject.transform.position) <= dummy.SenseRadius);
    //    }
    //    else if (dummy.AIMovement.MovementBehaviour.Alignment == Lodis.GridScripts.GridAlignment.RIGHT)
    //    {
    //        _opponent = BlackBoardBehaviour.Instance.Player1;
    //        return BlackBoardBehaviour.Instance.GetLHSActiveColliders().FindAll(collider => Vector3.Distance(collider.gameObject.transform.position, gameObject.transform.position) <= dummy.SenseRadius);
    //    }

    //    return new List<HitColliderBehaviour>();
    //}

    /// <summary>
    /// Gets a list of physics components from all attacks in range
    /// </summary>
    /// <returns></returns>
    private bool CheckIfProjectilesWillHit()
    {

        for (int i = 0; i < _dummy.GetAttacksInRange().Count; i++)
        {
            GridPhysicsBehaviour physics = _dummy.GetAttacksInRange()[i].GetComponentInParent<GridPhysicsBehaviour>();

            if (physics == null) continue;

            Vector3 direction = (physics.transform.position - _dummy.transform.position).normalized;
            float dotProduct = Vector3.Dot(direction, physics.LastVelocity.normalized);

            if (Mathf.Abs(dotProduct) >= 0.8f)
                return true;
        }

        return false;
    }


    /// <summary>
    /// Considered unsafe if hit boxes are in range, in the tumbling state,  or an attack has been started on the same row
    /// </summary>
    /// <returns></returns>
    public override bool Check()
    {
        List<HitColliderBehaviour> attacks = _dummy.GetAttacksInRange();

        if (_dummy.Executor.blackboard.boolParams[3] == true)
            return true;

        if (attacks.Count > 0)
            return false;

        string opponentState = _dummy.PlayerID == BlackBoardBehaviour.Instance.Player1ID ? BlackBoardBehaviour.Instance.Player2State : BlackBoardBehaviour.Instance.Player1State;

        //if (_dummy.OpponentMove.Position.y == _dummy.AIMovement.MovementBehaviour.Position.y && opponentState == "Attack")
        //    return false;

        return true;
    }
}
