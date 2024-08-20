using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

/// <summary>
/// Couples the unity game object with the simulation entity data.
/// Handles adding all of the simulation components to the simulation entity so they are in sync
/// with the rollback simulation instead of Unity's simulation.
/// </summary>
public class EntityDataBehaviour : MonoBehaviour
{
    [Tooltip("The rollback simulation's representation of this game object.")]
    [SerializeField] private EntityData _entityData;
    [Tooltip("If true the entity will not be added to the rollback simulation when the game starts.")]
    [SerializeField] private bool _addToGameManually;
    [Tooltip("The transform of the object that is the visual representation of this entity.")]
    [SerializeField] private Transform _visualRoot;

    /// <summary>
    /// The rollback simulations representation of this game object.
    /// </summary>
    public EntityData Data {  get { return _entityData; } set { _entityData = value; } }

    public FTransform FixedTransform { get { return _entityData.Transform; } }

    // Start is called before the first frame update
    void Awake()
    {
        //Adds all components to the entity so they can be updated by the rollback simulation.
        SimulationBehaviour[] simComponents = GetComponentsInChildren<SimulationBehaviour>();

        foreach (SimulationBehaviour sim in simComponents)
        {
            Data.AddComponent(sim);
            sim.Entity = this;
        }

        Data.Init();
        Data.OnTick += UpdateUnityTransform;

        _entityData.UnityObject = gameObject;

        Fixed32 angle = Fixed32.PI / 2;  // 90 degrees in radians
        Fixed32 sinValue = Fixed32.Sin(angle);
        Fixed32 cosValue = Fixed32.Cos(angle);
        Debug.Log($"Sin(90 degrees): {(float)sinValue}, Cos(90 degrees): {(float)cosValue}");

        //Try to add entity to game so it can be updated.
        if (!_addToGameManually)
            GridGame.AddEntityToGame(_entityData);
    }

    /// <summary>
    /// Adds this entity to the rollback simulation so it can be updated.
    /// Also sets it active in the unity scene so it can be visually represented.
    /// </summary>
    public void AddToGame()
    {
        gameObject.SetActive(true);
        GridGame.AddEntityToGame(_entityData);
    }

    /// <summary>
    /// Removes this entity to the rollback simulation so it won't be updated.
    /// Also sets it inactive in the unity scene so it won't be visually represented.
    /// </summary>
    public void RemoveFromGame()
    {
        gameObject.SetActive(false);    
        GridGame.RemoveEntityFromGame(_entityData);
    }

    private void UpdateUnityTransform(Fixed32 dt)
    {
        _visualRoot.SetPositionAndRotation((Vector3)Data.Transform.Position, (Quaternion)Data.Transform.Rotation);
    }

    private void OnDestroy()
    {
        GridGame.RemoveEntityFromGame(_entityData);
    }
}
