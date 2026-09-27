using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

public class DespawnTimerBehaviour : SimulationBehaviour
{

    #region Simulation Functions

    public override void Deserialize(Deserializer br) { }


    public override void Serialize(BinaryWriter bw) { }

    protected override string[] GetLogItems()
    {
        return null;
    }

    #endregion

    [SerializeField] private Fixed32 _despawnTime;
    [SerializeField] private GameObject _despawnEffect;
    [SerializeField] private bool _destroyOnDespawn;

    private FixedTimeAction _timer;

    public override string LogName => "DespawnTimer";



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

    /// <summary>
    /// Hashes the serialized despawn timer state, which is currently empty, so the
    /// component still exposes a consistent checksum surface.
    /// </summary>

}
