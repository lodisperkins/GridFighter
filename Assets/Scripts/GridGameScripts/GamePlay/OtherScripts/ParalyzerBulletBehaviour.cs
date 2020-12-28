using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame
{
    public class ParalyzerBulletBehaviour : BulletBehaviour
    {
        [SerializeField] private float stunTime;
        [SerializeField] private int drainAmount;
        public override void ResolveCollision(GameObject other)
        {
           
            if (other.CompareTag("Player") && other.name != Owner || reflected)
            {
                PlayHitParticleSystems(1);
                ps.transform.position = other.transform.position;
                PlayerSpawnBehaviour enemySpawnScript = other.GetComponent<PlayerSpawnBehaviour>();
                if(enemySpawnScript.CheckMaterial(drainAmount))
                {
                    enemySpawnScript.BuyItem(drainAmount);
                    if(Owner== "Player1")
                    {
                        BlackBoard.Player1.GetComponent<PlayerSpawnBehaviour>().AddMaterials(drainAmount);
                    }
                    else
                    {
                        BlackBoard.Player2.GetComponent<PlayerSpawnBehaviour>().AddMaterials(drainAmount);
                    }
                }
                BlackBoard.grid.StunPlayer(stunTime, other.name);
                Destroy(TempObject);
            }
            else if(other.CompareTag("Block"))
            {
                Destroy(TempObject);
            }
            
        }
    }
}


