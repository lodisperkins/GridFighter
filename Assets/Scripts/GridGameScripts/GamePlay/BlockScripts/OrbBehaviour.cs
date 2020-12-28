using System;
using UnityEngine;

namespace GridGame.GamePlay.BlockScripts
{
    public class OrbBehaviour : MonoBehaviour
    {

        public BlockBehaviour block;
        [SerializeField] public int DamageVal;
        [SerializeField] private GameObject ps;
        private void OnTriggerEnter(Collider other)
        {
            ResolveCollision(other.gameObject);
        }
        public void PlayHitParticleSystems(float duration)
        {
            var tempPs = Instantiate(ps, transform.position, transform.rotation);
            Destroy(tempPs, .5f);
        }
        private void ResolveCollision(GameObject other)
        {
            switch (other.tag)
            {
                case "Block":
                {
                    if(other == block.gameObject)
                    {
                        break;
                    }
                    var health = other.GetComponent<HealthBehaviour>();
                    if (health != null && block.owner != null && DamageVal > 0)
                    {
                        other.GetComponent<BlockBehaviour>().GiveMoneyForKill(block.owner.name,DamageVal);
                        health.takeDamage(DamageVal);
                    }
                    if(DamageVal > 0)
                    {
                        PlayHitParticleSystems(1);
                    }
                    
                    break;
                }
                case "Player":
                {
                    var health = other.GetComponent<HealthBehaviour>();
                    if (health != null && other.name != block.owner.name && DamageVal > 0)
                    {
                        health.takeDamage(DamageVal);
                        PlayHitParticleSystems(1);
                    }
                    
                    break;
                }
            }
        }
        private void Update()
        {
        }
    }
}
