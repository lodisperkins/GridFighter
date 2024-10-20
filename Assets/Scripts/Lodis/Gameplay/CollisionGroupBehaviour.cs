using FixedPoints;
using Lodis.Gameplay;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// Manages a collection of colliders that are for the same object. Useful for when only one collider in a group should hit.
/// </summary>
public class CollisionGroupBehaviour : SimulationBehaviour
{
    [SerializeField] private ColliderBehaviour[] _colliders;
    [SerializeField] private string _groupName;
    [SerializeField] private bool _isMultiHit;
    [SerializeField] private bool _despawnAfterTimeLimit;
    [SerializeField] private UnityEvent _onOverlapBegin;
    [SerializeField] private UnityEvent _onHitBegin;

    [ShowIf("_despawnAfterTimeLimit")]
    [SerializeField] private Fixed32 _despawnTime;
    [ShowIf("_despawnAfterTimeLimit")]
    [SerializeField] private EntityDataBehaviour _rootEntity;

    private bool _collisionResolved;

    public bool CollisionResolved { get => _collisionResolved; }

    public override void Deserialize(BinaryReader br)
    {

    }

    public override void Serialize(BinaryWriter bw)
    {

    }

    public void TrySetCollisionFinish()
    {
        _collisionResolved = !_isMultiHit;
    }

    public override void Init()
    {
        base.Init();

        foreach (var collider in _colliders)
        {
            collider.GroupManager = this;
            collider.EntityCollider.OnOverlapEnter += c => _onOverlapBegin?.Invoke();
            collider.EntityCollider.OnCollisionEnter += c => _onHitBegin?.Invoke();
        }
    }

    public override void Begin()
    {
        base.Begin();

        foreach (var collider in _colliders)
        {
            collider.Entity = Entity;
            Entity.Data.AddComponent(collider);
        }

        if (_isMultiHit)
        {
            return;
        }

        _collisionResolved = false;

        if (_despawnAfterTimeLimit)
        {
            FixedPointTimer.StartNewTimedAction(Entity.RemoveFromGame, _despawnTime);
        }
    }
}
