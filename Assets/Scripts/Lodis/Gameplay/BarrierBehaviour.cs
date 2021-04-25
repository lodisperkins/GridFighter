using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierBehaviour : MonoBehaviour
{
    private Material _material;
    [SerializeField]
    private string[] _visibleLayers;

    // Start is called before the first frame update
    void Start()
    {
        _material = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        int layerMask = LayerMask.GetMask(_visibleLayers);

        if (Physics.Raycast(transform.position, Vector3.forward, 1, layerMask))
            _material.color = new Color(1,1,1,0.5f);
        else
            _material.color = new Color(1, 1, 1, 1);
    }
}
