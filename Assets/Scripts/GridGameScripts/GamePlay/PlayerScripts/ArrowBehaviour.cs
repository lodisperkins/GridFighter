using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowBehaviour : MonoBehaviour
{
	
	[SerializeField] private  SpriteRenderer _arrow;
	private float _time;
	[SerializeField]
	private float _timeOffset;
	
	[SerializeField] private float _arrowRotationOffset;
	private bool _showTemp;
	// Use this for initialization
	void Start ()
	{
		_showTemp = false;
		
	}

	public void RotateArrow(int blockRotationDegrees)
	{
		float rotation = blockRotationDegrees + _arrowRotationOffset;
		_arrow.transform.rotation = Quaternion.Euler(_arrow.transform.eulerAngles.x,rotation, _arrow.transform.eulerAngles.z);
	}

	public void ShowArrow()
	{
		_arrow.enabled = true;
	}

	public void HideArrow()
	{
		if (_showTemp)
		{
			return;
		}
		_arrow.enabled = false;
	}

	public void ShowArrowTemporarily(int blockRotationDegrees)
	{
		_time = Time.time +_timeOffset;
		_showTemp = true;
	}
	// Update is called once per frame
	void Update () {
		if (_showTemp)
		{
			ShowArrow();
			if (Time.time >=_time )
			{
				HideArrow();
				_showTemp = false;
			}
		}
	}
}
