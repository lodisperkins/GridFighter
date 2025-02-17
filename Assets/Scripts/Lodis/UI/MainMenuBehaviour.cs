using Lodis.Sound;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.UI;

public class MainMenuBehaviour : MonoBehaviour
{
    [SerializeField]
    private InputSystemUIInputModule _module;
    [SerializeField]
    private VolumeManagerBehaviour _volumeManager;

    // Start is called before the first frame update
    void Awake()
    {
        SceneManagerBehaviour.Instance.Module = _module;
        _volumeManager.InitializeSettings();
    }

    public void LoadScene(int index)
    {
        SceneManagerBehaviour.Instance.LoadScene(index);
    }

    public void SetGameMode(int mode)
    {
        SceneManagerBehaviour.Instance.SetGameMode(mode);
    }

    public void StartOnlineMode()
    {
        GridGameManager.Instance.GetComponent<GridGameManager>().OnOnlineClick();
    }

    public void Start()
    {
        GridGameManager.Instance.GetComponent<GridGameManager>().OnLocalClick();
    }
    int count;
    public void Print()
    {
        Debug.Log("Count " + count);
        count++;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
