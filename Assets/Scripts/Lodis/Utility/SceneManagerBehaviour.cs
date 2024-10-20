﻿using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

namespace Lodis.Utility
{
    public enum GameMode
    {
        SINGLEPLAYER,
        PlayerVSCPU,
        PRACTICE,
        MULTIPLAYER,
        SIMULATE,
        TUTORIAL
    }

    public class SceneManagerBehaviour : MonoBehaviour
    {
        private static SceneManagerBehaviour _instance;
        [SerializeField]
        private IntVariable _gameMode;
        [SerializeField]
        private InputSystemUIInputModule _module;
        [SerializeField]
        private bool _updateDeviceBasedOnUI;
        private string _p1ControlScheme;
        private string _p2ControlScheme;
        [SerializeField]
        private InputProfileData _p1InputProfile;
        [SerializeField]
        private InputProfileData _p2InputProfile;
        [SerializeField]
        [Tooltip("Event called when scene starts. Cleared when transistioning between scenes.")]
        private UnityEvent _onStart;
        [SerializeField]
        private GameObject _loadScreen;

        private IntVariable _currentIndex;
        private int _previousScene;
        private bool _moduleEventAdded;


        public static SceneManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(SceneManagerBehaviour)) as SceneManagerBehaviour;

                if (!_instance)
                {
                    GameObject manager = Instantiate(Resources.Load<GameObject>("SceneManager"));
                    manager.name = "SceneManager";

                    _instance = manager.GetComponent<SceneManagerBehaviour>();
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        public IntVariable GameMode { get => _gameMode; set => _gameMode = value; }
        public string P1ControlScheme { get => _p1ControlScheme; set => _p1ControlScheme = value; }
        public string P2ControlScheme { get => _p2ControlScheme; set => _p2ControlScheme = value; }
        public InputDeviceData P1Devices { get => P1InputProfile.DeviceData; set => P1InputProfile.DeviceData = value; }
        public InputDeviceData P2Devices { get => P2InputProfile.DeviceData; set => P2InputProfile.DeviceData = value; }

        public int SceneIndex { get { return SceneManager.GetActiveScene().buildIndex; } }

        public InputProfileData P1InputProfile { get => _p1InputProfile; private set => _p1InputProfile = value; }
        public InputProfileData P2InputProfile { get => _p2InputProfile; private set => _p2InputProfile = value; }
        public UnityEvent OnStart { get => _onStart; set => _onStart = value; }
        public InputSystemUIInputModule Module { get => _module; set => _module = value; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _currentIndex = Resources.Load<IntVariable>("ScriptableObjects/CurrentScene");


            SceneManager.sceneLoaded += OnSceneLoaded;

            Cursor.visible = false;

            Application.targetFrameRate = 60;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            _loadScreen.SetActive(false);
        }

        private void Start()
        {
            _onStart?.Invoke();

            SceneManager.sceneUnloaded += s => _onStart.RemoveAllListeners();
        }

        public void UpdateDevices(int playerID)
        {
            ReadOnlyArray<InputDevice> pairedDevices = InputUser.all[playerID - 1].pairedDevices;

            if (playerID == 1)
                P1InputProfile.DeviceData.Value = pairedDevices.ToArray();
            else if (playerID == 2)
                P2InputProfile.DeviceData.Value = pairedDevices.ToArray();
        }

        public void UpdateDeviceP1(InputAction.CallbackContext context)
        {
            P1Devices.Value = new InputDevice[1];

            if (context.control.displayName == "Mouse")
                P1Devices[0] = Keyboard.current.device;
            else
                P1Devices[0] = context.control.device;

        }

        public void UpdateDeviceP2(InputAction.CallbackContext context)
        {
            P2Devices.Value = new InputDevice[1];
            P2InputProfile.DeviceData[0] = context.control.device;
        }

        public void SetGameMode(int mode)
        {
            _gameMode.Value = mode;
        }

        public void LoadBattleScene(int mode)
        {
            SetGameMode(mode);
            LoadScene(1);
            _previousScene = _currentIndex;
            _currentIndex.Value = 1;
        }

        public void LoadScene(int index)
        {
            SceneManager.LoadSceneAsync(index);
            _loadScreen.SetActive(true);

            _previousScene = _currentIndex;
            _currentIndex.Value = index;
        }

        public void LoadScene(string name)
        {
            SceneManager.LoadSceneAsync(name);
            _loadScreen.SetActive(true);

            _previousScene = _currentIndex;
            _currentIndex.Value = SceneManager.GetActiveScene().buildIndex;
        }

        public void LoadPreviousScene()
        {
            SceneManager.LoadSceneAsync(_previousScene);
            _loadScreen.SetActive(true);
        }

        public void QuitApplication()
        {
            Application.Quit();
        }

        private void Update()
        {


            if (_updateDeviceBasedOnUI && Module && !_moduleEventAdded)
            {
                Module.submit.action.started += UpdateDeviceP1;
                Module.leftClick.action.started += UpdateDeviceP1;
                _moduleEventAdded = true;
            }
        }
    }
}