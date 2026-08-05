using FixedPoints;
using Lodis.Gameplay;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    //---
    private bool _collisionResolved;

    public bool CollisionResolved { get => _collisionResolved; }

    public override string LogName => "CollisionGroup";

    public override void Deserialize(BinaryReader br)
    {
        foreach (var collider in _colliders)
        {
            collider.Deserialize(br);
        }
    }

    public override void Serialize(BinaryWriter bw)
    {
        foreach (var collider in _colliders)
        {
            collider.Serialize(bw);
        }
    }

    /// <summary>
    /// Hashes the serialized collision-group data so group membership or collider
    /// ordering issues can be isolated during sync diagnostics.
    /// </summary>
    protected override string[] GetLogItems()
    {
        if (_colliders == null || _colliders.Length == 0)
        {
            return System.Array.Empty<string>();
        }

        string[] logItems = new string[_colliders.Length];

        for (int i = 0; i < _colliders.Length; i++)
        {
            ColliderBehaviour collider = _colliders[i];
            GridCollider entityCollider = collider != null ? collider.EntityCollider : null;

            logItems[i] = entityCollider == null
                ? $"Collider[{i}]: null"
                : $"Collider[{i}] Width={entityCollider.Width}, Height={entityCollider.Height}, PanelYOffset={entityCollider.PanelYOffset}";
        }

        return logItems;
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

        foreach (var collider in _colliders)
        {
            collider.TickEnabled = true;
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

    public void SetHitCollisionInfo(HitColliderData info, EntityDataBehaviour spawner)
    {
        foreach (var collider in _colliders)
        {
            HitColliderBehaviour hitCollider = collider as HitColliderBehaviour;

            if (hitCollider)
            {
                hitCollider.ColliderInfo = info;
                hitCollider.Spawner = spawner;
            }
        }
    }

    public override void End()
    {
        base.End();

        foreach (var collider in _colliders)
        {
            collider.TickEnabled = false;
        }
    }

}
