using Lodis.ScriptableObjects;
using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

//If the name is taken or if there isn't a name set make the save button spawn the keyboard menu
/// <summary>
/// Spawns a confirmation menu on the given page if the save boolean has not been set to true.
/// </summary>
public class SavePopupBehaviour : MonoBehaviour
{
    [SerializeField] private BoolVariable _saveBoolean;
    [SerializeField] private string _saveMessage;
    [SerializeField] private EventSystem _eventSystem;
    [SerializeField] private UnityEvent _onContinueWithoutSaving;
    [SerializeField] private UnityEvent _onCancel;
    [SerializeField] private PageManagerBehaviour _pageManager;
    [SerializeField] private string _pageName;
    [SerializeField] private GameObject _selectOnClose;
    [SerializeField] private GameObject _selectOnContinue;
    [SerializeField] private GameObject _selectOnCancel;

    public static BoolVariable ChangesAreSaved { get; private set; }

    private void Awake()
    {
        ChangesAreSaved = _saveBoolean;
        Page page = _pageManager.RootPage.GetChildByName(_pageName);

        if (page == null)
        {
            Debug.LogError("Couldn't find page with the name " +  _pageName + " for the save confirmation popup.");
        }

        page.GoToParentCondition = _saveBoolean;
    }

    public void TrySpawnPopup()
    {
        if (_saveBoolean.Value || _pageManager.CurrentPage.PageName != _pageName)
            return;

        if (_selectOnClose)
            ConfirmationMenuSpawner.Spawn(() => _onContinueWithoutSaving?.Invoke(), () => _onCancel?.Invoke(), _eventSystem, _saveMessage, _selectOnClose);
        else if (_selectOnCancel || _selectOnContinue)
            ConfirmationMenuSpawner.Spawn(() => _onContinueWithoutSaving?.Invoke(), () => _onCancel?.Invoke(), _eventSystem, _saveMessage, _selectOnContinue, _selectOnCancel);

    }
}
