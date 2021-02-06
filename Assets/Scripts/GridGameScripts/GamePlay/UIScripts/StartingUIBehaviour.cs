using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame
{
	public class StartingUIBehaviour : MonoBehaviour
    {
    
    	private Vector3 _startingPosition;
    	private float _xPosition;
    	private Vector3 _endPosition;
        [SerializeField] private Event onHalfwayThere;
    	[SerializeField] private Event onEndPosition;
    	// Use this for initialization
    	void Start ()
    	{
    		_startingPosition = transform.position;
    		_endPosition = new Vector3(_startingPosition.x+50,_startingPosition.y,_startingPosition.z);
    	}
    	
    	// Update is called once per frame
    	void Update ()
    	{
    		if (_xPosition >= .3 && _xPosition <= .6)
    		{
    			_xPosition += .2f * Time.deltaTime;
                onHalfwayThere.Raise(gameObject);
    		}
    		else
    		{
    			 _xPosition+= 1 * Time.deltaTime;
    		}
    		transform.position = Vector3.Lerp(_startingPosition, _endPosition, _xPosition);
    		if (_xPosition >= .7)
    		{
    			onEndPosition.Raise(gameObject);
    		}
    	}
    }
}

