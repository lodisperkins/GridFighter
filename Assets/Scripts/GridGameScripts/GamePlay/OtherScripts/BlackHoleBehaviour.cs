using GridGame;
using GridGame.GamePlay.GridScripts;
using GridGame.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleBehaviour : MonoBehaviour {
    [SerializeField]
    private GridPhysicsBehaviour physicsBehaviour;
    private string _owner;
    private string _axis;
    [SerializeField]
    private float pullForceScale;
    [SerializeField]
    private float timeActive;
    [SerializeField]
    private GameObject _effects;
    [SerializeField]
    private GameObject _suctionCollider;
    [SerializeField]
    private float _travelDistance;
    [SerializeField]
    private SeekBehaviour _seekScript;
    private HealthBehaviour _playerHealth;
    public string Owner
    {
        get
        {
            return _owner;
        }

        set
        {
            _owner = value;
        }
    }

    // Use this for initialization
    void Start () {
        
	}

    public void DestroySelf()
    {
        GameObject temp = gameObject;
        Destroy(temp);
    }

	private void GetPosition()
    {
        if(Owner == "Player1")
        {
            Vector2 spawnOffset = GridPhysicsBehaviour.ConvertToGridVector(transform.parent.forward);
            if(GridBehaviour.globalPanelList.FindPanel(BlackBoard.p1Position.Position + spawnOffset, out physicsBehaviour.currentPanel) == false)
            {
                Debug.Log("Blackhole can't find offset panel P1");
            }
        }
        else if(Owner == "Player2")
        {
            Vector2 spawnOffset = GridPhysicsBehaviour.ConvertToGridVector(transform.parent.forward);
            if (GridBehaviour.globalPanelList.FindPanel(BlackBoard.p2Position.Position + spawnOffset, out physicsBehaviour.currentPanel) == false)
            {
                Debug.Log("Blackhole can't find offset panel P2");
            }
        }
        else
        {
            Debug.Log("BlackHole owner not set or invalid");
        }
    }

    public void ActivateSuction()
    {
        _effects.SetActive(true);
        _suctionCollider.SetActive(true);
        _seekScript.SeekEnabled = false;
        GameObject temp = gameObject;
        Destroy(temp, timeActive);
    }

    public void AddForce(GridPhysicsBehaviour other)
    {
        if(other.IsMoving || other.name == Owner || Owner == "")
        {
            return;
        }
        other.AddForce(pullForceScale, physicsBehaviour.currentPanel);
    }

    public void SetCurrentPanel(PanelBehaviour panel)
    {
        physicsBehaviour.currentPanel = panel;
        physicsBehaviour.StopsWhenHit = false;
        Vector3 reference = transform.forward * _travelDistance;
        physicsBehaviour.AddForce(GridPhysicsBehaviour.ConvertToGridVector(reference));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name != _owner && other.CompareTag("Player") && !_suctionCollider.activeSelf)
        {
            _playerHealth = other.GetComponent<HealthBehaviour>();
            _playerHealth.CanStun = false;
            ActivateSuction();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        GridPhysicsBehaviour gridPhysics = other.GetComponent<GridPhysicsBehaviour>();
        if(gridPhysics)
        {
            AddForce(gridPhysics);
        }
    }
    private void OnDestroy()
    {
        if (_playerHealth)
        {
            _playerHealth.CanStun = false;
            _playerHealth.SendMessage("EnableControls");
        }
            
    }
}
