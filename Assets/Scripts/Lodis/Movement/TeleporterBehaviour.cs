using FixedPoints;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using Types;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interface for objects that can be teleported.
/// </summary>
public interface ITeleportable
{
    /// <summary>
    /// The last teleporter used by the object.
    /// </summary>
    public TeleporterBehaviour LastTeleporterUsed { get; set; }

    /// <summary>
    /// Event triggered when the object is teleported. Argument is the teleporter used and not the teleporter its going to.
    /// </summary>
    public UnityAction<TeleporterBehaviour> OnTeleportedEvent { get; set; }

    /// <summary>
    /// Called when the object is teleported.
    /// </summary>
    /// <param name="teleporterOwner">The character that casted this teleporter.</param>
    /// <param name="teleporter">The first teleporter spawned.</param>
    /// <param name="linkedTeleporter">The linked teleporter.</param>
    void OnTeleported(EntityDataBehaviour teleporterOwner, TeleporterBehaviour teleporter, TeleporterBehaviour linkedTeleporter);
}

/// <summary>
/// Handles the behavior of a teleporter in the game.
/// </summary>
public class TeleporterBehaviour : SimulationBehaviour
{
    /// <summary>
    /// Represents an object that has been teleported.
    /// </summary>
    private class TeleportedObject
    {
        public GridPhysicsBehaviour EntityPhysics;
        public FVector3 TeleportVelocity;
    }

    [Header("References")]
    [Tooltip("The teleporter linked to this one.")]
    [SerializeField] private TeleporterBehaviour _linkedTeleporter;

    [Tooltip("The owner of this teleporter.")]
    [SerializeField] private EntityDataBehaviour _owner;

    [Tooltip("The collider associated with this teleporter.")]
    [SerializeField] private ColliderBehaviour _collider;

    [Header("Function Parameters")]
    [Tooltip("The maximum number of teleportations allowed.")]
    [SerializeField] private int _maxTeleportations = 3;

    [Tooltip("The number of teleportations remaining.")]
    [SerializeField] private int _teleportationsLeft = 3;

    [Tooltip("The time the teleported character is stunned.")]
    [SerializeField] private Fixed32 _stunTime;

    [Tooltip("The delay before the teleporter turns on.")]
    [SerializeField] private Fixed32 _activeDelay;

    [Tooltip("The delay before the teleporter is disabled automatically. -1 if it should stay forever.")]
    [SerializeField] private Fixed32 _inactiveDelay = -1;

    [Tooltip("The delay before the same object can be teleported again.")]
    [SerializeField] private Fixed32 _sameTeleportDelay;

    [Tooltip("Whether or not this portal will try to use the grid movement script of an object to switch panels.")]
    [SerializeField] private bool _canTeleportUsingGridMovement = true;

    [Header("Visual Parameters")]
    [Tooltip("The amount the teleporter shrinks per use.")]
    [SerializeField] private float _shrinkPerUse = 0.15f;

    [Tooltip("Effect displayed when the teleporter is inactive.")]
    [SerializeField] private GameObject _inactiveEffect;

    [Tooltip("Effect displayed when the teleporter is active.")]
    [SerializeField] private GameObject _activeEffect;

    [Tooltip("Effect displayed when an object is teleported.")]
    [SerializeField] private GameObject _teleportEffect;

    [Tooltip("Effect displayed when the teleporter is destroyed.")]
    [SerializeField] private GameObject _despawnEffect;

    private Vector3 _originalScale;
    private FixedTimeAction _activeDelayAction;
    private FixedTimeAction _sameTeleportAction;
    private bool _active;
    private bool _activeCooldownStarted;
    private bool _canTeleportSameItem = true;
    private bool _isHeldOpen;
    private bool _startedInactiveTimer;
    private EntityDataBehaviour _lastThingTeleported;
    private EntityDataBehaviour _entityHoldingOpen;
    private List<TeleportedObject> _teleportedObjects = new List<TeleportedObject>();

    /// <summary>
    /// The teleporter linked to this one.
    /// </summary>
    public TeleporterBehaviour LinkedTeleporter { get => _linkedTeleporter; set => _linkedTeleporter = value; }

