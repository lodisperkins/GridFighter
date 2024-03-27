using Lodis.Sound;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.WSA.Input;

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
}
