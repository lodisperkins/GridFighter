using FixedPoints;
using Lodis.Movement;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

public class FollowBehaviour : SimulationBehaviour
{
    [SerializeField] private EntityDataBehaviour _target;
    [SerializeField] private bool _snapToTarget;
    [SerializeField] private Fixed32 _speed;

    private GridPhysicsBehaviour _physics;

    public EntityDataBehaviour Target { get => _target; set => _target = value; }

    public override void Begin()
    {
        base.Begin();

        TryGetComponent<GridPhysicsBehaviour>(out _physics);
    }

    public override void Tick(Fixed32 dt)
    {
        base.Tick(dt);

        if (Target == null || !Target.Active)
            return;

        if (_snapToTarget)
        {
            FixedTransform.WorldPosition = Target.FixedTransform.WorldPosition;
        }
        else
        {
            FVector3 toTarget = Target.FixedTransform.WorldPosition - FixedTransform.WorldPosition;
            FVector3 velocity = toTarget.GetNormalized() * _speed * dt;

            // Follow the target with a fixed speed

            if (_physics)
                _physics.ApplyVelocityChange(velocity);
            else
                FixedTransform.WorldPosition += velocity;
        }
    }

    public override void Deserialize(BinaryReader br)
    {

    }

    public override void Serialize(BinaryWriter bw)
    {

    }
}
