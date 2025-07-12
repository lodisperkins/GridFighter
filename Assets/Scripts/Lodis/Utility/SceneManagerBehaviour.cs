using Lodis.AI;
using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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
        TUTORIAL,
        ONLINE
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
        [SerializeField] private bool _showMouse;
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
        private AsyncOperation _sceneOperation;
        private string lhsRecordingName;
        private string rhsRecordingName;
        private ActionPlaybackInfo[] lhsRecordings;
        private ActionPlaybackInfo[] rhsRecordings;



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
        public AsyncOperation SceneOperation { get => _sceneOperation; private set => _sceneOperation = value; }
        public string LhsRecordingName { get => lhsRecordingName; set => lhsRecordingName = value; }
        public string RhsRecordingName { get => rhsRecordingName; set => rhsRecordingName = value; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _currentIndex = Resources.Load<IntVariable>("ScriptableObjects/CurrentScene");

            GameMode.Value = -1;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Cursor.visible = _showMouse;

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

        public void SetGameMode(GameMode mode)
        {
            _gameMode.Value = (int)mode;
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
            SceneOperation = SceneManager.LoadSceneAsync(index);

            if ((_gameMode == 1 || _gameMode == 4) && index == 4)
            {
                SceneOperation.allowSceneActivation = false;

                try
                {
                    LoadAIDecisions().ContinueWith(_ =>
                    {
                        SceneOperation.allowSceneActivation = true;
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load AI decisions: {e.Message}");
                }
            }

            _loadScreen.SetActive(true);

            _previousScene = _currentIndex;
            _currentIndex.Value = index;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        }

        public void LoadScene(string name)
        {
            SceneOperation = SceneManager.LoadSceneAsync(name);

            if ((_gameMode == 1 || _gameMode == 4) && name == "Stadium")
            {
                SceneOperation.allowSceneActivation = false;

                try
                {
                    Task loadDecisionTask = LoadAIDecisions();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load AI decisions: {e.Message}");
                }
            }

            _loadScreen.SetActive(true);

            _previousScene = _currentIndex;
            _currentIndex.Value = SceneManager.GetActiveScene().buildIndex;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        }

        private async Task LoadAIDecisions()
        {
            if (!string.IsNullOrEmpty(LhsRecordingName))
            {
                lhsRecordings = await AIRecorderBehaviour.LoadAsync(LhsRecordingName);
            }

            if (!string.IsNullOrEmpty(RhsRecordingName))
            {
                rhsRecordings = await AIRecorderBehaviour.LoadAsync(RhsRecordingName);
            }

            while (SceneOperation.progress < 0.9f)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            SceneOperation.allowSceneActivation = true;
        }

        public ActionPlaybackInfo[] GetRecordings(int playerID)
        {
            if (playerID == 0)
            {
                return lhsRecordings;
            }
            else if (playerID == 1)
            {
                return rhsRecordings;
            }
            else
            {
                throw new ArgumentException("Invalid player ID. Must be 0 or 1.");
            }
        }

        public void LoadPreviousScene()
        {
            SceneOperation = SceneManager.LoadSceneAsync(_previousScene);
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