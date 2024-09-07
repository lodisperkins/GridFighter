using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using Types;
using UnityEngine;
using static PixelCrushers.DialogueSystem.ActOnDialogueEvent;
using static UnityEngine.Rendering.DebugUI.Table;

public struct Collision
{
    public FVector2 Normal;
    public GridCollider Collider;
    public FVector2 ContactPoint;
    public Fixed32 PenetrationDistance;
    public EntityData Entity;
}

public delegate void CollisionEvent(Collision collision);

/// <summary>
/// An AABB collider that use the y position of the panel it's on for depth.
/// </summary>
[Serializable]
public class GridCollider
{
    [Tooltip("The layers this collider won't collide with.")]
    [SerializeField] private LayerMask _layersToIgnore;
    [Tooltip("If true this collider will pass through objects without trying to apply a force to prevent them going through each other.")]
    [SerializeField] private bool _overlap;
    [Tooltip("How wide the AABB collider will be.")]
    [SerializeField] private Fixed32 _width = 1;
    [Tooltip("How tall the AABB collider will be.")]
    [SerializeField] private Fixed32 _height = 1;
    [Tooltip("The y position of this collider on the grid relative to its owner.")]
    [SerializeField] private int _panelYOffset;
    [Tooltip("The x position of this collider on the grid relative to its owner.")]
    [SerializeField] private int _panelXOffset;
    [Tooltip("The height of this collider in the world.")]
    [SerializeField] private Fixed32 _worldYPosition;

    //---
    private int _layer;
    private EntityData _owner;
    private GridPhysicsBehaviour _ownerPhysicsComponent;
    private Collision[] _collisions = new Collision[20];

    public event CollisionEvent OnCollisionEnter;
    public event CollisionEvent OnCollisionStay;
    public event CollisionEvent OnCollisionExit;

    public event CollisionEvent OnOverlapEnter;
    public event CollisionEvent OnOverlapStay;
    public event CollisionEvent OnOverlapExit;

    /// <summary>
    /// The current y position of this panel on the grid. Adds the y offset to the owner position.
    /// </summary>
    public int PanelY 
    {
        get
        {
            return PanelYOffset + OwnerPhysicsComponent.GetGridPosition().Y;
        }
        set => PanelYOffset = value;
    }

    /// <summary>
    /// The current x position of this panel on the grid. Adds the x offset to the owner position.
    /// </summary>
    public int PanelX 
    {
        get
        {
            return PanelXOffset + OwnerPhysicsComponent.GetGridPosition().X;
        }
        set => PanelXOffset = value;
    }

    /// <summary>
    /// Gets the position of this collider in the world based on the current panel its owner is on.
    /// </summary>
    public FVector3 WorldPosition
    {
        get
        {
            GridBehaviour.Grid.GetPanel(PanelX, PanelY, out PanelBehaviour panel);
            return (FVector3)(panel.transform.position + Vector3.up * WorldYPosition);
        }
    }

    /// <summary>
    /// How wide the AABB collider will be.
    /// </summary>
    public Fixed32 Width { get => _width; set => _width = value; }
    /// <summary>
    /// How tall the AABB collider will be.
    /// </summary>
    public Fixed32 Height { get => _height; set => _height = value; }

    /// <summary>
    /// The simulation object that will have collision events called on it.
    /// </summary>
    public EntityData Owner
    {
        get
        {
            return _owner;
        }
        set
        {
            _owner = value;
        }
    }

    /// <summary>
    /// The components responsible for calculating physics attached to the entity this collider belongs to.
    /// </summary>
    public GridPhysicsBehaviour OwnerPhysicsComponent { get => _ownerPhysicsComponent; }
    /// <summary>
    /// The layers this collider won't collide with.
    /// </summary>
    public LayerMask LayersToIgnore { get => _layersToIgnore; set => _layersToIgnore = value; }
    /// <summary>
    /// If true this collider will pass through objects without trying to apply a force to prevent them going through each other.
    /// </summary>
    public bool Overlap { get => _overlap; set => _overlap = value; }
    /// <summary>
    /// The y position of this collider on the grid relative to its owner.
    /// </summary>
    public int PanelYOffset { get => _panelYOffset; set => _panelYOffset = value; }

    /// <summary>
    /// The x position of this collider on the grid relative to its owner.
    /// </summary>
    public int PanelXOffset { get => _panelXOffset; set => _panelXOffset = value; }

    /// <summary>
    /// The height of this collider in the world.
    /// </summary>
    public Fixed32 WorldYPosition { get => _worldYPosition; set => _worldYPosition = value; }

    public void Init(EntityData owner, GridPhysicsBehaviour ownerPhysicsComponent = null)
    {
        _ownerPhysicsComponent = ownerPhysicsComponent;
        _owner = owner;
        _layer = ownerPhysicsComponent.gameObject.layer;
        owner.Collider = this;
    }

    public void Init(Fixed32 width, Fixed32 height, EntityData owner, GridPhysicsBehaviour ownerPhysicsComponent = null, int panelYOffset = 0, int panelXOffset = 0, Fixed32 worldYPosition = default)
    {
        _width = width;
        _height = height;
        PanelYOffset = panelYOffset;
        PanelXOffset = panelXOffset;
        WorldYPosition = worldYPosition;
        _ownerPhysicsComponent = ownerPhysicsComponent;
        _owner = owner;
        _layer = ownerPhysicsComponent.gameObject.layer;
    }

    /// <summary>
    /// Checks if the layer is in the colliders layer mask of 
    /// layers to ignore.
    /// </summary>
    /// <param name="layer">The unity physics collision layer of the game object.</param>
    /// <returns></returns>
    public bool CheckIfLayerShouldBeIgnored(int layer)
    {
        if (LayersToIgnore == 0)
            return false;

        int mask = LayersToIgnore;
        if (mask == (mask | 1 << layer))
            return true;

        return false;
    }

