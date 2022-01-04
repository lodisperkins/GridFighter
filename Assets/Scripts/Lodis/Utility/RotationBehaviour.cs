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

    public bool RotateOnSelf { get => _rotateOnSelf; set => _rotateOnSelf = value; }

    // Use this for initialization
    void Start()
    {
        _axis = _axis * _speed;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0)
        {
            return;
        }
        if (RotateOnSelf)
        {
            transform.Rotate(_axis, Space.Self);
            return;
        }
        transform.Rotate(_axis, Space.World);
    }
}
