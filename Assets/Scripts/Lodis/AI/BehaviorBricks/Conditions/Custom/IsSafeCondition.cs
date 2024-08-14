using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pada1.BBCore;           // Code attributes
using Pada1.BBCore.Framework; // ConditionBase
using BBUnity.Conditions;
using Lodis.Movement;
using Lodis.AI;
using Lodis.Gameplay;
using FixedPoints;

[Condition("CustomConditions/IsSafe")]
public class IsSafeCondition : GOCondition
{
    private GameObject _opponent = null;
    [InParam("Owner")]
    private AIControllerBehaviour _dummy;

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
            float dotProduct = FVector3.Dot((FVector3)direction, physics.Velocity.GetNormalized());

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
        List<HitColliderBehaviour> attacks = null;

        attacks = _dummy.GetAttacksInRange();

        if (attacks.Count > 0 || _dummy.Knockback.CurrentAirState == AirState.TUMBLING)
            return false;

        _dummy.Defense.DeactivateShield();

        return true;
    }
}
