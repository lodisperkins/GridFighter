using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
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
        private IntVariable _currentIndex;
        private int _previousScene;


        public static SceneManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(SceneManagerBehaviour)) as SceneManagerBehaviour;

                if (!_instance)
                {
                    GameObject manager = new GameObject("SceneManager");
                    _instance = manager.AddComponent<SceneManagerBehaviour>();
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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _currentIndex = Resources.Load<IntVariable>("ScriptableObjects/CurrentScene");

            if (_updateDeviceBasedOnUI && _module)
                _module.submit.action.started += UpdateDeviceP1;

            Application.targetFrameRate = 60;
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
            SceneManager.LoadScene(index);
            _previousScene = _currentIndex;
            _currentIndex.Value = index;
        }

        public void LoadScene(string name)
        {
            SceneManager.LoadScene(name);
            _previousScene = _currentIndex;
            _currentIndex.Value = SceneManager.GetActiveScene().buildIndex;
        }

        public void LoadPreviousScene()
        {
            SceneManager.LoadScene(_previousScene);
        }

        public void QuitApplication()
        {
            Application.Quit();
        }
    }
}