using System;
using System.Collections;
using System.Collections.Generic;
using GridGame;
using UnityEngine;

namespace GridGame.GamePlay
{
	public class RobotSpecialBehaviour : MonoBehaviour
    {
    	private Rigidbody RobotBody;
        public Event OnSpecialAbilityActivated;
        public Event OnSpecialAbilityDeactivated;
       
        public bool SpecialAbilityReady;
        [SerializeField] private TeleportBeamBehaviour _teleportBeam;
    	public float tackleForce;
    	// Use this for initialization
    	void Start ()
    	{
    		RobotBody = GetComponent<Rigidbody>();
            SpecialAbilityReady = false;
        }

        public void ToggleSpecialAbility()
        {
	        SpecialAbilityReady = !SpecialAbilityReady;
        }
        
    	public void RollAttack()
    	{
	        if (SpecialAbilityReady)
	        {
		        RobotBody.AddForce(0,0,tackleForce);
		        Debug.Log("try tackle");
		        OnSpecialAbilityActivated.Raise(gameObject);
	        }
	        
    	}
    
    	public void RollRecoil()
    	{
    		RobotBody.AddForce(0,0,-tackleForce/2);
    	}
    
//    	private void OnTriggerEnter(Collider other)
//    	{
//    		var health = other.GetComponent<HealthBehaviour>();
//            if(health != null)
//            {   
//	            if (other.CompareTag("Block"))
//    			{
//    				other.SendMessage("DestroyBlock",2);
//                    health.playDeathParticleSystems(2);
//    			}
//    			else if (other.CompareTag("Player") || other.CompareTag("Core")) 
//    			{
//    				health.takeDamage(0);
//	                OnSpecialAbilityDeactivated.Raise(gameObject);
//	                _teleportBeam.ascending = true;
//    			}
//            }
//    	}
    }

}

