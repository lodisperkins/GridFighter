
using System;
using System.Collections.Generic;
using System.IO;

namespace FixedPoints
{
    /// <summary>
    /// A transform that functions similar to Unity's transform but uses fixed point math.
    /// </summary>
    public class FTransform
    {
        public FVector3 Position { get; set; }
        public FQuaternion Rotation { get; set; }
        public FVector3 Scale { get; set; }

        private FTransform parent;
        private List<FTransform> children;

        public FTransform()
        {
            Position = new FVector3();
            Rotation = new FQuaternion(0,0,0,1);
            Scale = new FVector3(1,1,1);    
        }

        public FTransform(FVector3 position, FQuaternion rotation, FVector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
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

                if (parent != null)
                    parent.RemoveChild(this);

                parent = value;

                if (parent != null)
                    parent.AddChild(this);
            }
        }

        public int ChildCount => children.Count;

        public FTransform GetChild(int index)
        {
            return children[index];
        }

        public void AddChild(FTransform child)
        {
            if (!children.Contains(child))
            {
                children.Add(child);
                child.Parent = this;
            }
        }

        public void RemoveChild(FTransform child)
        {
            if (children.Contains(child))
            {
                children.Remove(child);
                child.Parent = null;
            }
        }

        public void Serialize(BinaryWriter bw)
        {
            Position.Serialize(bw);
            Rotation.Serialize(bw);
            Scale.Serialize(bw);
        }

        public void Deserialize(BinaryReader br)
        {
            Position.Deserialize(br);
            Rotation.Deserialize(br);
            Scale.Deserialize(br);
        }

        public FVector3 LocalPosition
        {
            get
            {
                if (parent == null) return Position;
                return FTransform.InverseTransformPoint(parent, Position);
            }
            set
            {
                if (parent == null) Position = value;
                else Position = FTransform.TransformPoint(parent, value);
            }
        }

        public FQuaternion LocalRotation
        {
            get
            {
                if (parent == null) return Rotation;
                return FTransform.InverseTransformRotation(parent, Rotation);
            }
            set
            {
                if (parent == null) Rotation = value;
                else Rotation = FTransform.TransformRotation(parent, value);
            }
        }

        public FVector3 LocalScale
        {
            get
            {
                if (parent == null) return Scale;
                return new FVector3(
                    Scale.X / parent.Scale.X,
                    Scale.Y / parent.Scale.Y,
                    Scale.Z / parent.Scale.Z
                );
            }
            set
            {
                if (parent == null) Scale = value;
                else Scale = new FVector3(
                    value.X * parent.Scale.X,
                    value.Y * parent.Scale.Y,
                    value.Z * parent.Scale.Z
                );
            }
        }

        public static FVector3 TransformPoint(FTransform transform, FVector3 point)
        {
            return transform.Position + transform.Rotation * FVector3.Scale(transform.Scale, point);
        }

        public static FVector3 InverseTransformPoint(FTransform transform, FVector3 point)
        {
            FVector3 subtraction = (point - transform.Position);
            FVector3 result = new FVector3(subtraction.X / transform.Scale.X, subtraction.Y / transform.Scale.Y, subtraction.Z / transform.Scale.Z);
            return FQuaternion.Inverse(transform.Rotation) * result;
        }

        public static FQuaternion TransformRotation(FTransform transform, FQuaternion rotation)
        {
            return transform.Rotation * rotation;
        }

        public static FQuaternion InverseTransformRotation(FTransform transform, FQuaternion rotation)
        {
            return FQuaternion.Inverse(transform.Rotation) * rotation;
        }

        /// <summary>
        /// Gets or sets the forward vector of the transform.
        /// </summary>
        public FVector3 Forward
        {
            get => Rotation * FVector3.Forward;
            set
            {
                FVector3 forward = Rotation * FVector3.Forward;
                if (forward == value) return;

                FQuaternion targetRotation = FQuaternion.LookRotation(value, FVector3.Up);
                Rotation = targetRotation;
            }
        }

        /// <summary>
        /// Gets the right vector of the transform.
        /// </summary>
        public FVector3 Right => Rotation * FVector3.Right;

        /// <summary>
        /// Gets the left vector of the transform.
        /// </summary>
        public FVector3 Left => Rotation * FVector3.Left;

        /// <summary>
        /// Gets the back vector of the transform.
        /// </summary>
        public FVector3 Back => Rotation * FVector3.Back;
    }


}