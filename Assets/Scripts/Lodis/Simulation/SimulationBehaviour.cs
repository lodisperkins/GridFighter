using FixedPoints;
using System.IO;
using UnityEngine;
using Types;
using UnityEngine.Events;
using Assets.Scripts.Lodis.Simulation;
using System.Text;
using System;

/// <summary>
/// A generic class used to represent components that should perform logic in line with the rollback simulation.
/// Also contains logic for serializing and deserializing the component data.
/// </summary>
public abstract class SimulationBehaviour : MonoBehaviour, ISerializedListObject
{
    [Tooltip("Called when the game state is saved.")]
    [SerializeField] private UnityEvent _onSerialize;
    [Tooltip("Called when the game state is loaded.")]
    [SerializeField] private UnityEvent _onDeserialize;

    private EntityDataBehaviour _entity;

    /// <summary>
    /// The unity component that stores a reference to the rollback simulation entity.
    /// </summary>
    public EntityDataBehaviour Entity { get => _entity;  set => _entity = value; }

    /// <summary>
    /// Determines if this component should be updated during the rollback simulation's tick phase.
    /// </summary>
    public bool TickEnabled 
    {
        get;
        set;
    } = true;

    /// <summary>
    /// The fixed point transform belonging to the rollback simulation entity.
    /// </summary>
    public FTransform FixedTransform { get => _entity.Data.Transform; }
    public int FrameAddedToSerializedList { get; set; }

    public abstract string LogName { get; }

    public string ListDisplayName => LogName;

    /// <summary>
    /// Called when this component is added to an entity.
    /// </summary>
    public virtual void Init() { }

    public void OnSerialize(BinaryWriter bw)
    {
        Serialize(bw);
        _onSerialize?.Invoke();
    }

    public void OnDeserialize(BinaryReader br)
    {
        Deserialize(br);
        _onDeserialize?.Invoke();
    }
    protected abstract string[] GetLogItems();

    /// <summary>
    /// Writes information about this component to the provided StringBuilder for debugging purposes.
    /// </summary>
    public void OnLogGameState(StringBuilder sb)
    {
        sb.AppendLine($"            {LogName}");
        //byte[] serializedPayload = GetSerializedPayload();
        //sb.AppendLine($"                  {LogName} Checksum: {CalcFletcher32(serializedPayload)}");
        //sb.AppendLine($"                  Serialized Payload: {BitConverter.ToString(serializedPayload)}");

        string[] logItems = GetLogItems();

        if (logItems == null || logItems.Length == 0)
            return;

        for (int i = 0; i < logItems.Length; i++)
        {
            sb.AppendLine($"                  {logItems[i]}");
        }
    }

    public ListEvent OnAddedToList { get; set; }
    public ListEvent OnRemovedFromList { get; set; }
    public int FrameRemoved { get; set; }

    /// <summary>
    /// Handles data that is saved and sent across the network.
    /// </summary>
    public abstract void Serialize(BinaryWriter bw);

    /// <summary>
    /// Handles data that is loaded when a rollback happens.
    /// </summary>
    public abstract void Deserialize(BinaryReader br);

    /// <summary>
    /// Serializes this component into a temporary buffer and hashes the bytes so
    /// component checksums stay aligned with the exact rollback payload.
    /// </summary>
    protected int CalculateChecksum()
    {
        return CalcFletcher32(GetSerializedPayload());
    }

    /// <summary>
    /// Serializes the exact rollback payload for this component so debug logging and
    /// checksums can both inspect the same byte representation the save state uses.
    /// </summary>
    private byte[] GetSerializedPayload()
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            Serialize(writer);
            return memoryStream.ToArray();
        }
    }

    /// <summary>
    /// Produces a lightweight deterministic fingerprint of serialized component data.
    /// Fletcher-32 is inexpensive enough for sync diagnostics while still making it
    /// easy to compare individual components when a rollback mismatch is detected.
    /// </summary>
    private static int CalcFletcher32(byte[] data)
    {
        uint sum1 = 0;
        uint sum2 = 0;

        for (int i = 0; i < data.Length; ++i)
        {
            sum1 = (sum1 + data[i]) % 0xffff;
            sum2 = (sum2 + sum1) % 0xffff;
        }

        return unchecked((int)((sum2 << 16) | sum1));
    }

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

    protected virtual void Awake()
    {
        SetEntity();
    }
    
    public void SetEntity()
    {
        if (Entity)
            return;

        Entity = GetComponent<EntityDataBehaviour>();

        if (!Entity)
            Entity = GetComponentInParent<EntityDataBehaviour>();

        if (!Entity)
            Entity = GetComponentInChildren<EntityDataBehaviour>();
    }

    /// <summary>
    /// Called when entity is added to the scene. Called at the same time as Unity Awake
    /// </summary>
    public virtual void Begin() {}

    /// <summary>
    /// Called when the rollback simulation decides to update.
    /// </summary>
    /// <param name="dt">The fixed time step of the rollback simulations update.</param>
    public virtual void Tick(Fixed32 dt) { }

    /// <summary>
    /// Called after the main update loop.
    /// </summary>
    /// <param name="dt">The fixed time step of the rollback simulations update.</param>
    public virtual void LateTick(Fixed32 dt) { }

    /// <summary>
    /// Called when the entity is removed from the scene.
    /// </summary>
    public virtual void End() {}

    private void OnDestroy()
    {
        Entity?.Data.RemoveComponent(this);
    }

    public bool CheckIfCanBeAddedToList()
    {
        return true;
    }
}
