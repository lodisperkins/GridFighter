using FixedPoints;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

/// <summary>
/// Contains info pertaining to an entity in the rollback simulation.
/// </summary>
[System.Serializable]
public class EntityData
{
    private readonly List<SimulationBehaviour> _components = new();
    private bool _active;
    private GridCollider _gridCollider;

    public string Name;
    public FTransform Transform;
    public int X;
    public int Y;

    public GridCollider Collider 
    { 
        get { return _gridCollider; }
        set
        {
            value.OnOverlapEnter += OnOverlapEnter;
            value.OnOverlapStay += OnOverlapStay;
            value.OnOverlapExit += OnOverlapExit;
            value.OnCollisionEnter += OnCollisionEnter;
            value.OnCollisionStay += OnCollisionStay;
            value.OnCollisionExit += OnCollisionExit;

            _gridCollider = value;
        }
    }

    public GameObject UnityObject;

    public bool Active 
    { 
        get => _active; 
        set
        {
            _active = value;

            if (UnityObject)
                UnityObject.SetActive(value);
        }
    }

    public delegate void EntityUpdateEvent(Fixed32 dt);
    public event EntityUpdateEvent OnTick;

    public delegate void EntityGameEvent();
    public event EntityGameEvent OnInit;
    public event EntityGameEvent OnBegin;
    public event EntityGameEvent OnEnd;

    public EntityData()
    {
        Name = "New Entity";
        Transform = new FTransform(this);
        Init();
    }

    public EntityData(string name) : this()
    {
        Name = name;
    }

    public virtual void Serialize(BinaryWriter bw)
    {
        bw.Write(Name);
        Collider?.Serialize(bw);
        Transform.Serialize(bw);
    }

    public virtual void Deserialize(BinaryReader br)
    {
        Name = br.ReadString();
        Collider?.Deserialize(br);
        Transform.Deserialize(br);
    }

    public void Init()
    {
        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].Init();
        }

        OnInit?.Invoke();
    }

    public void Begin()
    {
        Active = true;

        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].Begin();
        }

        OnBegin?.Invoke();
    }

    public void Tick(Fixed32 dt)
    {
        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].Tick(dt);
        }

        OnTick?.Invoke(dt);
    }

    public void End()
    {
        Active = false;

        if (Collider != null)
        {
            Collider.ClearCollisionExit();
        }

        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].End();
        }

        OnEnd?.Invoke();
    }

    public T AddComponent<T>(T comp) where T : SimulationBehaviour
    {
        _components.Add(comp);
        comp.Init();

        return comp;
    }

    public T AddComponent<T>() where T : SimulationBehaviour, new()
    {
        T comp = new();
        comp.Init();

        return comp;
    }

    public void RemoveComponent<T>() where T : SimulationBehaviour
    {
        T comp = (T)_components.Find(c => c.GetType() == typeof(T));

        _components.Remove(comp);
    }

    public void RemoveComponent<T>(T comp) where T : SimulationBehaviour
    {
        _components.Remove(comp);
    }

    public T GetComponent<T>() where T : SimulationBehaviour
    {

        T comp = (T)_components.Find(c => c.GetType() == typeof(T));

        return comp;
    }

    public bool HasComponent<T>() where T : SimulationBehaviour
    {
        return _components.Find(c => c.GetType() == typeof(T)) != null;
    }

    public void OnCollisionEnter(Collision collision)
    {
        foreach (var comp in _components)
        {
            comp.OnHitEnter(collision);
        }
    }

    public void OnCollisionStay(Collision collision)
    {
        foreach (var comp in _components)
        {
            comp.OnHitStay(collision);
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        foreach (var comp in _components)
        {
            comp.OnHitExit(collision);
        }
    }

    public void OnOverlapEnter(Collision collision)
    {
        foreach (var comp in _components)
        {
            comp.OnOverlapEnter(collision);
        }
    }
    public void OnOverlapStay(Collision collision)
    {
        foreach (var comp in _components)
        {
            comp.OnOverlapStay(collision);
        }
    }
    public void OnOverlapExit(Collision collision)
    {
        foreach (var comp in _components)
        {
            comp.OnOverlapExit(collision);
        }
    }

    public static implicit operator EntityData(EntityDataBehaviour entity) => entity.Data;
}