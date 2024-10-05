using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.Utility;
using NaughtyAttributes;
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
    /// <summary>
    /// The direction the collision occured in relative to the object that started the collision.
    /// </summary>
    public FVector2 Normal;
    /// <summary>
    /// The collider of the object that was collided with.
    /// </summary>
    public GridCollider OtherCollider;
    /// <summary>
    /// The area on the AABB that the collision occured.
    /// </summary>
    public FVector2 ContactPoint;
    /// <summary>
    /// How deep the colliders are overlapping.
    /// </summary>
    public Fixed32 PenetrationDistance;
    /// <summary>
    /// The entity that the collider is overlapping with.
    /// </summary>
    public EntityData OtherEntity;
    /// <summary>
    /// The entity that is attached to this collider.
    /// </summary>
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
    [Tooltip("The tags this collider won't collide with.")]
    [SerializeField] private string[] _tagsToIgnore;
    [Tooltip("If true this collider will pass through objects without trying to apply a force to prevent them going through each other.")]
    [SerializeField] private bool _overlap;
    [Tooltip("If true this collider will collide will not consider the panel y position when colliding.")]
    [SerializeField] private bool _collideOnAnyRow;
    [Tooltip("Whether or not this collider has a defined width. If true, will calculate collision on x based on whether the x position of the other object is greater that or lower than this object's x position.")]
    [SerializeField] private bool _isAWall;
    [Tooltip("How wide the AABB collider will be.")]
    [HideIf("_isAWall")]
    [SerializeField] private Fixed32 _width = 1;
    [Tooltip("Keeps track of the direction the object is facing. Used to determine when an object is behind this one.")]
    [ShowIf("_isAWall")]
    [SerializeField] private bool _facingRight = true;
    [Tooltip("How tall the AABB collider will be.")]
    [SerializeField] private Fixed32 _height = 1;
    [Tooltip("The y position of this collider on the grid relative to its owner.")]
    [SerializeField] private int _panelYOffset;
    [Tooltip("The x position of this collider on the grid relative to its owner.")]
    [SerializeField] private int _panelXOffset;
    [Tooltip("The height of this collider in the world.")]
    [SerializeField] private Fixed32 _worldYPosition;

    //---
    private EntityDataBehaviour _entity;
    private int _layer;
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
            return PanelXOffset * _entity.FixedTransform.Forward.Z + OwnerPhysicsComponent.GetGridPosition().X;
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
            if (PanelXOffset != 0 || PanelYOffset != 0)
            {
                GridBehaviour.Grid.GetPanel(PanelX, PanelY, out PanelBehaviour panel);
                if (panel != null)
                {
                    return panel.FixedWorldPosition + FVector3.Up * WorldYPosition;
                }
                else
                {
                    return FVector3.Zero;
                }
            }

            FVector3 position = Entity.FixedTransform.WorldPosition + FVector3.Up * WorldYPosition;

            return position;
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
    public EntityDataBehaviour Entity
    {
        get
        {
            return _entity;
        }
        set
        {
            _entity = value;
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
    public string[] TagsToIgnore { get => _tagsToIgnore; set => _tagsToIgnore = value; }
    public bool IsAWall { get => _isAWall; set => _isAWall = value; }

    public void Init(EntityDataBehaviour owner, GridPhysicsBehaviour ownerPhysicsComponent = null)
    {
        _ownerPhysicsComponent = ownerPhysicsComponent;
        _entity = owner;

        if (_ownerPhysicsComponent)
            _layer = ownerPhysicsComponent.gameObject.layer;

        //Try to auto add this collider to the owners array
        AddColliderToOwner();
    }


    public void Init(Fixed32 width, Fixed32 height, EntityDataBehaviour owner, GridPhysicsBehaviour ownerPhysicsComponent = null, int panelYOffset = 0, int panelXOffset = 0, Fixed32 worldYPosition = default)
    {
        _width = width;
        _height = height;
        PanelYOffset = panelYOffset;
        PanelXOffset = panelXOffset;
        WorldYPosition = worldYPosition;
        _ownerPhysicsComponent = ownerPhysicsComponent;
        _entity = owner;

        //Try to auto add this collider to the owners array
        AddColliderToOwner();

        if (_ownerPhysicsComponent)
            _layer = ownerPhysicsComponent.gameObject.layer;
    }

    private void AddColliderToOwner()
    {
        if (_entity.Data.Colliders != null)
        {
            if (_entity.Data.Colliders.Contains(this))
                throw new Exception("Tried to add the same collider to " + _entity.Data.Name);

            _entity.Data.Colliders = _entity.Data.Colliders.Add(this);
        }
        else
        {
            _entity.Data.Colliders = new GridCollider[] { this };
        }
    }
    /// <summary>
    /// Checks if the layer is in the colliders layer mask of 
    /// layers to ignore.
    /// </summary>
    /// <param name="layer">The unity physics collision layer of the game object.</param>
    /// <returns></returns>
    public bool CheckIfColliderShouldBeIgnored(GameObject unityObject)
    {
        if (LayersToIgnore == 0)
            return false;

        int layer = unityObject.layer;

        int mask = LayersToIgnore;

        bool ignoresLayer = false;
        bool ignoresTag = false;

        if (mask == (mask | 1 << layer))
            ignoresLayer = true;

        foreach (string tag in TagsToIgnore)
        {
            if (unityObject.CompareTag(tag))
            {
                ignoresTag = true;
                break;
            }
        }

        return ignoresLayer || ignoresTag;
    }

    /// <summary>
    /// Removes a collider from the array of active collisions. Used with calling on collision exit events.
    /// </summary>
    private bool RemoveCollider(GridCollider other, out Collision collision)
    {
        collision = default;

        for (int i = 0; i < _collisions.Length; i++)
        {
            if (_collisions[i].OtherCollider == other || _collisions[i].Entity == other.Entity.Data)
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
            if (_collisions[i].OtherEntity == collision.OtherEntity)
                return false;

            if (_collisions[i].OtherCollider == null || !_collisions[i].OtherEntity.Active)
            {
                _collisions[i] = collision;
                return true;
            }
        }

        return false;
    }

    public void ClearCollisionExit()
    {
        for (int i = 0; i < _collisions.Length; i++)
        {
            _collisions[i] = default;
        }
    }

    private bool CheckWallCollisionOnX(GridCollider other)
    {
        bool collidingOnX = false;

        if (_facingRight)
        {
            collidingOnX = other.WorldPosition.X <= WorldPosition.X;
        }
        else
        {
            collidingOnX = other.WorldPosition.X >= WorldPosition.X;
        }

        return collidingOnX;
    }

    public bool CheckCollision(GridCollider other)
    {
        //Check if collision should occur using Unity layers.
        if (CheckIfColliderShouldBeIgnored(other.Entity.Data.UnityObject) || other.CheckIfColliderShouldBeIgnored(Entity.Data.UnityObject))
            return false;

        bool collidingOnX = false;

        if (IsAWall)
        {
            collidingOnX = CheckWallCollisionOnX(other);
        }
        else if (other.IsAWall)
        {
            collidingOnX = other.CheckWallCollisionOnX(this);
        }
        else
        {
            collidingOnX = GetRight() > other.GetLeft() && GetLeft() < other.GetRight();
        }

        //Check collision for AABB.
        bool collisionDetected = 
            //Check vertical AABB collision
            GetBottom() < other.GetTop() && GetTop() > other.GetBottom() &&
            //Checking horizontal collision
            collidingOnX &&
            //Check special collision params
            (other.PanelY == PanelY || other._collideOnAnyRow || _collideOnAnyRow);


        if (!collisionDetected)
        {
            Collision collision;
            Collision collision2;

            //If the collider was active and was removed...
            if (RemoveCollider(other, out collision))
            {
                other.RemoveCollider(this, out collision2);

                //...call the exit events.
                if (Overlap)
                {
                    OnOverlapExit?.Invoke(collision);
                    other.OnOverlapExit?.Invoke(collision2);
                }
                else
                {
                    OnCollisionExit?.Invoke(collision);
                    OnCollisionExit?.Invoke(collision2);
                }
            }

            return false;
        }

        //Calculating contact point.
        FVector3 otherToAABB = other.Entity.Data.Transform.WorldPosition - Entity.Data.Transform.WorldPosition;

        if (otherToAABB.X > Width / 2)
            otherToAABB.X = Width / 2;
        else if (otherToAABB.Z < -Width / 2)
            otherToAABB.X = -Width / 2;

        if (otherToAABB.Y > Height / 2)
            otherToAABB.Y = Height / 2;
        else if (otherToAABB.Y < -Height / 2)
            otherToAABB.Y = -Height / 2;

        FVector3 closestPoint = Entity.Data.Transform.WorldPosition + otherToAABB;

        FVector3 otherToClosestPoint = (other.Entity.Data.Transform.WorldPosition - closestPoint);

        Collision collisionData = new Collision
        {
            OtherCollider = other,
            Normal = GetPenetrationAmount(other).GetNormalized(),
            ContactPoint = closestPoint,
            PenetrationDistance = GetPenetrationAmount(other).Magnitude,
            OtherEntity = other.Entity,
            Entity = Entity
        };

        Collision collisionData2 = new Collision
        {
            Normal = collisionData.Normal * -1,
            OtherCollider = this,
            PenetrationDistance = GetPenetrationAmount(other).Magnitude,
            ContactPoint = closestPoint,
            OtherEntity = Entity,
            Entity = other.Entity
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
        {
            OnOverlapStay?.Invoke(collisionData);
            other.OnOverlapStay?.Invoke(collisionData2);
        }
        else
        {
            OnCollisionStay?.Invoke(collisionData);
            other.OnCollisionStay?.Invoke(collisionData2);
        }

        return true;
    }

    public void Draw()
    {
        FVector2 position = Entity.Data.Transform.WorldPosition;
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