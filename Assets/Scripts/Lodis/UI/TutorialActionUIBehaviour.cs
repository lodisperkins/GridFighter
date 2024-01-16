using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialActionUIBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _images;

    public void SetImageActive(string name)
    {
        foreach(GameObject image in _images)
        {
            image.SetActive(image.name == name);
        }
    }
}
