using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverdriveToggleBehaviour : MonoBehaviour
{

	[SerializeField] private Image _toggleOnImage;
	[SerializeField] private GameObject _particles;
	[SerializeField] private OverdriveBehaviour _enabled;
	// Use this for initialization
	void Start ()
	{
	}
	
	// Update is called once per frame
	void Update () {
		if (_enabled.used)
		{
			_toggleOnImage.enabled = false;
		}
	}
}
