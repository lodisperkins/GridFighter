using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileParticleColorSwapBehaviour : MonoBehaviour
{
    [SerializeField] private ParticleColorManagerBehaviour colorManager;
    [SerializeField] private ColliderBehaviour colliderBehaviour;
    [SerializeField] private bool clearParticles;
    [SerializeField] private bool restartParticles;

    //---
    private bool _shouldUpdateColors;

    private void Start()
    {
        if (colliderBehaviour == null)
        {
            colliderBehaviour = GetComponentInParent<ColliderBehaviour>();
        }

        if (colliderBehaviour == null)
        {
            colliderBehaviour = GetComponentInChildren<ColliderBehaviour>();
        }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        _shouldUpdateColors = true;
    }

    private void OnDisable()
    {
        _shouldUpdateColors = false;
    }

    public void SetColors(GridAlignment alignment)
    {
        if (colorManager != null)
        {
            colorManager.SetColors(alignment);
        }
        else
        {
            Debug.LogWarning("Color manager is not set on " + gameObject.name);
        }
    }

    private void LateUpdate()
    {
        if (colliderBehaviour != null && colliderBehaviour.Spawner != null && _shouldUpdateColors)
        {
            GridMovementBehaviour move = colliderBehaviour.Spawner.GetComponent<GridMovementBehaviour>();
            colorManager.SetColors(move.Alignment, clearParticles, restartParticles);
            _shouldUpdateColors = false;
        }
    }
}
