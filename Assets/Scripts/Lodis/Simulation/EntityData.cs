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
    private GridCollider[] _gridColliders;

    public string Name;
    public FTransform Transform;
    public int X;
    public int Y;

    public GridCollider[] Colliders 
    { 
        get { return _gridColliders; }
        set
        {
            foreach (GridCollider col in value)
            {
                col.OnOverlapEnter += OnOverlapEnter;
                col.OnOverlapStay += OnOverlapStay;
                col.OnOverlapExit += OnOverlapExit;
                col.OnCollisionEnter += OnCollisionEnter;
                col.OnCollisionStay += OnCollisionStay;
                col.OnCollisionExit += OnCollisionExit;
            }

            _gridColliders = value;


            if (_gridColliders != null && _gridColliders.Length > 0 && Active)
                GridGame.AddPhysicsEntity(this);

            if (_gridColliders == null)
                GridGame.RemovePhysicsEntity(this);
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
    public event EntityUpdateEvent OnLateTick;

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

        foreach (var col in _gridColliders)
        {
            col?.Serialize(bw);
        }
        Transform.Serialize(bw);
    }

    public virtual void Deserialize(BinaryReader br)
    {
        Name = br.ReadString();

        foreach (var col in _gridColliders)
        {
            col?.Deserialize(br);
        }

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

        if (_gridColliders != null && _gridColliders.Length > 0)
            GridGame.AddPhysicsEntity(this);

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

    public void LateTick(Fixed32 dt)
    {
        for (int i = 0; i < _components.Count; i++)
        {
            _components[i].LateTick(dt);
        }

        OnLateTick?.Invoke(dt);
    }

    public void End()
    {
        Active = false;

        GridGame.RemovePhysicsEntity(this);

        if (_gridColliders != null)
        {
            foreach (GridCollider col in _gridColliders)
            {
                col.ClearCollisionExit();
            }
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
        T comp = UnityObject.AddComponent<T>();
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

    public T[] GetComponentsInChildren<T>() where T : SimulationBehaviour
    {
        //Create a list so that components can be easily added when found.
        List<T> componentsFound = new List<T>();

        //Find the component attached to this entity.
        T comp = (T)_components.Find(c => c.GetType() == typeof(T));
        componentsFound.Add(comp);

        //Go through all the children and grab all of their components.
        for (int i = 0; i < Transform.ChildCount; i++)
        {
            //Recursively calls the function so the game object adds its component and the components of its children.
            T[] childComponentsFound = Transform.GetChild(i).Entity.GetComponentsInChildren<T>();
            componentsFound.AddRange(childComponentsFound);
        }

        //Return an array for easy iteration.
        return componentsFound.ToArray();
    }

    public T GetComponentInChildren<T>(bool includeParent = true) where T : SimulationBehaviour
    {
        //Find the component attached to this entity.
        T comp = null;
        if (includeParent)
        {
            comp = (T)_components.Find(c => c.GetType() == typeof(T));

            if (comp != null)
                return comp;
        }

        //Go through all the children and grab find the component.
        for (int i = 0; i < Transform.ChildCount; i++)
        {
            //Recursively calls the function so the game object searches its components and the components of its children.
            comp = Transform.GetChild(i).Entity.GetComponentInChildren<T>();

            if (comp != null)
                return comp;
        }

        //Return an array for easy iteration.
        return null;
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