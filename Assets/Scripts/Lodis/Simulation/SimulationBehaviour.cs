using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using FixedPoints;
using Types;

/// <summary>
/// A generic class used to represent components that should perform logic in line with the rollback simulation.
/// Also contains logic for serializing and deserializing the component data.
/// </summary>
public abstract class SimulationBehaviour : MonoBehaviour
{
    private EntityDataBehaviour _entity;

    /// <summary>
    /// The unity component that stores a reference to the rollback simulation entity.
    /// </summary>
    public EntityDataBehaviour Entity { get => _entity;  set => _entity = value; }

    /// <summary>
    /// The fixed point transform belonging to the rollback simulation entity.
    /// </summary>
    public FTransform EntityTransform { get => _entity.Data.Transform; }

    /// <summary>
    /// Called when this component is added to an entity.
    /// </summary>
    public virtual void Init() { }

    /// <summary>
    /// Handles data that is saved and sent across the network.
    /// </summary>
    public abstract void Serialize(BinaryWriter bw);

    /// <summary>
    /// Handles data that is loaded when a rollback happens.
    /// </summary>
    public abstract void Deserialize(BinaryReader br);

    /// <summary>
    /// Called when this entity starts hitting another solid object.
    /// </summary>
    public virtual void OnHitEnter(Collision collision) { }

    /// <summary>
    /// Called when this entity is hitting another solid object.
    /// </summary>
    public virtual void OnHitStay(Collision collision) { }

    /// <summary>
    /// Called when this entity stops hitting another solid object.
    /// </summary>
    public virtual void OnHitExit(Collision collision) { }

    /// <summary>
    /// Called when this entity starts touching another.
    /// </summary>
    public virtual void OnOverlapEnter(Collision collision) { }

    /// <summary>
    /// Called when this entity touches another.
    /// </summary>
    public virtual void OnOverlapStay(Collision collision) { }

    /// <summary>
    /// Called when this entity stops touching another.
    /// </summary>
    public virtual void OnOverlapExit(Collision collision) { }

    private void Awake()
    {
        Entity = GetComponent<EntityDataBehaviour>();
    }

    /// <summary>
    /// Called when entity is added to the scene.
    /// </summary>
    public virtual void Begin() {}

    /// <summary>
    /// Called when the rollback simulation decides to update.
    /// </summary>
    /// <param name="dt">The fixed time step of the rollback simulations update.</param>
    public virtual void Tick(Fixed32 dt) { }

    /// <summary>
    /// Called when the entity is removed from the scene.
    /// </summary>
    public virtual void End() {}
}
