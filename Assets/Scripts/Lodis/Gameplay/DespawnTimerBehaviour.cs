using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

public class DespawnTimerBehaviour : SimulationBehaviour
{
    [SerializeField] private Fixed32 _despawnTime;
    [SerializeField] private GameObject _despawnEffect;
    [SerializeField] private bool _destroyOnDespawn;

    private FixedTimeAction _timer;

    public override void Deserialize(BinaryReader br) { }

    public override void Serialize(BinaryWriter bw) { }

    private void OnEnable()
    {
        _timer = FixedPointTimer.StartNewTimedAction(OnDespawnTimerComplete, _despawnTime);
    }

    private void OnDespawnTimerComplete()
    {
        Instantiate(_despawnEffect, transform.position, Quaternion.identity);
        Entity.RemoveFromGame(_destroyOnDespawn);
    }

    private void OnDisable()
    {
        _timer.Stop();
    }
}
