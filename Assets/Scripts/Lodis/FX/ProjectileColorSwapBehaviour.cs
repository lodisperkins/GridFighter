using Lodis.Gameplay;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileColorSwapBehaviour : MonoBehaviour
{
    [SerializeField] private ColorManagerBehaviour colorManager;
    [SerializeField] private ColliderBehaviour colliderBehaviour;
    [SerializeField] private bool manuallySetColors = false;


    //---
    private bool _shouldUpdateColors;

    private void OnEnable()
    {
        _shouldUpdateColors = true;
    }

    private void OnDisable()
    {
        _shouldUpdateColors = false;
    }

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
    }

    public void SetColors(int alignment)
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
        if (colliderBehaviour != null && colliderBehaviour.Spawner != null && _shouldUpdateColors && !manuallySetColors)
        {
            GridMovementBehaviour move = colliderBehaviour.Spawner.GetComponent<GridMovementBehaviour>();
            colorManager.SetColors((int)move.Alignment);
            _shouldUpdateColors = false;
        }
    }
}
