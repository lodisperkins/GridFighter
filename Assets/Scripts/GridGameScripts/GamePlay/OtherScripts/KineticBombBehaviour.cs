using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame
{


    public class KineticBombBehaviour : MonoBehaviour
    {
        public Vector3 moveDirection;
        public Transform ownerTransform;
        public string owner;
        public SeekBehaviour seekScript;
        private bool _explosionActive;
        public float forceScale;
        public Rigidbody rigidbody;
        public ParticleSystem ps;
        [SerializeField]
        private int _damageVal;
        [SerializeField]
        private GridGame.Event _onExplosion;
        // Use this for initialization
        void Start()
        {
            
            rigidbody.AddForce(moveDirection * forceScale, ForceMode.Impulse);
        }
        void CheckPosition()
        {
            if (seekScript != null)
            {
                return;
            }
            if (transform.position.x >= 1 || transform.position.x <= -7 || transform.position.z >= 29 || transform.position.z <= 8)
            {
                seekScript = GetComponent<SeekBehaviour>();
                seekScript.Init(ownerTransform.position, rigidbody.velocity, 10, 1);
            }
        }
        private void OnTriggerStay(Collider other)
        {
            if(other.CompareTag("Projectile"))
            {
                if(other.GetComponent<BulletBehaviour>().Owner == owner)
                {
                    return;
                }
                if(transform.localScale.magnitude <= 5)
                {
                    transform.localScale *= 1.2f;
                    _damageVal += 2;
                }
            }
            else if(other.CompareTag("Panel") && _explosionActive)
            {
                GamePlay.GridScripts.PanelBehaviour panel = other.GetComponent<GamePlay.GridScripts.PanelBehaviour>();
                if(panel != null)
                {
                    panel.BreakPanel(5);
                }
            }
            if (_explosionActive)
            {
                var health = other.GetComponent<HealthBehaviour>();
                if (health != null)
                {
                    health.takeDamage(_damageVal);
                }
                this.Destroy();
            }
        }
        public void Explode()
        {
            _explosionActive = true;
            var tempPs = Instantiate(ps, transform.position, transform.rotation);
            tempPs.transform.localScale = transform.localScale;
            tempPs.Play();
            _onExplosion.Raise();
            Destroy(tempPs, .5f);
        }
        public void Destroy()
        {
            GameObject temp = gameObject;
            Destroy(temp);
        }
        // Update is called once per frame
        void Update()
        {
            CheckPosition();
        }
    }
}
