using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BBCore.Actions;
using BBUnity.Actions;
using Lodis.AI;
using Pada1.BBCore;

[Action("CustomAction/Attack")]
public class AttackAction : GOAction
{
    [InParam("Owner")]
    private AttackDummyBehaviour _dummy;

    public override void OnStart()
    {
        base.OnStart();
        
    }
}
