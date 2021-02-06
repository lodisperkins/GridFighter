using System;
using System.Collections;
using System.Collections.Generic;
using GridGame;
using UnityEngine;

namespace GridGame.GamePlay
{
	public class SorcererSpecialBehaviour : MonoBehaviour
    {
    	private Rigidbody SorcererBody;
        public Event OnSpecialAbilityActivated;
        public Event OnSpecialAbilityDeactivated;
        private RoutineBehaviour[] _timers;
        public bool SpecialAbilityReady;
    	// Use this for initialization
    	void Start ()
    	{
            SpecialAbilityReady = false;
            _timers = GetComponents<RoutineBehaviour>();
        }

        public void ToggleSpecialAbility()
        {
	        SpecialAbilityReady = !SpecialAbilityReady;
        }
        
    	public void FreezeEnemy()
    	{
	        if (SpecialAbilityReady)
	        {
		        _timers[1].enabled = true;
		        Debug.Log("Tried Freeze");
		        OnSpecialAbilityActivated.Raise(gameObject);
	        }
    	}

        public void DisableFreeze()
        {
	        _timers[0].ResetActions();
	        _timers[1].enabled = false;
        }
    	// Update is called once per frame
    	
    	void Update () {
    		
    	}
    }

}

