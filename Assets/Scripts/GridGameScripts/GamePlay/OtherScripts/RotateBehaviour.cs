using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBehaviour : MonoBehaviour {
    Quaternion quaternion;
    [SerializeField]
    private Vector3 axis;
    [SerializeField]
    private float speed;
    public bool rotateOnSelf;
	// Use this for initialization
	void Start () {
		axis = axis * speed;
	}
	
	// Update is called once per frame
	void Update () {
        if(Time.timeScale == 0)
        {
            return;
        }
        if(rotateOnSelf)
        {
            transform.Rotate(axis, Space.Self);
            return;
        }
        transform.Rotate(axis, Space.World);
	}
}
