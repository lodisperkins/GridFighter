using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintOnAwake : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("Awake called on " + gameObject.name);
    }
}
