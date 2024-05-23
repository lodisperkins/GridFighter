using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounterBehaviour : MonoBehaviour
{
    [SerializeField]
    private Text _text;

    // Update is called once per frame
    void Update()
    {
        _text.text = "FPS: " + (1 / Time.deltaTime);
    }
}
