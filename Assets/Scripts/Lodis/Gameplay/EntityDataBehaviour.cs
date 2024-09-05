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

    //---
    protected bool inGame;

    /// <summary>
    /// The rollback simulations representation of this game object.
    /// </summary>
    public EntityData Data {  get { return _entityData; } set { _entityData = value; } }

    public FTransform FixedTransform { get { return _entityData.Transform; } }

    public bool AddToGameManually { get => _addToGameManually; private set => _addToGameManually = value; }

    public bool Active { get => Data.Active; }

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

        //Adds all components to the entity so they can be updated by the rollback simulation.
        EntityDataBehaviour[] entityComps = GetComponentsInChildren<EntityDataBehaviour>();

        foreach (EntityDataBehaviour entity in entityComps)
        {
            if (entity == this)
                continue;

            entity.transform.parent = null;
            Data.Transform.AddChild(entity.Data.Transform);
        }

        if (string.IsNullOrEmpty(_entityData.Name))
            _entityData.Name = gameObject.name;

        Data.Init();
        Data.OnTick += UpdateUnityTransform;

        _entityData.UnityObject = gameObject;

        //Try to add entity to game so it can be updated.
        if (!AddToGameManually)
        {
            GridGame.AddEntityToGame(_entityData);
            inGame = true;
        }
    }

    /// <summary>
    /// Adds this entity to the rollback simulation so it can be updated.
    /// Also sets it active in the unity scene so it can be visually represented.
    /// </summary>
    public void AddToGame()
    {
        gameObject.SetActive(true);
        GridGame.AddEntityToGame(_entityData);
        inGame = true;
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
        if (_visualRoot)
            _visualRoot.SetPositionAndRotation((Vector3)Data.Transform.WorldPosition, (Quaternion)Data.Transform.WorldRotation);
    }

    private void OnEnable()
    {
        if (!inGame)
        {
            AddToGame();
        }
    }

    private void OnDisable()
    {
        inGame = false;
        GridGame.RemoveEntityFromGame(_entityData);
    }

    private void OnDestroy()
    {
        inGame = false;
        GridGame.RemoveEntityFromGame(_entityData);
    }

    private void OnDrawGizmos()
    {
        if (_entityData == null || _entityData.Transform == null)
            return;

        // Get the FTransform global position, rotation, and scale
        FTransform fixedTransform = _entityData.Transform;
        Vector3 position = (Vector3)fixedTransform.WorldPosition;

        // Calculate the forward, right, and up directions based on the FTransform
        Vector3 forward = (Vector3)(fixedTransform.WorldRotation * FVector3.Forward);
        Vector3 right = (Vector3)(fixedTransform.WorldRotation * FVector3.Right);
        Vector3 up = (Vector3)(fixedTransform.WorldRotation * FVector3.Up);

        // Set Gizmo color and draw the lines for forward (blue), right (red), and up (green)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + forward * 2); // Forward (Z-axis)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + right * 2); // Right (X-axis)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(position, position + up * 2); // Up (Y-axis)

        // Draw a small cube at the position to represent the object
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(position, Vector3.one * 0.1f);
    }
}
