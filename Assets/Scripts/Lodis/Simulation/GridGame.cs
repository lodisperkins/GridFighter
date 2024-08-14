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


public struct GridGame : IGame
{
    private static List<EntityData> ActiveEntities = new List<EntityData>();

    //A dictionary of entity pairs that determines whether or not they collide. Used to ignore specific entities instead of layers.
    private static Dictionary<(EntityData, EntityData), bool> _collisionPairs;

    public static Fixed32 FixedTimeStep = 0.01667f;
    public static float TimeScale = 1f;

    public delegate long InputPollCallback(int id);
    public delegate void InputProcessCallback(int id, long inputs);
    public delegate void SerializationCallback(BinaryWriter writer);
    public delegate void DeserializationCallback(BinaryReader reader);

    public static event InputPollCallback OnPollInput;
    public static event InputProcessCallback OnProcessInput;
    public static event SerializationCallback OnSerialization;
    public static event DeserializationCallback OnDeserialization;
    public static event EntityUpdateEvent OnSimulationUpdate;

    public static BufferedInput P1BufferedAction;
    public static BufferedInput P2BufferedAction;

    public int Framenumber { get; private set; }

    public int Checksum => GetHashCode();

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Framenumber);
        bw.Write(ActiveEntities.Count);

        P1BufferedAction?.Serialize(bw);
        P2BufferedAction?.Serialize(bw);

        for (int i = 0; i < ActiveEntities.Count; ++i)
        {
            ActiveEntities[i].Serialize(bw);
        }
    }

    public void Deserialize(BinaryReader br)
    {
        Framenumber = br.ReadInt32();
        int length = br.ReadInt32();

        P1BufferedAction.Deserialize(br);
        P2BufferedAction?.Deserialize(br);  

        if (length != ActiveEntities.Count)
        {
            ActiveEntities = new List<EntityData>(length);
        }
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
        return (long)(OnPollInput?.Invoke(controllerId));
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
        entityData.Transform.Position = position;

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
        entityData.Transform.Position = (FVector3)position;
        entityData.Transform.Scale = (FVector3)scale;

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
        entityData.Transform.Position = (FVector3)position;
        entityData.Transform.Scale = (FVector3)scale;
        entityData.Transform.Rotation = rotation;
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

    public static void AddEntityToGame(EntityData entity)
    {
        if (ActiveEntities.Contains(entity))
        {
            Debug.LogError("Tried adding entity that was already in the game simulation. Entity was " + entity.Name);
            return;
        }

        ActiveEntities.Add(entity);
        entity.Begin();
    }

    public static void RemoveEntityFromGame(EntityData entity)
    {
        ActiveEntities.Remove(entity);
        entity.End();
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
        OnSimulationUpdate?.Invoke(FixedTimeStep);

        //Component update
        for (int i = 0; i < ActiveEntities.Count; i++)
        {
            ActiveEntities[i].Tick(FixedTimeStep);
        }

        //Input update
        InputSystem.Update();
        OnProcessInput?.Invoke(0, inputs[0]);
        OnProcessInput?.Invoke(1, inputs[1]);

        //Timer update
        for (int i = 0; i < FixedPointTimer.Actions.Count; i++)
        {
            FixedPointTimer.Actions[i].TryPerformAction();
        }

        //Collision update

        //This loop ensures that we aren't checking collisions with the same colliders by have the second loop start where the first one left off.
        for (int row = 0; row < ActiveEntities.Count; row++)
        {
            for (int column = row; column < ActiveEntities.Count; column++)
            {
                //Continue to prevent the object from colliding with itself.
                if (row == column)
                    continue;

                //Check if these entities should ignore each other.
                bool shouldIgnore;

                if (_collisionPairs.TryGetValue((ActiveEntities[row], ActiveEntities[column]), out shouldIgnore))
                {
                    if (shouldIgnore)
                        continue;
                }

                //Cache attached colliders
                Collision collisionData1;
                Collision collisionData2;
                GridCollider collider1 = ActiveEntities[row].Collider;
                GridCollider collider2 = ActiveEntities[column].Collider;

                //If they aren't on the same row there's no point in checking collision.
                if ((collider1 == null || collider2 == null) || collider1.PanelY != collider2.PanelY)
                    continue;

                //Check the next thing if a collision wasn't found.
                if (!collider1.CheckCollision(collider2, out collisionData1))
                {
                    continue;
                }

                //Flip the values for the other colliders data.
                collisionData2 = collisionData1;
                collisionData2.Normal = collisionData1.Normal * -1;
                collisionData2.Collider = collider1;
                collisionData2.Entity = ActiveEntities[row];

                //Handle the collision events based on whether or not the collision should treat the objects like solid surfaces.
                if (!collider1.Overlap && !collider2.Overlap)
                {
                    collider1.OwnerPhysicsComponent.ResolveCollision(collisionData1);

                    collider1.Owner.OnCollisionStay(collisionData1);
                    collider2.Owner.OnCollisionStay(collisionData2);
                }
                else
                {
                    collider1.Owner.OnOverlapStay(collisionData1);
                    collider2.Owner.OnOverlapStay(collisionData2);
                }
            }

        }

    }
}
