using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


namespace GridGame.GamePlay
{
	public class TeleportBeamBehaviour : MonoBehaviour {
		[FormerlySerializedAs("Target")] [SerializeField]
		public string target;

		[SerializeField] private GameObject itemToTeleport; 
		public Transform objectTransform;
        private Vector3 objectDestination;
		private Vector3 _beamdestination;
		[SerializeField] private float _beamHeight;
		[SerializeField]
		private Vector3 velocity;
		[SerializeField]
		private int max_speed;
		[SerializeField]
		private Event _onAscending;
		[SerializeField]
		private Event _onLanding ;
		public bool ascending;
		public bool landed =true;
		private Rigidbody _body;
		// Use this for initialization
		void Start()
		{
			_body = GetComponent<Rigidbody>();
			objectTransform = itemToTeleport.transform;
			ascending = false;
			landed = true;
		}
		void MoveBeamUpwards()
		{
			_onAscending.Raise(gameObject);
			_beamdestination = new Vector3(objectTransform.position.x,_beamHeight,objectTransform.position.z);
			Vector3 seekforce = _beamdestination - transform.position;
			seekforce = (seekforce.normalized*max_speed) - velocity;
			if (velocity.magnitude > max_speed)
			{
				velocity=velocity.normalized*max_speed;
			}
			transform.position += seekforce*Time.deltaTime;
			if (transform.position.y >= _beamdestination.y)
			{
                if(itemToTeleport.CompareTag("Player"))
                {
                    itemToTeleport.SendMessage("ResetPositionToStartPanel", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    itemToTeleport.transform.position = objectDestination;
                }
				@ascending = false;
			}
			
		}
		void MoveBeamDownwards()
		{
            if(objectTransform == null)
            {
                return;
            }
			Vector3 seekforce = objectTransform.position- transform.position;
			seekforce = (seekforce.normalized*max_speed) - velocity;
			if (velocity.magnitude > max_speed)
			{
				velocity=velocity.normalized*max_speed;
			}
			transform.position += seekforce*Time.deltaTime;
			if (transform.position.y < objectTransform.position.y && @ascending == false)
			{
				landed = true;
				_onLanding.Raise(gameObject);
			}
			
		}

		public void EnableAscending()
		{
			if (itemToTeleport.name == "Player1")
			{
				if (itemToTeleport.transform.position.z > 18)
				{
					ascending = true;
					landed = false;
				}
			}
			else if(itemToTeleport.name =="Player2")
			{
				if (itemToTeleport.transform.position.z < 18)
				{
					ascending = true;
					landed = false;
				}
			}
		}
		public void Teleport(Vector3 destination)
        {
            objectDestination = destination;
            ascending = true;
            landed = false;
        }
		// Update is called once per frame
		
		void Update()
		{
			if (ascending)
			{
				MoveBeamUpwards();
			}
			else if (ascending == false && landed == false) 
			{
				MoveBeamDownwards();
				
			}
		}
	}
}
