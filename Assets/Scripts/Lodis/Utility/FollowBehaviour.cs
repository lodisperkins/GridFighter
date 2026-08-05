using FixedPoints;
using Lodis.Movement;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;

public class FollowBehaviour : SimulationBehaviour
{
    [SerializeField] private bool _useSimulationUpdate = true;

    [ShowIf("_useSimulationUpdate")]
    [SerializeField] private EntityDataBehaviour _target;
    [HideIf("_useSimulationUpdate")]
    [SerializeField] private GameObject _unityTarget;

    [SerializeField] private Fixed32 _speed;
    [SerializeField] private bool _snapToTarget;
    [SerializeField] private bool _disableOnComplete;
    [SerializeField] private bool _resetOnComplete;
    [SerializeField] private bool _resetOnEnable;
    [SerializeField] private UnityEvent _onComplete;

    //---
    private GridPhysicsBehaviour _physics;
    private bool _completed;
    private Fixed32 _completeThreshold = Fixed32.One / 100; // Small threshold to avoid jittering when snapping
    private Vector3 _unityStart;
    private FVector3 _simStart;

    public EntityDataBehaviour Target { get => _target; set => _target = value; }

    /// <summary>
    /// Whether or not the entity has reached its target.
    /// </summary>
    public bool Completed
    {
        get => _completed;
        private set
        {
            if (_completed == value)
                return;

            //Disable on complete if we want to.
            if (value && _disableOnComplete)
            {
                gameObject.SetActive(false);
            }

            //Reset based on the simulation or unity position
            if (_resetOnComplete && value)
            {
                if (_useSimulationUpdate)
                    FixedTransform.WorldPosition = _simStart;
                else
                    transform.position = _unityStart;
            }

            //Be sure to only call the event once.
            if (value && !_completed)
                _onComplete?.Invoke();

            _completed = value;
        }
    }

    public override string LogName => "FollowBehaviour";

    private void OnEnable()
    {
        if (_resetOnEnable && !_useSimulationUpdate)
        {
            //Reset the position to the start position if we are using unity stuff.
            transform.position = _unityStart;
        }
        else if (_resetOnEnable && _useSimulationUpdate)
        {
            //Reset the position to the start position if we are using simulation stuff.
            FixedTransform.WorldPosition = _simStart;
        }
    }

    private void Start()
    {
        //Initialize the start position for reset if we are using unity stuff.
        if (_useSimulationUpdate)
            return;

        _completed = false;
        _unityStart = transform.position;
    }

    public override void Begin()
    {
        // Initialize the start position for reset if we are using simulation stuff.
        base.Begin();
        _completed = false;
        _simStart = FixedTransform.WorldPosition;
        TryGetComponent<GridPhysicsBehaviour>(out _physics);
    }

    public override void Tick(Fixed32 dt)
    {
        base.Tick(dt);

        //Return if we don't have a target or if the target is inactive OR if we shouldn't be using simulation stuff.
        if (Target == null || !Target.Active || !_useSimulationUpdate)
            return;

        //Check if we reached the target.
        Completed = (FixedTransform.WorldPosition - Target.FixedTransform.WorldPosition).Magnitude < _completeThreshold;

        if (Completed)
            return;

        //Move towards the target.
        if (_snapToTarget)
        {
            FixedTransform.WorldPosition = Target.FixedTransform.WorldPosition;
        }
        else
        {
            FVector3 toTarget = Target.FixedTransform.WorldPosition - FixedTransform.WorldPosition;
            FVector3 velocity = toTarget.GetNormalized() * _speed * dt;

            // Follow the target with a fixed speed

            if (_physics)
                _physics.ApplyVelocityChange(velocity);
            else
                FixedTransform.WorldPosition += velocity;
        }
    }

    private void Update()
    {
        //Return if we don't have a target or if the target is inactive OR if we should be using simulation stuff.
        if (_unityTarget == null || !_unityTarget.activeInHierarchy|| _useSimulationUpdate)
            return;

        //Check if we reached the target.
        Completed = (transform.position - _unityTarget.transform.position).magnitude < _completeThreshold;

        if (Completed)
            return;

        //Move towards the target.
        if (_snapToTarget)
        {
            transform.position = _unityTarget.transform.position;
        }
        else
        {
            Vector3 toTarget = _unityTarget.transform.position - transform.position;
            Vector3 velocity = toTarget.normalized * _speed * Time.deltaTime;

            // Follow the target with a fixed speed
            transform.position += velocity;
        }
    }

    public override void Deserialize(BinaryReader br)
    {
        _simStart = _simStart.Deserialize(br);
    }

    public override void Serialize(BinaryWriter bw)
    {
        _simStart.Serialize(bw);
    }

    /// <summary>
    /// Hashes the serialized follow target state so replay mismatches caused by this
    /// helper behavior can be isolated quickly.
    /// </summary>
    protected override string[] GetLogItems()
    {
        return new string[] { "Sim Start Position: " + _simStart };
    }
}