    /// <summary>
    /// Whether the teleporter is being held open.
    /// </summary>
    public bool IsHeldOpen { get => _isHeldOpen; private set => _isHeldOpen = value; }

    /// <summary>
    /// The entity that is preventing the teleporter from shrinking and closing.
    /// </summary>
    public EntityDataBehaviour EntityHoldingOpen { get => _entityHoldingOpen; private set => _entityHoldingOpen = value; }

    /// <summary>
    /// Deserializes the teleporter's state from a binary reader.
    /// </summary>
    public override void Deserialize(BinaryReader br)
    {
        _teleportationsLeft = br.ReadInt32();
        _active = br.ReadBoolean();
        _activeCooldownStarted = br.ReadBoolean();
        _canTeleportSameItem = br.ReadBoolean();
        IsHeldOpen = br.ReadBoolean();
        _startedInactiveTimer = br.ReadBoolean();
    }

    /// <summary>
    /// Serializes the teleporter's state to a binary writer.
    /// </summary>
    public override void Serialize(BinaryWriter bw)
    {
        bw.Write(_teleportationsLeft);
        bw.Write(_active);
        bw.Write(_activeCooldownStarted);
        bw.Write(_canTeleportSameItem);
        bw.Write(IsHeldOpen);
        bw.Write(_startedInactiveTimer);
    }

    public override void Init()
    {
        base.Init();
        MatchManagerBehaviour.Instance.AddOnMatchRestartAction(DisableTeleporter);
    }

    public override void Begin()
    {
        base.Begin();
        _originalScale = _activeEffect.transform.localScale;
        _teleportationsLeft = _maxTeleportations;
    }

    public override void Tick(Fixed32 dt)
    {
        base.Tick(dt);

        //Turn off teleporter if out of uses.
        if (_teleportationsLeft <= 0 && _active)
            DisableTeleporter();

        //If this is linked to a teleporter and is inactive, start the cooldown to reactivate it.
        if (LinkedTeleporter != null && !_active)
        {
            StartActiveCooldown();
        }
        else if (_active && !_startedInactiveTimer && _inactiveDelay != -1)
        {
            FixedPointTimer.StartNewTimedAction(() =>
            {
                DisableTeleporter();
            }, _inactiveDelay);

            _startedInactiveTimer = true;
        }

        // Check if teleported objects have changed direction. Force them back through if they have.
        for (int i = 0; i < _teleportedObjects.Count; i++)
        {
            GridPhysicsBehaviour gridPhysics = _teleportedObjects[i].EntityPhysics;

            if (gridPhysics.Velocity == _teleportedObjects[i].TeleportVelocity)
                continue;

            FVector3 oldVelocity = _teleportedObjects[i].TeleportVelocity.GetNormalized();
            FVector3 newVelocity = gridPhysics.Velocity.GetNormalized();

            Fixed32 dot = FVector3.Dot(oldVelocity, newVelocity);

            if (dot < 0)
            {
                ForceTeleport(gridPhysics.Entity);
            }
        }
    }

    /// <summary>
    /// Called when an object enters the teleporter's collider.
    /// </summary>
    public override void OnOverlapEnter(Collision collision)
    {
        base.OnOverlapEnter(collision);

        if (!CheckTeleportValidity(collision.OtherEntity.UnityScript))
            return;

        ForceTeleport(collision.OtherEntity);
    }

    /// <summary>
    /// Called when an object exits the teleporter's collider.
    /// </summary>
    public override void OnOverlapExit(Collision collision)
    {
        base.OnOverlapExit(collision);

        for (int i = 0; i < _teleportedObjects.Count; i++)
        {
            if (_teleportedObjects[i].EntityPhysics.Entity == collision.OtherEntity.UnityScript)
            {
                _teleportedObjects.RemoveAt(i);
                break;
            }
        }
    }


