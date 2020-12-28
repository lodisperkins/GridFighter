using System.Collections;
using System.Collections.Generic;
using GridGame;
using UnityEngine;
using UnityEngine.Serialization;
using Event = GridGame.Event;

public class OverdriveBehaviour : MonoBehaviour
{

	[SerializeField] private PlayerSpawnBehaviour _playerSpawnScript;
    [SerializeField] private GridGame.Movement.PlayerMovementBehaviour _playerMoveScript;
    [SerializeField] private HealthBehaviour _health;
	private float _time;
	[SerializeField]
	private float _overdriveLength;
	[SerializeField] private ParticleSystem ps;
	[SerializeField] private GameObject _particles;
	[SerializeField] private Event _onOverdriveEnabled;
	[SerializeField] private Event _onOverdriveDisabled;
	[FormerlySerializedAs("_used")] public bool used;
	// Use this for initialization
	void Start ()
	{
		used = false;
	}
	public void PlayParticleSystems(float duration)
	{
		var tempPs = Instantiate(ps,transform.position,transform.rotation);
		tempPs.playbackSpeed = 2.5f;
		tempPs.Play();
		Destroy(tempPs, duration);
	}
	public void StopMaterialLoss()
	{
		if ( used)
		{
			return;
		}
		_playerSpawnScript.overdriveEnabled = true;
		_onOverdriveEnabled.Raise(gameObject);
		_playerSpawnScript.AddMaterials(60);
		
		if (_health.health.Val >= 75)
		{
			_overdriveLength = 5;
		}
		else if(_health.health.Val >= 25 && _health.health.Val < 75)
		{
			_overdriveLength = 15;
		}
		else
		{
			_overdriveLength = 30;
		}
		_particles.SetActive(true);
		_time = Time.time + _overdriveLength;
	}
	
	// Update is called once per frame
	void Update () {
		if (_playerSpawnScript.overdriveEnabled)
		{
			used = true;
			if (Time.time >=_time )
			{
				_playerSpawnScript.overdriveEnabled = false;
				_onOverdriveDisabled.Raise(gameObject);
				_particles.SetActive(false);
				
			}
		}
        if(Input.GetAxis("PanelAction1") <= -.5f && Input.GetAxis("PanelAction1") >= .5f)
        {
            StopMaterialLoss();
        }
        //Debug.Log(Input.GetAxis("PanelAction1"));

    }
}
