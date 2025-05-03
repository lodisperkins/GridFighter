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
using Types;

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
        List<HitColliderBehaviour> attacksInRange = _dummy.GetAttacksInRange();

        for (int i = 0; i < attacksInRange.Count; i++)
        {
            GridPhysicsBehaviour physics = attacksInRange[i].GetComponentInParent<GridPhysicsBehaviour>();

            if (physics == null) continue;

            FVector3 direction = (physics.FixedTransform.WorldPosition - _dummy.FixedTransform.WorldPosition).GetNormalized();
            Fixed32 dotProduct = FVector3.Dot(direction, physics.Velocity.GetNormalized());

            //0.8
            if (Fixed32.Abs(dotProduct) >= new Fixed32(52428) || physics.GetGridPosition() == _dummy.AIMovement.MovementBehaviour.Position || attacksInRange[i].CheckInCollisionRange(_dummy.AIMovement.MovementBehaviour.Position))
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
        if (CheckIfProjectilesWillHit() || _dummy.Knockback.CurrentAirState == AirState.TUMBLING)
            return false;

        return true;
    }
}
