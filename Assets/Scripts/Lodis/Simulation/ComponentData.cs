
using FixedPoints;
using System.IO;

[System.Serializable]
public class ComponentData
{
    public EntityData Owner;

    public FTransform OwnerTransform { get => Owner.Transform; }

    public virtual void Init() { }

    public virtual void Serialize(BinaryWriter bw) { }
    public virtual void Deserialize(BinaryReader br) { }

    public virtual void Update(float dt) 
    {
    }

    public virtual void OnCollision(Collision collision) { }
}