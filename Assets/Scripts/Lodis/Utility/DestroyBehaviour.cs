using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyBehaviour : MonoBehaviour
{
    [SerializeField] private float _destroyDelay = 0f; // Delay before destruction, in seconds

    public void DestroyAfterDelay()
    {
        if (_destroyDelay <= 0f)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, _destroyDelay);
        }
    }

    public void DisableAfterDelay()
    {
        if (_destroyDelay <= 0f)
        {
            gameObject.SetActive(false);
        }
        else
        {
            RoutineBehaviour.Instance.StartNewTimedAction(a => gameObject.SetActive(false), TimedActionCountType.SCALEDTIME, _destroyDelay); 
        }
    }
}
