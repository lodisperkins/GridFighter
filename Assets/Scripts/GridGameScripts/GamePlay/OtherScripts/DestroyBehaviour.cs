using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyBehaviour : MonoBehaviour {
    GameObject temp;
	// Use this for initialization
	void Start () {
        temp = gameObject;
	}
	
	// Update is called once per frame
	void Update () {
        Destroy(temp, 0.001f);
	}
}