    /// <summary>
    /// Called when the teleporter is destroyed.
    /// </summary>
    public override void End()
    {
        base.End();
        _active = false;

        _inactiveEffect.SetActive(true);
        _activeEffect.SetActive(false);
        _activeEffect.transform.localScale = _originalScale;
        _activeCooldownStarted = false;
        _sameTeleportAction?.Stop();
        _linkedTeleporter = null;
    }

    /// <summary>
    /// Activates or deactivates the teleporter. Spawns the appropriate effects.
    /// </summary>
    public void SetActive(bool active)
    {
        if (active && !_active)
            _startedInactiveTimer = false;

        _active = active;
        _inactiveEffect.SetActive(!active);
        _activeEffect.SetActive(active);

        if (LinkedTeleporter != null && LinkedTeleporter._active != active)
            LinkedTeleporter.SetActive(active);
    }

    /// <summary>
    /// Starts the cooldown for the teleporter to become active again.
    /// </summary>
    public void StartActiveCooldown()
    {
        if (_activeCooldownStarted || _teleportationsLeft <= 0)
            return;

        _activeCooldownStarted = true;

        //Create a new timed action if one doesn't already exist. Otherwise, reset the existing one.
        if (_activeDelayAction == null)
        {
            _activeDelayAction = FixedPointTimer.StartNewTimedAction(() =>
            {
                _activeCooldownStarted = false;
                SetActive(true);
            }, _activeDelay);
        }
        else
        {
            _activeDelayAction.Reset();
        }
    }

    /// <summary>
    /// Holds the teleporter open for a specific entity. Prevents it from shrinking and stops the countdown of uses.
    /// </summary>
    public void HoldTeleporterOpen(EntityDataBehaviour entity)
    {
        IsHeldOpen = true;
        EntityHoldingOpen = entity;
        _teleportationsLeft++;

        _linkedTeleporter.IsHeldOpen = true;
        _linkedTeleporter.EntityHoldingOpen = entity;
        _linkedTeleporter._teleportationsLeft++;
    }

    /// <summary>
    /// Initializes the teleporter with a linked teleporter and owner.
    /// </summary>
    public void InitTeleporter(TeleporterBehaviour linked, EntityDataBehaviour owner)
    {
        _teleportationsLeft = _maxTeleportations;
        LinkedTeleporter = linked;
        _owner = owner;

        if (_collider)
            _collider.Spawner = owner;

        _teleportedObjects.Clear();
        _lastThingTeleported = null;
    }

    public void InitTeleporter(EntityDataBehaviour owner)
    {
        InitTeleporter(LinkedTeleporter, owner);
    }

    /// <summary>
    /// Plays the teleportation effect.
    /// </summary>
    public void PlayTeleportEffect()
    {
        _teleportEffect.SetActive(true);
    }

    /// <summary>
    /// Returns the teleporter to the object pool and plays the shutdown effect.
    /// </summary>
    public void DisableTeleporter()
    {
        if (IsHeldOpen)
        {
            Debug.Log("Teleporter is being held open, not disabling. Blocker: " + EntityHoldingOpen.Data.Name);
            return;
        }

        if (!Entity.Active)
            return;

        Instantiate(_despawnEffect, transform.position, Camera.main.transform.rotation);
        ObjectPoolBehaviour.Instance.ReturnGameObject(Entity);
    }

    /// <summary>
    /// Checks if an entity can be teleported.
    /// </summary>
    private bool CheckTeleportValidity(EntityDataBehaviour entity)
    {
        bool teleporterReady = _teleportationsLeft > 0 || _active;
        bool sameItemCheckValid = entity != LinkedTeleporter._lastThingTeleported || _canTeleportSameItem;
        bool notOwner = entity != _owner;

        TeleportedObject teleportedObj = _teleportedObjects.Find(t => t.EntityPhysics.Entity.FixedTransform == entity.FixedTransform.Parent);

        bool teleportedParent = teleportedObj != null;

        return teleporterReady && sameItemCheckValid && notOwner && !teleportedParent;
    }

