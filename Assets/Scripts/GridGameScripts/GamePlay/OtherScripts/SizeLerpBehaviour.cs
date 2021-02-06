using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeLerpBehaviour : MonoBehaviour {
    [SerializeField]
    private Vector3 size1;
    [SerializeField]
    private Vector3 size2;
    [SerializeField]
    private float lerpRate;
    private float lerpVal;
	// Use this for initialization
	void Start () {
        lerpVal = 0;
	}
	// Update is called once per frame
	void Update () {
        transform.localScale = Vector3.Lerp(size1, size2,lerpVal);
        lerpVal += lerpRate * Time.deltaTime;
        if(lerpVal >= 1)
        {
            Vector3 temp = size1;
            size1 = size2;
            size2 = temp;
            lerpVal = 0;
        }
	}
}
