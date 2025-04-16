using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ConfirmationMenuSpawner
{
    private static GameObject _confirmationMenuReference;

    private static ConfirmationMenuBehaviour _confirmationMenu = null;

    public static void Spawn(UnityAction onYes, UnityAction onNo, EventSystem eventSystem, string prompt, GameObject selectionOnClose = null, string leftText = "Yes", string rightText = "No")
    {
        if (!_confirmationMenuReference)
        {
            _confirmationMenuReference = Resources.Load<GameObject>("UI/ConfirmationMenuCanvas");
        }

        if (_confirmationMenu == null)
        {
            _confirmationMenu = MonoBehaviour.Instantiate(_confirmationMenuReference).GetComponent<ConfirmationMenuBehaviour>();
        }

        _confirmationMenu.Init(onYes, onNo, eventSystem, prompt, selectionOnClose, leftText, rightText);

        _confirmationMenu.Open();
    }

    public static void Spawn(UnityAction onYes, UnityAction onNo, EventSystem eventSystem, string prompt, GameObject selectionOnYes, GameObject selectionOnNo, string leftText = "Yes", string rightText = "No")
    {
        if (!_confirmationMenuReference)
        {
            _confirmationMenuReference = Resources.Load<GameObject>("UI/ConfirmationMenuCanvas");
        }

        if (_confirmationMenu == null)
        {
            _confirmationMenu = MonoBehaviour.Instantiate(_confirmationMenuReference).GetComponent<ConfirmationMenuBehaviour>();
        }

        _confirmationMenu.Init(onYes, onNo, eventSystem, prompt, selectionOnYes, selectionOnNo, leftText, rightText);

        _confirmationMenu.Open();
    }

    public static void Close()
    {
        _confirmationMenu.Close();
    }
}
