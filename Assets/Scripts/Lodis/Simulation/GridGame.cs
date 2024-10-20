using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGGPO;
using SharedGame;
using Unity.Collections;
using System.IO;
using Lodis.Input;
using static EntityData;
using FixedPoints;
using Types;
using UnityEngine.InputSystem;
using Lodis.ScriptableObjects;
using System.Runtime.Remoting.Messaging;
using Lodis.Gameplay;
using UnityEngine.InputSystem.HID;
using System.Linq;


public class TagSelectorAttribute : PropertyAttribute
{
    public bool UseDefaultTagFieldDrawer = true;
}

public struct GridGame : IGame
{
    private static List<EntityData> _activeEntities = new();
    private static readonly List<EntityData> _activePhysicsEntities = new();

    private static List<EntityData> _entitiesToRemove = new();
    private static List<EntityData> _entitiesToDestory = new();
    private static List<EntityData> _physicsEntitiesToRemove = new();

    //A dictionary of entity pairs that determines whether or not they collide. Used to ignore specific entities instead of layers.
    private static readonly Dictionary<(EntityData, EntityData), bool> _collisionPairs = new();

    private static long _p1Inputs;
    private static long _p2Inputs;

    /// <summary>
    /// The timestep in which the rollback simulation updates.
    /// The decimal value is 0.01667.
    /// </summary>
    public static Fixed32 FixedTimeStep = new Fixed32(1092);
    public static Fixed32 TimeScale = 1;
    /// <summary>
    /// The amount of time that has passed since the simulation began.
    /// </summary>
    public static Fixed32 Time;

    public delegate void InputPollCallback(int id);
    public delegate void InputProcessCallback(int id, long inputs);
    public delegate void SerializationCallback(BinaryWriter writer);
    public delegate void DeserializationCallback(BinaryReader reader);

    public static event InputPollCallback OnPollInput;
    public static event InputProcessCallback OnProcessInput;
    public static event SerializationCallback OnSerialization;
    public static event DeserializationCallback OnDeserialization;
    public static event EntityUpdateEvent OnSimulationUpdate;

    public int Framenumber { get; private set; }

    public readonly int Checksum => GetHashCode();

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Framenumber);

        for (int i = 0; i < _activeEntities.Count; ++i)
        {
            _activeEntities[i].Serialize(bw);
        }
    }

    public void Deserialize(BinaryReader br)
    {
        Framenumber = br.ReadInt32();

        for (int i = 0; i < _activeEntities.Count; ++i)
        {
            _activeEntities[i].Deserialize(br);
        }
    }

    public NativeArray<byte> ToBytes()
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                Serialize(writer);
            }
            return new NativeArray<byte>(memoryStream.ToArray(), Allocator.Persistent);
        }
    }

    public static void SetPlayerInput(IntVariable playerID, long inputs)
    {
        if (playerID == 1)
            _p1Inputs = inputs;
        else if (playerID == 2)
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
    }

    public void LogInfo(string filename)
    {
    }

    public long ReadInputs(int controllerId)
    {
        controllerId++;

        if (OnPollInput == null)
            return 0;

        OnPollInput?.Invoke(controllerId);

        long inputs = controllerId == 1 ? _p1Inputs : _p2Inputs;

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
        entityData.Transform.Parent = parent.Transform;

        //Create visual for entity.
        GameObject unityObject = new GameObject(entityData.Name);
        unityObject.transform.position = position;
        unityObject.transform.localScale = scale;
        unityObject.transform.SetParent(parent.UnityObject.transform);

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
    public static EntityDataBehaviour SpawnEntity(EntityData parent = null)
    {
        //Create new entity.
        EntityData entityData = new EntityData();
        entityData.Transform.Parent = parent.Transform;

        //Create visual for entity.
        GameObject unityObject = new GameObject(entityData.Name);
        unityObject.transform.SetParent(parent.UnityObject.transform);

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
        if (_activeEntities.Contains(entity))
        {
            Debug.LogWarning("Tried adding entity that was already in the game simulation. Entity was " + entity.Name);
            return;
        }

        _activeEntities.Add(entity);


        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            AddEntityToGame(entity.Transform.GetChild(i).Entity);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            _activePhysicsEntities.Add(entity);
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
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation.
    /// Doesn't remove from unity scene.
    /// </summary>
    public static void RemoveEntityFromGame(EntityData entity)
    {
        _entitiesToRemove.Add(entity);
        entity.End();

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).Entity;
            _entitiesToRemove.Add(child);
            child.End();

            if (child.Colliders?.Length > 0)
                _physicsEntitiesToRemove.Add(child);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            _physicsEntitiesToRemove.Add(entity);
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation.
    /// Doesn't remove from unity scene.
    /// </summary>
    public static void RemoveEntityFromGame(EntityData entity, bool destroy)
    {
        _entitiesToRemove.Add(entity);
        entity.End();

        if (destroy)
            _entitiesToDestory.Add(entity);

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).Entity;
            _entitiesToRemove.Add(child);
            child.End();

            if (child.Colliders?.Length > 0)
                _physicsEntitiesToRemove.Add(child);

            if (destroy)
                _entitiesToDestory.Add(child);

        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            _physicsEntitiesToRemove.Add(entity);
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

    public void Update(long[] inputs, int disconnectFlags)
    {
        Time += FixedTimeStep;
        OnSimulationUpdate?.Invoke(FixedTimeStep);

        //Remove all unwanted entities
        for (int i = 0; i < _entitiesToRemove.Count; i++)
        {
            _activeEntities.Remove(_entitiesToRemove[i]);
        }

        for (int i = 0; i < _entitiesToDestory.Count; i++)
        {
            MonoBehaviour.Destroy(_entitiesToDestory[i].UnityObject);
        }

        

        _entitiesToRemove.Clear();


        //Remove all unwanted physics entities
        for (int i = 0; i < _physicsEntitiesToRemove.Count; i++)
        {
            _activePhysicsEntities.Remove(_physicsEntitiesToRemove[i]);
        }

        _physicsEntitiesToRemove.Clear();

        //Component update
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            if (_entitiesToRemove.Contains(_activeEntities[i]))
                continue;

            if (!_activeEntities[i].Active)
                _activeEntities[i].Begin();

            _activeEntities[i].Tick(FixedTimeStep);
        }

        //Input update
        InputSystem.Update();
        OnProcessInput?.Invoke(1, inputs[0]);
        OnProcessInput?.Invoke(2, inputs[1]);

        //Timer update
        for (int i = 0; i < FixedPointTimer.Actions.Count; i++)
        {
            FixedPointTimer.Actions[i].TryPerformAction();
        }

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

    }
}
