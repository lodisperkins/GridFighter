using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedGame;
using Unity.Collections;
using System.IO;
using static EntityData;
using FixedPoints;
using Types;
using UnityEngine.InputSystem;
using Lodis.ScriptableObjects;
using Lodis.Gameplay;
using Assets.Scripts.Lodis.Simulation;
using System.Text;


public class TagSelectorAttribute : PropertyAttribute
{
    public bool UseDefaultTagFieldDrawer = true;
}

public struct GridGame : IGame
{
    private const string SerializeDebugLogDirectory = "serialize_logs";
    private static readonly List<EntityData> _activePhysicsEntities = new();

    private static List<EntityData> _entitiesToRemove = new();
    private static List<EntityData> _entitiesToDestroy = new();
    private static List<EntityData> _physicsEntitiesToRemove = new();
    private static List<EntityData> _serializedEntities = new();
    private static SerializedListHandler<EntityData> _activeEntities = new("Entity List");

    //A dictionary of entity pairs that determines whether or not they collide. Used to ignore specific entities instead of layers.
    private static readonly Dictionary<(EntityData, EntityData), bool> _collisionPairs = new();

    private static long _p1Inputs;
    private static long _p2Inputs;
    

    /// <summary>
    /// The timestep in which the rollback simulation updates.
    /// The decimal value is 0.01667.
    /// </summary>
    public static Fixed32 FixedTimeStep = new Fixed32(1092);
    public static Fixed32 TimeScale
    {
        get;
        set;
    } = 1;
    public static bool IsPaused;
    public static bool IsResimulating { get; private set; }
    /// <summary>
    /// The amount of time that has passed since the simulation began.
    /// </summary>
    public static Fixed32 Time
    {
        get;
        private set;
    }

    /// <summary>
    /// The amount of time that has passed since the simulation began.
    /// </summary>
    public static Fixed32 UnscaledTime
    {
        get;
        private set;
    }

    public delegate void InputPollCallback(int id);
    public delegate void InputProcessCallback(int id, long inputs);
    public delegate void SerializationCallback(BinaryWriter writer);
    public delegate void DeserializationCallback(BinaryReader reader);
    public delegate void DebugCallback(StringBuilder stringBuilder);
    public delegate void ClearMemoryCallback();
    public delegate void ResimulationStartedCallback(int rollbackFrame, int targetFrame);
    public delegate void ResimulationCompleteCallback(int framesResimulated);

    public static event InputPollCallback OnPollInput;
    public static event InputProcessCallback OnProcessInput;
    public static event SerializationCallback OnSerialization;
    public static event DeserializationCallback OnDeserialization;
    public static event DebugCallback OnLogGameState;
    public static event SerializationCallback OnLateSerialization;
    public static event DeserializationCallback OnLateDeserialization;
    public static event ClearMemoryCallback OnClearMemory;
    public static event EntityUpdateEvent OnSimulationUpdate;
    /// <summary>
    /// Relays GGPO replay-start notifications from the runner into GridGame so
    /// gameplay-side systems can prepare for manual replay updates.
    /// Args:
    /// (int) The frame we deserialized to rollback to.
    /// (int) The frame we are going to resimulate forward to get back to.
    /// </summary>
    public static event ResimulationStartedCallback OnResimulationStarted;
    public static event ResimulationCompleteCallback OnResimulationComplete;

    public int Framenumber { get; private set; }

    public readonly int Checksum => 0;

    private static bool _hasSerialized;
    Fixed32 test { get; set; }

    static GridGame()
    {
        GGPORunner.OnResimulationStarted += RelayResimulationStarted;
        GGPORunner.OnResimulationComplete += RelayResimulationComplete;
    }

