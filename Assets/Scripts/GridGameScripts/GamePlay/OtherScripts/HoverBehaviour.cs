using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverBehaviour : MonoBehaviour {
    [SerializeField]
    private float intensity;
	
	// Update is called once per frame
	void Update () {
        Vector3 hoverPos = (Vector3.up * Mathf.Cos(Time.time))* intensity;
        transform.position = new Vector3(transform.position.x, hoverPos.y + transform.position.y, transform.position.z) ;
	}
}
