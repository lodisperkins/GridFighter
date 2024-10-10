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


public class TagSelectorAttribute : PropertyAttribute
{
    public bool UseDefaultTagFieldDrawer = true;
}

public struct GridGame : IGame
{
    private static List<EntityData> ActiveEntities = new();
    private static readonly List<EntityData> ActivePhysicsEntities = new();

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

        for (int i = 0; i < ActiveEntities.Count; ++i)
        {
            ActiveEntities[i].Serialize(bw);
        }
    }

    public void Deserialize(BinaryReader br)
    {
        Framenumber = br.ReadInt32();

        for (int i = 0; i < ActiveEntities.Count; ++i)
        {
            ActiveEntities[i].Deserialize(br);
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
    public static EntityData SpawnEntity(FVector3 position)
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

        return entityData;
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
    public static EntityData SpawnEntity(EntityData parent = null)
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

        return entityData;
    }

    /// <summary>
    /// Adds the entity and all of its children to the rollback simulation.
    /// Doesn't add to unity scene.
    /// </summary>
    public static void AddEntityToGame(EntityData entity)
    {
        if (ActiveEntities.Contains(entity))
        {
            Debug.LogWarning("Tried adding entity that was already in the game simulation. Entity was " + entity.Name);
            return;
        }

        ActiveEntities.Add(entity);


        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            AddEntityToGame(entity.Transform.GetChild(i).Entity);
        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            ActivePhysicsEntities.Add(entity);
    }

    public static void AddPhysicsEntity(EntityData entity)
    {
        if (!ActiveEntities.Contains(entity))
        {
            throw new System.Exception("Cannot add a physics entity that has not been added to the game. Entity was " + entity.Name);
        }

        if (ActivePhysicsEntities.Contains(entity) || entity.Colliders?.Length == 0)
            return;

        ActivePhysicsEntities.Add(entity);
    }

    public static void RemovePhysicsEntity(EntityData entity)
    {
        ActivePhysicsEntities.Remove(entity);
    }

    /// <summary>
    /// Removes the entity and all of its children from the rollback simulation.
    /// Doesn't remove from unity scene.
    /// </summary>
    public static void RemoveEntityFromGame(EntityData entity)
    {
        ActiveEntities.Remove(entity);
        entity.End();

        for (int i = 0; i < entity.Transform.ChildCount; i++)
        {
            EntityData child = entity.Transform.GetChild(i).Entity;
            ActiveEntities.Remove(child);
            child.End();

            if (child.Colliders?.Length > 0)
                ActivePhysicsEntities.Remove(child);

        }

        if (entity.Colliders?.Length > 0 || entity.HasComponent<ColliderBehaviour>())
            ActivePhysicsEntities.Remove(entity);
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

        //Component update
        for (int i = 0; i < ActiveEntities.Count; i++)
        {
            if (!ActiveEntities[i].Active)
                ActiveEntities[i].Begin();

            ActiveEntities[i].Tick(FixedTimeStep);
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
        for (int row = 0; row < ActivePhysicsEntities.Count; row++)
        {
            for (int column = row + 1; column < ActivePhysicsEntities.Count; column++)
            {
                //Check if these entities should ignore each other.
                bool shouldIgnore;

                if (_collisionPairs.TryGetValue((ActivePhysicsEntities[row], ActivePhysicsEntities[column]), out shouldIgnore))
                {
                    if (shouldIgnore)
                        continue;
                }

                //Cache current entities
                EntityData entity1 = ActivePhysicsEntities[row];
                EntityData entity2 = ActivePhysicsEntities[column];

                if (entity1.Colliders == null || entity2.Colliders == null) continue;

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
        for (int i = 0; i < ActiveEntities.Count; i++)
        {
            ActiveEntities[i].LateTick(FixedTimeStep);
        }

    }
}
