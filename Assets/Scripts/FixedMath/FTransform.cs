using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FixedPoints
{
    /// <summary>
    /// A transform that functions similar to Unity's transform but uses fixed-point math.
    /// </summary>
    [Serializable]
    public class FTransform
    {
        [SerializeField] private FVector3 _localPosition;
        [SerializeField] private FQuaternion _localRotation;
        [SerializeField] private FVector3 _worldScale;
        public EntityData Entity { get; private set; }

        private FTransform parent;
        private List<FTransform> children;

        public FTransform(EntityData owner)
        {
            _localPosition = new FVector3();
            _localRotation = new FQuaternion(0, 0, 0, 1);
            _worldScale = new FVector3(1, 1, 1);
            Entity = owner;
        }

        public FTransform(FVector3 position, FQuaternion rotation, FVector3 scale, EntityData owner)
        {
            Entity = owner;
            WorldPosition = position;
            WorldRotation = rotation;
            WorldScale = scale;
            parent = null;
            children = new List<FTransform>();
        }

        public FTransform Parent
        {
            get => parent;
            set
            {
                if (parent == value)
                    return;

                parent?.RemoveChild(this);
                parent = value;
                parent?.AddChild(this);
            }
        }

        public int ChildCount => children?.Count ?? 0;

        public FTransform GetChild(int index) => children?[index];

        public void AddChild(FTransform child)
        {
            if (children == null)
                children = new List<FTransform>();

            if (!children.Contains(child))
            {
                children.Add(child);
                child.Parent = this;
            }
        }

        public void RemoveChild(FTransform child)
        {
            if (children != null && children.Contains(child))
            {
                children.Remove(child);
                child.Parent = null;
            }
        }

        public void Serialize(BinaryWriter bw)
        {
            WorldPosition.Serialize(bw);
            WorldRotation.Serialize(bw);
            WorldScale.Serialize(bw);
        }

        public void Deserialize(BinaryReader br)
        {
            WorldPosition.Deserialize(br);
            WorldRotation.Deserialize(br);
            WorldScale.Deserialize(br);
        }

        public FVector3 WorldPosition
        {
            get
            {
                if (parent == null)
                {
                    return _localPosition;
                }
                else
                {
                    // World position is the parent's world position plus the local position transformed by the parent's rotation and scale
                    return TransformPoint(Parent, LocalPosition);
                }
            }
            set
            {
                if (parent != null)
                    _localPosition = InverseTransformPoint(parent, value); // Directly update the private value
                else
                    _localPosition = value;
            }
        }

        public FQuaternion WorldRotation
        {
            get
            {
                if (parent == null)
                {
                    return _localRotation;
                }
                else
                {
                    // World rotation is the parent's world rotation combined with the local rotation
                    return TransformRotation(Parent, LocalRotation);
                }
            }
            set
            {
                if (parent != null)
                    _localRotation = InverseTransformRotation(parent, value); // Directly update the private value
                else
                    _localRotation = value;
            }
        }

        public FVector3 WorldScale
        {
            get
            {
                if (parent == null)
                {
                    return _worldScale;
                }
                else
                {
                    // World scale is the parent's scale multiplied by the local scale
                    return FVector3.Scale(parent.WorldScale, _worldScale);
                }
            }
            set
            {
                _worldScale = value; // Directly update the private value
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
            }
        }

        public FVector3 LocalScale
        {
            get
            {
                if (parent == null) return WorldScale;
                return new FVector3(
                    WorldScale.X / parent.WorldScale.X,
                    WorldScale.Y / parent.WorldScale.Y,
                    WorldScale.Z / parent.WorldScale.Z
                );
            }
            set
            {
                if (parent == null) _worldScale = value;
                else _worldScale = new FVector3(
                    value.X * parent.WorldScale.X,
                    value.Y * parent.WorldScale.Y,
                    value.Z * parent.WorldScale.Z
                );
            }
        }

        public static FVector3 TransformPoint(FTransform transform, FVector3 point)
        {
            return transform.WorldPosition + transform.WorldRotation * FVector3.Scale(transform.WorldScale, point);
        }

        public static FVector3 InverseTransformPoint(FTransform transform, FVector3 point)
        {
            FVector3 subtraction = point - transform.WorldPosition;
            FVector3 result = new FVector3(subtraction.X / transform.WorldScale.X, subtraction.Y / transform.WorldScale.Y, subtraction.Z / transform.WorldScale.Z);
            return FQuaternion.Inverse(transform.WorldRotation) * result;
        }

        public static FQuaternion TransformRotation(FTransform transform, FQuaternion rotation)
        {
            return transform.WorldRotation * rotation;
        }

        public static FQuaternion InverseTransformRotation(FTransform transform, FQuaternion rotation)
        {
            return FQuaternion.Inverse(transform.WorldRotation) * rotation;
        }

        internal void SetPositionAndRotation(FVector3 position, FQuaternion rotation)
        {
            WorldPosition = position;
            WorldRotation = rotation;
        }

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
    }
}
