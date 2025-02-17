using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileParticleColorSwapBehaviour : MonoBehaviour
{
    [SerializeField] private ParticleColorManagerBehaviour colorManager;
    [SerializeField] private ColliderBehaviour colliderBehaviour;

    // Start is called before the first frame update
    void Start()
    {
        if (colliderBehaviour == null)
        {
            colliderBehaviour = GetComponentInParent<ColliderBehaviour>();
        }

        if (colliderBehaviour == null)
        {
            colliderBehaviour = GetComponentInChildren<ColliderBehaviour>();
        }

        if (colliderBehaviour != null && colliderBehaviour.Spawner != null)
        {
            GridMovementBehaviour move = colliderBehaviour.Spawner.GetComponent<GridMovementBehaviour>();
            colorManager.SetColors(move.Alignment);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
