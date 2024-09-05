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
    [SerializeField] private LayerMask _layersToIgnore;
    [SerializeField] private bool _overlap;

    //---
    private Fixed32 _width;
    private Fixed32 _height;
    private int _panelYOffset;
    private int _panelXOffset;
    private Fixed32 _worldYPosition;
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

    public int PanelY 
    {
        get
        {
            return _panelYOffset + OwnerPhysicsComponent.GetGridPosition().Y;
        }
        set => _panelYOffset = value;
    }

    public int PanelX 
    {
        get
        {
            return _panelXOffset + OwnerPhysicsComponent.GetGridPosition().X;
        }
        set => _panelXOffset = value;
    }

    public FVector3 WorldPosition
    {
        get
        {
            GridBehaviour.Grid.GetPanel(PanelX, PanelY, out PanelBehaviour panel);
            return (FVector3)(panel.transform.position + Vector3.up * _worldYPosition);
        }
    }

    public Fixed32 Width { get => _width; set => _width = value; }
    public Fixed32 Height { get => _height; set => _height = value; }

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

    public GridPhysicsBehaviour OwnerPhysicsComponent { get => _ownerPhysicsComponent; }
    public LayerMask LayersToIgnore { get => _layersToIgnore; set => _layersToIgnore = value; }
    public bool Overlap { get => _overlap; set => _overlap = value; }

    public void Init(Fixed32 width, Fixed32 height, EntityData owner, GridPhysicsBehaviour ownerPhysicsComponent = null, int panelYOffset = 0, int panelXOffset = 0, Fixed32 worldYPosition = default)
    {
        _width = width;
        _height = height;
        _panelYOffset = panelYOffset;
        _panelXOffset = panelXOffset;
        _worldYPosition = worldYPosition;
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

        //If this is a new collision...
        if (AddCollider(collisionData))
        {
            //...call on collision enter events.
            if (Overlap)
                OnOverlapEnter?.Invoke(collisionData);
            else
                OnCollisionEnter?.Invoke(collisionData);
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
        return WorldPosition.Z - Width / 2;
    }

    public Fixed32 GetRight()
    {
        return WorldPosition.Z + Width / 2;
    }

    public Fixed32 GetTop()
    {
        return WorldPosition.Y + Height / 2;
    }

    public Fixed32 GetBottom()
    {
        return Owner.Transform.WorldPosition.Y - Height / 2;
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
        bw.Write(_panelYOffset);
    }

    public void Deserialize(BinaryReader br)
    {
        _width.Deserialize(br);
        _height.Deserialize(br);
        _panelYOffset = br.ReadInt32();
    }
}