using System;
using System.Collections;
using System.Collections.Generic;
using GridGame;
using UnityEngine;

public class MeleeWeaponBehaviour : MonoBehaviour {

	[SerializeField] private ParticleSystem ps;
	[SerializeField] private int _damageVal;
	[SerializeField] private GridGame.VariableScripts.IntVariable playerEnergy;
	private void OnTriggerEnter(Collider other)
	{
		
		
	}
	//Plays particle effect when hit
	public void PlayParticleSystems(float duration)
	{
		var tempPs = Instantiate(ps,transform.position,transform.rotation);
		tempPs.playbackSpeed = 2.5f;
		tempPs.Play();
		Destroy(tempPs, duration);
	}
}