    public void Serialize(BinaryWriter bw)
    {
        //HandleRemovalOfMarkedEntities();
        //_serializedEntities.Clear();

        //for (int i = 0; i < _activeEntities.Count; ++i)
        //{
        //    _activeEntities[i].Serialize(bw);
        //    _serializedEntities.Add(_activeEntities[i]);
        //}
        //test.Serialize(bw);

        bw.Write(Framenumber);

        Time.Serialize(bw);
        UnscaledTime.Serialize(bw);
        TimeScale.Serialize(bw);

        FixedPointTimer.SerializeActions(bw);
        FixedLerp.SerializeActions(bw);

        OnSerialization?.Invoke(bw);


        _activeEntities.Serialize(bw);

        OnLateSerialization?.Invoke(bw);

        //AppendSerializeDebugLog();
        _hasSerialized = true;
    }

    public void Deserialize(BinaryReader br)
    {
        //_physicsEntitiesToRemove.Clear();
        //test.Deserialize(br);
        //Debug.Log($"Starting deserializing at position {br.BaseStream.Position}");
        //int num = br.ReadInt32();

        Framenumber = br.ReadInt32();

        Time = Time.Deserialize(br);
        UnscaledTime = UnscaledTime.Deserialize(br);
        TimeScale = TimeScale.Deserialize(br);

        FixedPointTimer.DeserializeActions(br);
        FixedLerp.DeserializeActions(br);

        OnDeserialization?.Invoke(br);

        _activeEntities.Deserialize(br);

        OnLateDeserialization?.Invoke(br);
    }



