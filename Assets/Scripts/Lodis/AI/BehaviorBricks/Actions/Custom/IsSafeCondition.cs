using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pada1.BBCore;           // Code attributes
using Pada1.BBCore.Framework; // ConditionBase

[Condition("CustomConditions/IsSafe")]
public class IsSafeCondition : ConditionBase
{
    public override bool Check()
    {
        throw new System.NotImplementedException();
    }
}
