using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GridGame.GamePlay
{
    public class BombBlockBehaviour : MonoBehaviour {

        [SerializeField]
        private int _damageVal;
        [SerializeField]
        private BlockBehaviour _block;
        private SphereCollider _explosionRadius;
        [SerializeField]
        private Text _timerText;
        [SerializeField]
        private RoutineBehaviour _routineScript;
        bool canExplode;
	    // Use this for initialization
	    void Start () {
            _explosionRadius = GetComponent<SphereCollider>();
            //this is mainly to check to see if the block is being displayed on the ui or in battle
            canExplode = true;
            if (_block == null)
            {
                
                Rigidbody rigidbody = GetComponent<Rigidbody>();
                Destroy(rigidbody);
                canExplode = false;
            }
        }
	    public void Explode()
        {
            if(_block.HealthScript != null && canExplode)
            { _explosionRadius.enabled = true;
                _block.HealthScript.PlayDeathParticleSystems(.5f);
                _block.DestroyBlock(.1f);
            }
            
        }
        private void OnTriggerEnter(Collider other)
        {
            //breaks all panels in range
            if(other.CompareTag("Panel"))
            {
                if(other.gameObject == _block.currentPanel && _explosionRadius.enabled)
                {
                    other.GetComponent<GridScripts.PanelBehaviour>().Occupied = false;
                }
                other.GetComponent<GridScripts.PanelBehaviour>().BreakPanel(10);
            }
            //damages everything else 
            else if(other.gameObject != _block.gameObject)
            {
                var health = other.GetComponent<HealthBehaviour>();
                if(health != null)
                {
                    health.takeDamage(_damageVal);
                }
            }
        }
        // Update is called once per frame
        void Update () {
            _timerText.text = (_routineScript.numberOfActionsLeft + 1).ToString();
	    }
    }
}


