using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuSelectorBehaviour : MonoBehaviour
{
    [SerializeField]
    private EventButtonBehaviour[] _options;
    [SerializeField]
    private EventSystem _eventSystem;

    public void SetFirstAvailableSelected()
    {
        foreach (EventButtonBehaviour option in _options)
        {
            if (option.gameObject.activeInHierarchy)
            {
                _eventSystem.SetSelectedGameObject(option.gameObject);
                _eventSystem.UpdateModules();
                option.OnSelect();
                return;
            }    
        }
    }
}
