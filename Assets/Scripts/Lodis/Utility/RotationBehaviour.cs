using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationBehaviour : MonoBehaviour
{
    [SerializeField]
    private Vector3 _axis;
    [SerializeField]
    private float _speed;
    [SerializeField]
    private bool _rotateOnSelf;
    [SerializeField]
    private bool _resetOnDisable;
    private Quaternion _startRotation;

    public bool RotateOnSelf { get => _rotateOnSelf; set => _rotateOnSelf = value; }
    public float Speed { get => _speed; set => _speed = value; }
    public Vector3 Axis { get => _axis; set => _axis = value; }

    // Use this for initialization
    void Start()
    {
        Axis = Axis * Speed;
        _startRotation = transform.rotation;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Time.timeScale == 0)
        {
            return;
        }
        if (RotateOnSelf)
        {
            transform.Rotate(Axis, Space.Self);
            return;
        }
        transform.Rotate(Axis, Space.World);
    }

    private void OnDisable()
    {
        if (_resetOnDisable)
            transform.rotation = _startRotation; 
    }
}