    /// <summary>
    /// Forces an entity to teleport to the linked teleporter.
    /// </summary>
    public void ForceTeleport(EntityData entityToTeleport)
    {
        // If the entity being teleported is holding the teleporter open, release it.  
        if (entityToTeleport.UnityScript == EntityHoldingOpen)
        {
            IsHeldOpen = false;
            EntityHoldingOpen = null;
            LinkedTeleporter.IsHeldOpen = false;
            LinkedTeleporter.EntityHoldingOpen = null;
        }

        // Check if the entity implements the ITeleportable interface and handle teleportation accordingly.  
        ITeleportable teleportHandler = entityToTeleport.UnityObject.GetComponentInChildren<ITeleportable>();

        if (teleportHandler != null)
        {
            // Trigger the OnTeleported event and update the last teleporter used.  
            teleportHandler.OnTeleportedEvent?.Invoke(this);
            teleportHandler.OnTeleported(_owner, this, LinkedTeleporter);
            teleportHandler.LastTeleporterUsed = this;
        }
        else
        {
            // Handle teleportation for entities without the ITeleportable interface.  
            GridMovementBehaviour movement = entityToTeleport.GetComponentInChildren<GridMovementBehaviour>();
            KnockbackBehaviour health = entityToTeleport.GetComponentInChildren<KnockbackBehaviour>();

            if (health && health.CanBeHit())
            {
                // If the entity is in the air, stun it and teleport directly.  
                if (health.CurrentAirState != AirState.NONE || !_canTeleportUsingGridMovement)
                {
                    health.Stun(_stunTime);
                    entityToTeleport.Transform.WorldPosition = LinkedTeleporter.FixedTransform.WorldPosition;
                }
                else if (movement)
                {
                    // If the entity is on the ground, move it to the linked teleporter's panel.  
                    PanelBehaviour panel;
                    GridBehaviour.Instance.GetPanelAtLocationInWorld((Vector3)LinkedTeleporter.FixedTransform.WorldPosition, out panel);
                    movement.SnapToTarget();
                    movement.MoveToPanel(panel);

                    // Apply a delayed stun effect.Delayed so the movement to the new location still happens. 
                    FixedPointTimer.StartNewTimedAction(() => health.Stun(_stunTime), new Fixed32(6553));
                }
            }
            else
            {
                // If the entity cannot be hit, teleport it directly.  
                entityToTeleport.Transform.WorldPosition = LinkedTeleporter.FixedTransform.WorldPosition;
            }
        }

        // Decrease the teleportation count for both teleporters.  
        _teleportationsLeft--;
        _linkedTeleporter._teleportationsLeft--;

        // Shrink the teleporter's visual effect if it is not being held open.  
        if (!_isHeldOpen)
        {
            Vector3 shrinkAmount = Vector3.one * _shrinkPerUse;

            _activeEffect.transform.localScale -= shrinkAmount;
            LinkedTeleporter._activeEffect.transform.localScale -= shrinkAmount;
        }

        // Play the teleportation effect for both teleporters.  
        PlayTeleportEffect();
        LinkedTeleporter.PlayTeleportEffect();

        // Update the last teleported entity and prevent immediate re-teleportation of the same entity.  
        _lastThingTeleported = entityToTeleport.UnityScript;
        _canTeleportSameItem = false;
        _linkedTeleporter._lastThingTeleported = entityToTeleport.UnityScript;
        _linkedTeleporter._canTeleportSameItem = false;

        // Track the teleported entity and its velocity. This is so we can teleport it back if it changes directions too soon.
        GridPhysicsBehaviour gridPhysics = entityToTeleport.GetComponent<GridPhysicsBehaviour>();

        _teleportedObjects.RemoveAll(t => t.EntityPhysics == gridPhysics);

        TeleportedObject teleportedObj = new TeleportedObject()
        {
            EntityPhysics = gridPhysics,
            TeleportVelocity = gridPhysics.Velocity
        };

        _linkedTeleporter._teleportedObjects.Add(teleportedObj);

        // Start a timer to allow the same entity to be teleported again after a delay.  
        _sameTeleportAction = FixedPointTimer.StartNewTimedAction(() =>
        {
            _linkedTeleporter._canTeleportSameItem = true;
            _canTeleportSameItem = true;
        }, _sameTeleportDelay);
    }
}