    // GGPO checksums are most useful when they reflect the exact rollback state.
    // To keep this aligned with save/load behavior, we serialize the same state
    // that ToBytes/FromBytes use and hash those bytes instead of relying on
    // GetHashCode, which would not meaningfully capture the simulation state.
    private readonly int CalculateChecksum()
    {
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            //Serialize(writer);
            //return CalcFletcher32(memoryStream.ToArray());

            return 0;
        }
    }

    /// <summary>
    /// Hashes the values GridGame writes directly in its own serialize path so the
    /// sync log can compare the top-level serialized state as one grouped payload.
    /// </summary>
    private int CalculateCoreStateChecksum()
    {
        return CalculateSectionChecksum(bw =>
        {
            OnSerialization?.Invoke(bw);


            Time.Serialize(bw);
            UnscaledTime.Serialize(bw);
            TimeScale.Serialize(bw);

            //_entityListHandler.Serialize(bw);
            FixedPointTimer.SerializeActions(bw);
            FixedLerp.SerializeActions(bw);

            OnLateSerialization?.Invoke(bw);

        });
    }

    /// <summary>
    /// Serializes a specific rollback section into a temporary buffer so the sync
    /// logs can report a checksum for that exact portion of GridGame state.
    /// </summary>
    private static int CalculateSectionChecksum(System.Action<BinaryWriter> serializeSection)
    {
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            serializeSection(writer);
            return CalcFletcher32(memoryStream.ToArray());
        }
    }

    // Fletcher-32 accumulates two running 16-bit sums over the serialized bytes
    // and packs them into a single 32-bit value. It is inexpensive, deterministic,
    // and good enough for sync-test mismatch detection where we want a stable
    // fingerprint of the current serialized game state.
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
    public NativeArray<byte> ToBytes()
    {
        //Allocates memory for a new array of bites that has the game state data and returns it.
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                // Write total size first if needed
                long startPos = writer.BaseStream.Position;

                // Do ALL writing in one pass
                Serialize(writer);

                // Verify end position if needed
                long endPos = writer.BaseStream.Position;
                //Debug.Log($"Wrote from {startPos} to {endPos}");
            }
            return new NativeArray<byte>(memoryStream.ToArray(), Allocator.Persistent);
        }
    }

    public static void SetPlayerInput(IntVariable playerID, long inputs)
    {
        if (playerID == 0)
            _p1Inputs = inputs;
        else if (playerID == 1)
            _p2Inputs = inputs;
    }

    public void FromBytes(NativeArray<byte> bytes)
    {
        using (var memoryStream = new MemoryStream(bytes.ToArray()))
        {
            using (var reader = new BinaryReader(memoryStream))
            {
                Deserialize(reader);
            }
        }
    }

    public void FreeBytes(NativeArray<byte> data)
    {
        //Frees up the memory used in the game state previously saved.
        if (data.IsCreated)
        {
            data.Dispose();
        }

        OnClearMemory?.Invoke();
    }  


    public void LogInfo(string filename)
    {
        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
        {
            using (var writer = new StreamWriter(stream))
            {
                WriteLogInfo(writer);
            }
        }
    }

    /// <summary>
    /// Appends a standalone per-serialize debug log entry so save-state generation
    /// can be inspected without relying on GGPO synctest log dumps.
    /// </summary>
    private void AppendSerializeDebugLog()
    {
        string directory = Path.Combine(Application.dataPath, "..", SerializeDebugLogDirectory);
        Directory.CreateDirectory(directory);

        string filename = Path.Combine(directory, $"gridgame-serialize-{Framenumber:D6}.log");
        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.WriteLine($"Serialize Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            WriteLogInfo(writer);
        }
    }

    /// <summary>
    /// Writes the current rollback game state in the same human-readable format used
    /// by both GGPO-triggered logs and the standalone serialize debug log.
    /// </summary>
    private void WriteLogInfo(TextWriter writer)
    {
        writer.WriteLine("GridGame Items");
        writer.WriteLine($"Frame number: {Framenumber}");
        writer.WriteLine($"Time: {Time.RawValue}");
        writer.WriteLine($"Unscaled Time: {UnscaledTime.RawValue}");
        writer.WriteLine($"Time Scale: {TimeScale.RawValue}");

        StringBuilder sb = new StringBuilder();
        _activeEntities.OnLogGameState(sb);
        FixedPointTimer.LogGameState(sb);
        FixedLerp.LogGameState(sb);

        OnLogGameState?.Invoke(sb);

        writer.WriteLine(sb.ToString());
    }

    public long ReadInputs(int controllerId)
    {
        if (OnPollInput == null)
            return 0;

        OnPollInput?.Invoke(controllerId);

        long inputs = controllerId == 0 ? _p1Inputs : _p2Inputs;

        return inputs;
    }

    /// <summary>
    /// Creates a new entity at the given position and adds it to the rollback simulation.
    /// </summary>
    /// <param name="position">The unity world position of the entity.</param>
    /// <returns>The entity that was created.</returns>
    public static EntityDataBehaviour SpawnEntity(FVector3 position)
    {
        //Create new entity.
        EntityData entityData = new EntityData();
        entityData.Transform.WorldPosition = position;

        //Create visual for entity.
        GameObject unityObject = new GameObject(entityData.Name);
        unityObject.transform.position = (Vector3)position;

        //Add the entity script to the visual.
        EntityDataBehaviour script = unityObject.AddComponent<EntityDataBehaviour>();
        script.Data = entityData;

        entityData.UnityObject = unityObject;

        //Adding the entity to the rollback simulattion.
        AddEntityToGame(entityData);

        return script;
    }

    /// <summary>
    /// Creates a new entity at the given position and adds it to the rollback simulation.
    /// </summary>
    /// <param name="position">The unity world position of the entity.</param>
    /// <returns>The entity that was created.</returns>
    public static EntityData SpawnEntity(Vector3 position, Vector3 scale)
    {
        //Create new entity.
        EntityData entityData = new EntityData();
        entityData.Transform.WorldPosition = (FVector3)position;
        entityData.Transform.WorldScale = (FVector3)scale;

        //Create visual for entity.
        GameObject unityObject = new GameObject(entityData.Name);
        unityObject.transform.position = position;
        unityObject.transform.localScale = scale;

        //Add the entity script to the visual.
        EntityDataBehaviour script = unityObject.AddComponent<EntityDataBehaviour>();
        script.Data = entityData;

        entityData.UnityObject = unityObject;

        //Adding the entity to the rollback simulattion.
        AddEntityToGame(entityData);

        return entityData;
    }

    /// <summary>
    /// Creates a new entity at the given position and adds it to the rollback simulation.
    /// </summary>
    /// <param name="position">The unity world position of the entity.</param>
    /// <returns>The entity that was created.</returns>
    public static EntityData SpawnEntity(Vector3 position, Vector3 scale, FQuaternion rotation, EntityData parent = null)
    {
        //Create new entity.
        EntityData entityData = new EntityData();
        entityData.Transform.WorldPosition = (FVector3)position;
        entityData.Transform.WorldScale = (FVector3)scale;
        entityData.Transform.WorldRotation = rotation;
        entityData.Transform.Parent = parent?.Transform;

        //Create visual for entity.
        GameObject unityObject = new GameObject(entityData.Name);
        unityObject.transform.position = position;
        unityObject.transform.localScale = scale;
        unityObject.transform.SetParent(parent?.UnityObject.transform);

        //Add the entity script to the visual.
        EntityDataBehaviour script = unityObject.AddComponent<EntityDataBehaviour>();
        script.Data = entityData;

        entityData.UnityObject = unityObject;

        //Adding the entity to the rollback simulattion.
        AddEntityToGame(entityData);

        return entityData;
    }

    /// <summary>
    /// Creates a new entity at the given position and adds it to the rollback simulation.
    /// </summary>
    /// <param name="position">The unity world position of the entity.</param>
    /// <returns>The entity that was created.</returns>
    public static T SpawnEntity<T>(T component, FVector3 position, FVector3 scale, FQuaternion rotation, EntityData parent = null) where T : SimulationBehaviour
    {
        //Create new entity.
        EntityData entityData = new EntityData();
        entityData.Transform.WorldPosition = position;
        entityData.Transform.WorldScale = scale;
        entityData.Transform.WorldRotation = rotation;
        entityData.Transform.Parent = parent?.Transform;

        //Create visual for entity.
        T comp = UnityEngine.Object.Instantiate(component);
        GameObject unityObject = comp.gameObject;
        unityObject.name = component.gameObject.name + "(Clone)";
        unityObject.transform.position = (Vector3)position;
        unityObject.transform.localScale = (Vector3)scale;
        unityObject.transform.SetParent(parent?.UnityObject.transform);

        //Add the entity script to the visual.

        EntityDataBehaviour entityDataBehaviour = null;

        if (!unityObject.TryGetComponent<EntityDataBehaviour>(out entityDataBehaviour))
        {
            entityDataBehaviour = unityObject.AddComponent<EntityDataBehaviour>();
        }

        entityDataBehaviour.Data = entityData;

        entityData.UnityObject = unityObject;
        comp.Entity = entityDataBehaviour;

        //Adding the entity to the rollback simulattion.
        AddEntityToGame(entityData);

        return comp;
    }

    /// <summary>
    /// Creates a new entity at the given position and adds it to the rollback simulation.
    /// </summary>
    /// <param name="position">The unity world position of the entity.</param>
    /// <returns>The entity that was created.</returns>
    public static EntityDataBehaviour SpawnEntity(EntityData parent = null)
    {
        //Create new entity.
        EntityData entityData = new EntityData();
        entityData.Transform.Parent = parent?.Transform;

        //Create visual for entity.
        GameObject unityObject = new GameObject(entityData.Name);
        unityObject.transform.SetParent(parent?.UnityObject.transform);

        //Add the entity script to the visual.
        EntityDataBehaviour script = unityObject.AddComponent<EntityDataBehaviour>();
        script.Data = entityData;

        entityData.UnityObject = unityObject;

        //Adding the entity to the rollback simulattion.
        AddEntityToGame(entityData);

        return script;
    }

    /// <summary>
    /// Adds the entity and all of its children to the rollback simulation.
    /// Doesn't add to unity scene.
    /// </summary>
    public static void AddEntityToGame(EntityData entity)
    {
        //If this entity was marked for removal, unmark it.
        if (_entitiesToRemove.Contains(entity))
        {
            _entitiesToRemove.Remove(entity);
            entity.Begin();

            for (int i = 0; i < entity.Transform.ChildCount; i++)
            {
                EntityData child = entity.Transform.GetChild(i).EntityData;
                _entitiesToRemove.Remove(child);
                child.Begin();

                if (child.Colliders?.Length > 0)
                    _physicsEntitiesToRemove.Remove(child);

                child.FrameAdded = GridGameManager.FrameNumber;
            }

            entity.FrameAdded = GridGameManager.FrameNumber;
        }

        if (_activeEntities.Contains(entity))
        {
            //Debug.LogWarning("Tried adding entity that was already in the game simulation. Entity was " + entity.Name);
            return;
        }

        _activeEntities.Add(entity);
        entity.FrameAdded = GridGameManager.FrameNumber;

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            AddEntityToGame(entity.Transform.GetChild(i).EntityData);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            AddPhysicsEntity(entity);


        //Debug.Log($"Added {entity.Name}");
    }

    /// <summary>
    /// Adds the entity and all of its children to the rollback simulation without calling begin or setting frame added.
    /// Doesn't add to unity scene. Mainly used to add entities back into the simulation on rollback.
    /// </summary>
    public static void AddEntityToGameWithoutEvents(EntityData entity)
    {
        //If this entity was marked for removal, unmark it.
        if (_entitiesToRemove.Contains(entity))
        {
            _entitiesToRemove.Remove(entity);

            for (int i = 0; i < entity.Transform.ChildCount; i++)
            {
                EntityData child = entity.Transform.GetChild(i).EntityData;
                _entitiesToRemove.Remove(child);

                if (child.Colliders?.Length > 0)
                    _physicsEntitiesToRemove.Remove(child);

                child.FrameAdded = GridGameManager.FrameNumber;
            }

            entity.FrameAdded = GridGameManager.FrameNumber;
        }

        if (_activeEntities.Contains(entity))
        {
            //Debug.LogWarning("Tried adding entity that was already in the game simulation. Entity was " + entity.Name);
            return;
        }

        _activeEntities.Add(entity);

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            AddEntityToGame(entity.Transform.GetChild(i).EntityData);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            AddPhysicsEntity(entity);

        //Debug.Log($"Added {entity.Name}");
    }

    public static void AddPhysicsEntity(EntityData entity)
    {
        if (!_activeEntities.Contains(entity))
        {
            throw new System.Exception("Cannot add a physics entity that has not been added to the game. Entity was " + entity.Name);
        }

        if (_activePhysicsEntities.Contains(entity) || entity.Colliders?.Length == 0)
            return;

        _activePhysicsEntities.Add(entity);
    }

    public static void RemovePhysicsEntity(EntityData entity)
    {
        _physicsEntitiesToRemove.Add(entity);
        CleanColliderArrays();
    }

    public static void RemovePhysicsEntityImmediate(EntityData entity)
    {
        _physicsEntitiesToRemove.Remove(entity);
        _activePhysicsEntities.Remove(entity);
        CleanColliderArrays();
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation.
    /// Doesn't remove from unity scene.
    /// </summary>
    public static void RemoveEntityFromGame(EntityData entity)
    {
        if (_entitiesToRemove.Contains(entity) || !_activeEntities.Contains(entity))
        {
            //Debug.LogWarning("Tried removing entity that was already marked for removal from the game simulation. Entity was " + entity.Name);
            return;
        }

        _entitiesToRemove.Add(entity);
        entity.End();
        entity.FrameRemoved = GridGameManager.FrameNumber;

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).EntityData;
            _entitiesToRemove.Add(child);
            child.End();

            if (child.Colliders?.Length > 0)
                _physicsEntitiesToRemove.Add(child);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            RemovePhysicsEntity(entity);
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation immediately. Doesnt handle things cleanly so avoid using normally.
    /// </summary>
    public static void RemoveEntityFromGameImmediate(EntityData entity)
    {
        if (_entitiesToRemove.Contains(entity))
        {
            _entitiesToRemove.Remove(entity);
        }

        _activeEntities.Remove(entity);

        entity.End();
        entity.FrameRemoved = GridGameManager.FrameNumber;

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).EntityData;

            if (_entitiesToRemove.Contains(child))
                _entitiesToRemove.Remove(child);

            _activeEntities.Remove(child);
            child.End();

            if (child.Colliders?.Length > 0)
                RemovePhysicsEntityImmediate(child);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            RemovePhysicsEntityImmediate(entity);
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation immediately. Doesnt handle things cleanly so avoid using normally.
    /// Doesnt call events like End.
    /// </summary>
    public static void RemoveEntityFromGameImmediateWithoutEvents(EntityData entity)
    {
        if (_entitiesToRemove.Contains(entity))
        {
            _entitiesToRemove.Remove(entity);
        }

        _activeEntities.Remove(entity);

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).EntityData;

            if (_entitiesToRemove.Contains(child))
                _entitiesToRemove.Remove(child);

            _activeEntities.Remove(child);

            if (child.Colliders?.Length > 0)
                RemovePhysicsEntityImmediate(child);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            RemovePhysicsEntityImmediate(entity);
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation.
    /// Doesn't remove from unity scene.
    /// </summary>
    public static void RemoveEntityFromGame(EntityData entity, bool destroy)
    {
        if (_entitiesToRemove.Contains(entity))
        {
            //Debug.LogWarning("Tried removing entity that was already marked for removal from the game simulation. Entity was " + entity.Name);
            return;
        }
        _entitiesToRemove.Add(entity);
        entity.End();

        if (destroy)
            _entitiesToDestroy.Add(entity);

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).EntityData;
            _entitiesToRemove.Add(child);
            child.End();

            if (child.Colliders?.Length > 0)
                _physicsEntitiesToRemove.Add(child);

            if (destroy)
                _entitiesToDestroy.Add(child);

        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            RemovePhysicsEntity(entity);
    }

    public static void IgnoreCollision(EntityData entity1, EntityData entity2, bool ignore = true)
    {
        if (_collisionPairs.ContainsKey((entity1, entity2)))
        {
            _collisionPairs[(entity1,entity2)] = ignore;
            return;
        }

        _collisionPairs.Add((entity1, entity2), ignore);
    }


    private void HandleRemovalOfMarkedEntities()
    {
        //Remove all unwanted entities
        for (int i = 0; i < _entitiesToRemove.Count; i++)
        {
            _activeEntities.Remove(_entitiesToRemove[i]);
        }

        for (int i = 0; i < _entitiesToDestroy.Count; i++)
        {
            MonoBehaviour.Destroy(_entitiesToDestroy[i].UnityObject);
        }

        _entitiesToRemove.Clear();


        //Remove all unwanted physics entities
        for (int i = 0; i < _physicsEntitiesToRemove.Count; i++)
        {
            _activePhysicsEntities.Remove(_physicsEntitiesToRemove[i]);
        }

        _physicsEntitiesToRemove.Clear();
    }

    public static void CleanColliderArrays()
    {
        foreach (var entity in _activePhysicsEntities)
        {
            foreach (var collider in entity.Colliders)
            {
                collider.CleanCollisionList();
            }
        }
    }

    public void Update(long[] inputs, int disconnectFlags)
    {
        if (IsPaused)
        {
            UpdateInput(inputs);
            return;
        }

        test += FixedTimeStep;

        Time += FixedTimeStep * TimeScale;
        UnscaledTime += FixedTimeStep;

        OnSimulationUpdate?.Invoke(FixedTimeStep);

        Framenumber++;

        HandleRemovalOfMarkedEntities();

        UpdateInput(inputs);

        //Component update
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            if (_entitiesToRemove.Contains(_activeEntities[i]))
                continue;

            if (!_activeEntities[i].Active)
                _activeEntities[i].Begin();

            _activeEntities[i].Tick(FixedTimeStep);
        }


        //Timer update
        for (int i = 0; i < FixedPointTimer.Actions.Count; i++)
        {
            FixedPointTimer.Actions[i].TryPerformAction();
        }

        //Debug.Log($"Fixed timer count is {FixedPointTimer.Actions.Count}");
        //Collision update

        //This loop ensures that we aren't checking collisions with the same colliders by have the second loop start where the first one left off.
        for (int row = 0; row < _activePhysicsEntities.Count; row++)
        {
            for (int column = row + 1; column < _activePhysicsEntities.Count; column++)
            {
                //Check if these entities should ignore each other.
                bool shouldIgnore;

                if (_collisionPairs.TryGetValue((_activePhysicsEntities[row], _activePhysicsEntities[column]), out shouldIgnore))
                {
                    if (shouldIgnore)
                        continue;
                }

                //Cache current entities
                EntityData entity1 = _activePhysicsEntities[row];
                EntityData entity2 = _activePhysicsEntities[column];

                if (entity1.Colliders == null || entity2.Colliders == null || !entity1.Active || !entity2.Active)
                {
                    continue;
                }

                //Check collision between all possible colliders
                for (int i = 0; i < entity1.Colliders.Length; i++)
                {
                    for (int j = 0; j < entity2.Colliders.Length; j++)
                    {
                        GridCollider collider1 = entity1.Colliders[i];
                        GridCollider collider2 = entity2.Colliders[j];

                        //If they aren't on the same row there's no point in checking collision.
                        if ((collider1 == null || collider2 == null))
                            continue;

                        //Check the next thing if a collision wasn't found.
                        collider1.CheckCollision(collider2);
                    }
                }
            }

        }


        //Component late update
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            _activeEntities[i].LateTick(FixedTimeStep);
        }

        //Debug.Log($"Entity count is {_activeEntities.Count}");
    }

    private static int _lastInputUpdateFrame = -1;

    private static void UpdateInput(long[] inputs)
    {
        //Input update - only once per Unity frame, not per simulation frame
        if (GridGameManager.Instance.inputEnabled && UnityEngine.Time.frameCount != _lastInputUpdateFrame)
        {
            InputSystem.Update();
            _lastInputUpdateFrame = UnityEngine.Time.frameCount;
        }

        OnProcessInput?.Invoke(0, inputs[0]);
        OnProcessInput?.Invoke(1, inputs[1]);
    }

    public static void OnSceneChange()
    {
        _hasSerialized = false;

        foreach (var entity in _activeEntities)
        {
            entity.DestroyComponents();
        }

        _activeEntities.Destroy(true);
        _activePhysicsEntities.Clear();
        FixedPointTimer.Actions.Destroy(true);
        FixedLerp.Actions.Destroy(true);
    }

    /// <summary>
    /// Relays GGPO replay-start notifications from the runner into GridGame so
    /// gameplay-side systems can prepare for manual replay updates.
    /// </summary>
    /// <param name="rollbackFrame">The frame we deserialized to rollback to.</param>
    /// <param name="targetFrame">The frame we are going to resimulate forward to get back to.</param>
    private static void RelayResimulationStarted(int rollbackFrame, int targetFrame)
    {
        IsResimulating = true;
        OnResimulationStarted?.Invoke(rollbackFrame, targetFrame);
    }

    /// <summary>
    /// Relays GGPO replay completion notifications from the runner into GridGame so
    /// gameplay-side systems can subscribe without depending directly on the runner.
    /// </summary>
    /// <param name="framesResimulated">How many simulation frames were replayed.</param>
    private static void RelayResimulationComplete(int framesResimulated)
    {
        IsResimulating = false;
        OnResimulationComplete?.Invoke(framesResimulated);
    }
}
