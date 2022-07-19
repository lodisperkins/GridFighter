using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testjump : MonoBehaviour
{
    public float distance;
    public float height;
    public float duration;
    public Ease ease;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().DOJump(Vector3.right * distance, height, 1, duration, false).SetEase(ease);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
