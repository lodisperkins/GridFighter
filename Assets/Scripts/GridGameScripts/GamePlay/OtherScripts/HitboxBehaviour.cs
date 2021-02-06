using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GridGame;
using GridGame.Movement;

public class HitboxBehaviour : MonoBehaviour {
    private Collider collider;
    [SerializeField]
    private bool activeByDefault;
    [SerializeField]
    private int damageVal;
    [SerializeField]
    private bool doesKnockback;
    [SerializeField]
    private bool stunsOpponent;
    [SerializeField]
    private float stunTime;
    [SerializeField]
    private UnityEvent onEnabled;
    [SerializeField]
    private UnityEvent onDisabled;
    [SerializeField]
    private bool onTriggerStay = false;
	// Use this for initialization
	void Start () {
        collider = GetComponent<Collider>();
		if(!activeByDefault)
        {
            collider.enabled = false;
            return;
        }
        onEnabled.Invoke();
	}
    IEnumerator MakeActiveTemporarily(float time)
    {
        collider.enabled = true;
        onEnabled.Invoke();
        yield return new WaitForSeconds(time);
        collider.enabled = false;
        onDisabled.Invoke();
    }
	public void MakeActive(float time = 0)
    {
        if(time <= 0)
        {
            collider.enabled = true;
            onEnabled.Invoke();
        }
        else if(!collider.enabled)
        {
            StartCoroutine(MakeActiveTemporarily(time));
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(onTriggerStay)
        {
            return;
        }
        HealthBehaviour objectHealth = other.GetComponent<HealthBehaviour>();
        if(objectHealth != null)
        {
            objectHealth.takeDamage(damageVal);
            if(doesKnockback)
            {
                Vector2 direction = other.transform.position - transform.position;
                GridPhysicsBehaviour physicsScript = other.gameObject.GetComponent<GridPhysicsBehaviour>();
                if (physicsScript != null)
                {
                    physicsScript.AddForce(direction);
                }
            }
            else if(stunsOpponent && other.gameObject.CompareTag("Player"))
            {
                BlackBoard.grid.StunPlayer(stunTime, other.gameObject.name);
            }
        }
        
    }
    private void OnTriggerStay(Collider other)
    {
        if (!onTriggerStay)
        {
            return;
        }
        HealthBehaviour objectHealth = other.GetComponent<HealthBehaviour>();
        if (objectHealth != null)
        {
            objectHealth.takeDamage(damageVal);
            if (doesKnockback)
            {
                Vector2 direction = other.transform.position - transform.position;
                GridPhysicsBehaviour physicsScript = other.gameObject.GetComponent<GridPhysicsBehaviour>();
                if (physicsScript != null)
                {
                    physicsScript.AddForce(direction);
                }
            }
            else if (stunsOpponent && other.gameObject.CompareTag("Player"))
            {
                BlackBoard.grid.StunPlayer(stunTime, other.gameObject.name);
            }
        }
    }
    public void DestroyObject()
    {
        GameObject temp = gameObject;
        Destroy(temp);
    }
    // Update is called once per frame
    void Update () {
		
	}
}