    /// <summary>
    /// Removes a collider from the array of active collisions. Used with calling on collision exit events.
    /// </summary>
    private bool RemoveCollider(GridCollider other, out Collision collision)
    {
        collision = default;

        for (int i = 0; i < _collisions.Length; i++)
        {
            if (_collisions[i].Collider == other && other != null)
            {
                collision = _collisions[i];
                _collisions[i] = default;
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Adds a collider to the array of active collisions. Used with calling on collision enter events.
    /// </summary>
    private bool AddCollider(Collision collision)
    {
        for (int i = 0; i < _collisions.Length; i++)
        {
            if (_collisions[i].Collider == null)
            {
                _collisions[i] = collision;
                return true;
            }
        }

        return false;
    }

    public bool CheckCollision(GridCollider other, out Collision collisionData)
    {
        collisionData = default;

        //Check if collision should occur using Unity layers.
        if (CheckIfLayerShouldBeIgnored(other._layer))
            return false;

        //Check collision for AABB.
        bool collisionDetected =
            GetRight() > other.GetLeft() &&
            GetBottom() < other.GetTop() &&
            GetTop() > other.GetBottom() &&
            GetLeft() < other.GetRight() && 
            other.PanelY == PanelY;


        if (!collisionDetected)
        {
            Collision collision;

            //If the collider was active and was removed...
            if (RemoveCollider(other, out collision))
            {
                //...call the exit events.
                if (Overlap)
                    OnOverlapExit?.Invoke(collision);
                else 
                    OnCollisionExit?.Invoke(collision);
            }

            return false;
        }

        //Calculating contact point.
        FVector3 otherToAABB = other.Owner.Transform.WorldPosition - Owner.Transform.WorldPosition;

        if (otherToAABB.Z > Width / 2)
            otherToAABB.Z = Width / 2;
        else if (otherToAABB.Z < -Width / 2)
            otherToAABB.Z = -Width / 2;

        if (otherToAABB.Y > Height / 2)
            otherToAABB.Y = Height / 2;
        else if (otherToAABB.Y < -Height / 2)
            otherToAABB.Y = -Height / 2;

        FVector3 closestPoint = Owner.Transform.WorldPosition + otherToAABB;

        FVector3 otherToClosestPoint = (other.Owner.Transform.WorldPosition - closestPoint);

        collisionData = new Collision
        {
            Collider = other,
            Normal = GetPenetrationAmount(other).GetNormalized(),
            ContactPoint = closestPoint,
            PenetrationDistance = GetPenetrationAmount(other).Magnitude,
            Entity = other.Owner
        };

        Collision collisionData2 = new Collision
        {
            Normal = collisionData.Normal * -1,
            Collider = this,
            PenetrationDistance = GetPenetrationAmount(other).Magnitude,
            ContactPoint = closestPoint,
            Entity = Owner
        };

        //If this is a new collision...
        if (AddCollider(collisionData))
        {
            //...call on collision enter events.
            if (Overlap)
            {
                OnOverlapEnter?.Invoke(collisionData);
                other.OnOverlapEnter?.Invoke(collisionData2);
            }
            else
            {
                OwnerPhysicsComponent.ResolveCollision(collisionData);
                OnCollisionEnter?.Invoke(collisionData);
                other.OnCollisionEnter?.Invoke(collisionData2);
            }
        }

        //On collision stay events are always called when collision occurs.
        if (Overlap)
            OnOverlapStay?.Invoke(collisionData);
        else
            OnCollisionStay?.Invoke(collisionData);

        return true;
    }

    public void Draw()
    {
        FVector2 position = Owner.Transform.WorldPosition;
        // Replace this with an appropriate draw method for your framework, if any
        // For example, using UnityEngine:
        Debug.DrawLine(new Vector3(GetLeft(), GetBottom()), new Vector3(GetRight(), GetTop()), Color.red);
    }

    public Fixed32 GetLeft()
    {
        return WorldPosition.X - Width / 2;
    }

    public Fixed32 GetRight()
    {
        return WorldPosition.X + Width / 2;
    }

    public Fixed32 GetTop()
    {
        return WorldPosition.Y + Height / 2;
    }

    public Fixed32 GetBottom()
    {
        return WorldPosition.Y - Height / 2;
    }

    private FVector2 GetPenetrationAmount(GridCollider other)
    {
        Fixed32 smallestPenetration = Math.Abs(GetRight() - other.GetLeft());

        FVector2 normalFace = new FVector2(1, 0);

        if (Math.Abs(GetLeft() - other.GetRight()) < smallestPenetration)
        {
            smallestPenetration = Math.Abs(GetLeft() - other.GetRight());
            normalFace = new FVector2(-1, 0);
        }
        if (Math.Abs(GetTop() - other.GetBottom()) < smallestPenetration)
        {
            smallestPenetration = Math.Abs(GetTop() - other.GetBottom());
            normalFace = new FVector2(0, 1);
        }
        if (Math.Abs(GetBottom() - other.GetTop()) < smallestPenetration)
        {
            smallestPenetration = Math.Abs(GetBottom() - other.GetTop());
            normalFace = new FVector2(0, -1);
        }

        return normalFace * smallestPenetration;
    }

    public void Serialize(BinaryWriter bw)
    {
        _width.Serialize(bw);
        _height.Serialize(bw);
        bw.Write(PanelYOffset);
    }

    public void Deserialize(BinaryReader br)
    {
        _width.Deserialize(br);
        _height.Deserialize(br);
        PanelYOffset = br.ReadInt32();
    }
}