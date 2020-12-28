using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame.GamePlay.AI
{
    public class BulletNotifierBehaviour : MonoBehaviour
    {
        [SerializeField]
        private AIControllerBehaviour playerAI;
        [SerializeField]
        private GridScripts.PanelBehaviour panel;
        [SerializeField]
        private string notifyType;
        // Use this for initialization
        void Start()
        {
            enabled = playerAI.enabled;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Projectile") || other.CompareTag("Hazard"))
            {
                playerAI.NotifyOfBulletHit(panel,notifyType);
            }
        }
    }
}


