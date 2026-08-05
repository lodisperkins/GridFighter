using Assets.Scripts.Lodis.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace FixedPoints
{
    /// <summary>
    /// A transform that functions similar to Unity's transform but uses fixed-point math.
    /// </summary>
    [Serializable]
    public class FTransform : ISerializedListObject
    {
        [SerializeField] private FVector3 _localPosition;
        [SerializeField] private FQuaternion _localRotation;
        [SerializeField] private FVector3 _worldScale;

        private FTransform _parent;
        private SerializedListHandler<FTransform> _children;
        private FVector3 _cachedWorldPosition;
        private FQuaternion _cachedWorldRotation;
        private FVector3 _cachedWorldScale;
        private bool _worldTransformDirty = true;

        public EntityData EntityData { get; private set; }
        public bool TrackingEnabled { get; set; }

        public FVector3 WorldPosition
        {
            get
            {
                if (_parent == null)
                {
                    UpdateWorldTransformCache();
                    return _cachedWorldPosition;
                }
                else
                {
                    UpdateWorldTransformCache();
                    return _cachedWorldPosition;
                }
            }
            set
            {
                if (_parent != null)
                    _localPosition = InverseTransformPoint(_parent, value); // Directly update the private value
                else
                    _localPosition = value;

                MarkWorldTransformDirty();

                if (TrackingEnabled)
                {
                    StackTrace stackTrace = new StackTrace(true);
                    UnityEngine.Debug.Log($"[FTransform] WorldPosition set to {value} for Entity {EntityData?.Name}. Called from {stackTrace.GetFrame(1).GetMethod().Name} in {stackTrace.GetFrame(1).GetFileName()} at line {stackTrace.GetFrame(1).GetFileLineNumber()}");
                }
            }
        }

        public FQuaternion WorldRotation
        {
            get
            {
                if (_parent == null)
                {
                    UpdateWorldTransformCache();
                    return _cachedWorldRotation;
                }
                else
                {
                    UpdateWorldTransformCache();
                    return _cachedWorldRotation;
                }
            }
            set
            {
                if (_parent != null)
                    _localRotation = InverseTransformRotation(_parent, value); // Directly update the private value
                else
                    _localRotation = value;

                MarkWorldTransformDirty();

                if (TrackingEnabled)
                {
                    StackTrace stackTrace = new StackTrace(true);
                    UnityEngine.Debug.Log($"[FTransform] WorldRotation set to {value} for Entity {EntityData?.Name}. Called from {stackTrace.GetFrame(1).GetMethod().Name} in {stackTrace.GetFrame(1).GetFileName()} at line {stackTrace.GetFrame(1).GetFileLineNumber()}");
                }
            }
        }

        public FVector3 WorldScale
        {
            get
            {
                if (_parent == null)
                {
                    UpdateWorldTransformCache();
                    return _cachedWorldScale;
                }
                else
                {
                    UpdateWorldTransformCache();
                    return _cachedWorldScale;
                }
            }
            set
            {
                _worldScale = value; // Directly update the private value
                MarkWorldTransformDirty();

                if (TrackingEnabled)
                {
                    StackTrace stackTrace = new StackTrace(true);
                    UnityEngine.Debug.Log($"[FTransform] WorldScale set to {value} for Entity {EntityData?.Name}. Called from {stackTrace.GetFrame(1).GetMethod().Name} in {stackTrace.GetFrame(1).GetFileName()} at line {stackTrace.GetFrame(1).GetFileLineNumber()}");

                }
            }
        }


        public FVector3 LocalPosition
        {
            get
            {
                return _localPosition;
            }
            set
            {
                _localPosition = value;
                MarkWorldTransformDirty();

                if (TrackingEnabled)
                {
                    StackTrace stackTrace = new StackTrace(true);
                    UnityEngine.Debug.Log($"[FTransform] LocalPosition set to {value} for Entity {EntityData?.Name}. Called from {stackTrace.GetFrame(1).GetMethod().Name} in {stackTrace.GetFrame(1).GetFileName()} at line {stackTrace.GetFrame(1).GetFileLineNumber()}");

                }
            }
        }

        public FQuaternion LocalRotation
        {
            get
            {
                return _localRotation;
            }
            set
            {
                _localRotation = value;
                MarkWorldTransformDirty();
                if (TrackingEnabled)
                {
                    StackTrace stackTrace = new StackTrace(true);
                    UnityEngine.Debug.Log($"[FTransform] LocalRotation set to {value} for Entity {EntityData?.Name}. Called from {stackTrace.GetFrame(1).GetMethod().Name} in {stackTrace.GetFrame(1).GetFileName()} at line {stackTrace.GetFrame(1).GetFileLineNumber()}");

                }
            }
        }

        public FVector3 LocalScale
        {
            get
            {
                if (_parent == null) 
                    return WorldScale;

                return new FVector3(
                    WorldScale.X / _parent.WorldScale.X,
                    WorldScale.Y / _parent.WorldScale.Y,
                    WorldScale.Z / _parent.WorldScale.Z
                );
            }
            set
            {
                if (_parent == null) _worldScale = value;
                else _worldScale = new FVector3(
                    value.X * _parent.WorldScale.X,
                    value.Y * _parent.WorldScale.Y,
                    value.Z * _parent.WorldScale.Z
                );
                MarkWorldTransformDirty();

                if (TrackingEnabled)
                {
                    StackTrace stackTrace = new StackTrace(true);
                    UnityEngine.Debug.Log($"[FTransform] LocalScale set to {value} for Entity {EntityData?.Name}. Called from {stackTrace.GetFrame(1).GetMethod().Name} in {stackTrace.GetFrame(1).GetFileName()} at line {stackTrace.GetFrame(1).GetFileLineNumber()}");

                }
            }
        }

        /// <summary>
        /// Gets the right vector of the transform.
        /// </summary>
        public FVector3 Right => WorldRotation * FVector3.Right;

        /// <summary>
        /// Gets the left vector of the transform.
        /// </summary>
        public FVector3 Left => WorldRotation * FVector3.Left;

        /// <summary>
        /// Gets the back vector of the transform.
        /// </summary>
        public FVector3 Back => WorldRotation * FVector3.Back;

        /// <summary>
        /// Gets or sets the forward vector of the transform.
        /// </summary>
        public FVector3 Forward
        {
            get => WorldRotation * FVector3.Forward;
            set
            {
                FVector3 forward = WorldRotation * FVector3.Forward;
                if (forward == value) return;

                FQuaternion targetRotation = FQuaternion.LookRotation(value, FVector3.Up);
                _localRotation = targetRotation;
                MarkWorldTransformDirty();
            }
        }

        public ListEvent OnAddedToList { get; set; }
        public ListEvent OnRemovedFromList { get; set; }
        public int FrameAddedToSerializedList { get; set; }

        public string ListDisplayName => $"{EntityData.Name}'s Transform";

        public int FrameRemoved { get; set; }
        public FTransform Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                    return;

                _parent?.RemoveChild(this);
                _parent = value;
                _parent?.AddChild(this);
                MarkWorldTransformDirty();
            }
        }
        public int ChildCount => _children?.Count ?? 0;

        public FTransform(EntityData owner)
        {
            _localPosition = new FVector3();
            _localRotation = new FQuaternion(0, 0, 0, 1);
            _worldScale = new FVector3(1, 1, 1);
            EntityData = owner;
            _children = new SerializedListHandler<FTransform>(ListDisplayName + "Children List");
            _children.ShouldSerializeItemsIndividually = false;
            MarkWorldTransformDirty(false);
        }

        public FTransform(FVector3 position, FQuaternion rotation, FVector3 scale, EntityData owner)
        {
            EntityData = owner;
            WorldPosition = position;
            WorldRotation = rotation;
            WorldScale = scale;
            _parent = null;
            _children =  new SerializedListHandler<FTransform>(ListDisplayName + "Children List");
            _children.ShouldSerializeItemsIndividually = false;
            MarkWorldTransformDirty(false);
        }

        public FTransform GetChild(int index) => _children?[index];

        /// <summary>
        /// Adds a child to the list of children and st this transform as its parent.
        /// </summary>
        public void AddChild(FTransform child)
        {
            bool addedFirstChild = _children == null || _children.Count == 0;

            if (_children == null)
                _children = new();

            if (!_children.Contains(child))
            {
                _children.Add(child);
                child.Parent = this;
                child.MarkWorldTransformDirty();
            }

            if (addedFirstChild)
                GridGame.OnLateDeserialization += CleanChildren;
        }

        /// <summary>
        /// Removes the child from the list of children and sets its parent to null.
        /// </summary>
        public void RemoveChild(FTransform child)
        {
            if (_children != null && _children.Contains(child))
            {
                _children.Remove(child);
                child.Parent = null;
                child.MarkWorldTransformDirty();
            }
        }

        /// <summary>
        /// Marks the cached world transform as stale and propagates that state to children.
        /// </summary>
        private void MarkWorldTransformDirty(bool includeChildren = true)
        {
            _worldTransformDirty = true;

            if (!includeChildren || _children == null)
                return;

            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].MarkWorldTransformDirty();
            }
        }

        /// <summary>
        /// Rebuilds cached world position, rotation, and scale only when an upstream change has invalidated them.
        /// </summary>
        private void UpdateWorldTransformCache()
        {
            if (!_worldTransformDirty)
                return;

            if (_parent == null)
            {
                _cachedWorldPosition = _localPosition;
                _cachedWorldRotation = _localRotation;
                _cachedWorldScale = _worldScale;
            }
            else
            {
                _parent.UpdateWorldTransformCache();
                _cachedWorldPosition = TransformPoint(_parent, _localPosition);
                _cachedWorldRotation = TransformRotation(_parent, _localRotation);
                _cachedWorldScale = FVector3.Scale(_parent.WorldScale, _worldScale);
            }

            _worldTransformDirty = false;
        }

        /// <summary>
        /// Set the childrens parents to this after deserialization just in case the child had a different parent this frame.
        /// </summary>
        private void CleanChildren(BinaryReader br)
        {
            //If this transform doesn't have children just frame remove the extra call.
            if (_children == null || _children.Count == 0)
            {
                GridGame.OnLateDeserialization -= CleanChildren;
                return;
            }

            //Make sure all the children know who the pappy is.
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Parent = this;
            }
        }


        public static FVector3 TransformPoint(FTransform transform, FVector3 point)
        {
            return transform.WorldPosition + transform.WorldRotation * FVector3.Scale(transform.WorldScale, point);
        }

        public static FVector3 InverseTransformPoint(FTransform transform, FVector3 point)
        {
            FVector3 subtraction = point - transform.WorldPosition;
            FVector3 unrotatedPoint = FQuaternion.Inverse(transform.WorldRotation) * subtraction;

            return new FVector3(
                unrotatedPoint.X / transform.WorldScale.X,
                unrotatedPoint.Y / transform.WorldScale.Y,
                unrotatedPoint.Z / transform.WorldScale.Z
            );
        }

        public static FQuaternion TransformRotation(FTransform transform, FQuaternion rotation)
        {
            FQuaternion worldRotation = transform.WorldRotation * rotation;
            worldRotation.Normalize();
            return worldRotation;
        }

        public static FQuaternion InverseTransformRotation(FTransform transform, FQuaternion rotation)
        {
            FQuaternion localRotation = FQuaternion.Inverse(transform.WorldRotation) * rotation;
            localRotation.Normalize();
            return localRotation;
        }

        internal void SetPositionAndRotation(FVector3 position, FQuaternion rotation)
        {
            WorldPosition = position;
            WorldRotation = rotation;
        }
        /// <summary>
        /// Rotates the transform to look at a target point in world space.
        /// </summary>
        /// <param name="target">The point to look at.</param>
        /// <param name="up">The up direction to use. Defaults to FVector3.Up.</param>
        public void LookAt(FVector3 target)
        {
            FVector3 direction = (target - WorldPosition).GetNormalized();
            if (direction == FVector3.Zero)
                return;

            WorldRotation = FQuaternion.LookRotation(direction, FVector3.Up);
        }

        public void Serialize(BinaryWriter bw)
        {
            _localPosition.Serialize(bw);
            _localRotation.Serialize(bw);
            LocalScale.Serialize(bw);
        }

        public void Deserialize(BinaryReader br)
        {
            FVector3 localPosition = LocalPosition;
            localPosition = localPosition.Deserialize(br);
            _localPosition = localPosition;

            FQuaternion localRotation = LocalRotation;
            localRotation = localRotation.Deserialize(br);
            _localRotation = localRotation;

            FVector3 localScale = LocalScale;
            localScale = localScale.Deserialize(br);
            LocalScale = localScale;
        }

        public void OnLogGameState(StringBuilder sb)
        {
            sb.AppendLine($"Entity: {EntityData?.Name}, LocalPosition: {LocalPosition}, LocalRotation: {LocalRotation}, LocalScale: {LocalScale}");
        }
        public bool CheckIfCanBeAddedToList()
        {
            return true;
        }

        public void OnSerialize(BinaryWriter bw)
        {
            //This is told to serialize by the entity that its attached to. Not by the serialized list handler.
        }

        public void OnDeserialize(BinaryReader br)
        {
            //This is told to serialize by the entity that its attached to. Not by the serialized list handler.
        }
    }
}
