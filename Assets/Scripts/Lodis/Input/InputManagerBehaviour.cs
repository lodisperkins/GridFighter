using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManagerBehaviour : MonoBehaviour
{
    [SerializeField]
    private PlayerInputManager _inputManager;
    [SerializeField]
    private GameObject _player;

    private void Awake()
    {
        _inputManager.playerPrefab = _player;
    }

    // Start is called before the first frame update
    void Start()
    {
        _inputManager.JoinPlayer(0, 0, "Player", Keyboard.current);
        _inputManager.JoinPlayer(1, 1, "Player", InputSystem.devices[2]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
