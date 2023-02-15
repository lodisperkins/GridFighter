using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

namespace Lodis.Utility
{
    public enum GameMode
    {
        SINGLEPLAYER,
        PRACTICE,
        MULTIPLAYER,
        SIMULATE
    }

    public class SceneManagerBehaviour : MonoBehaviour
    {
        private static SceneManagerBehaviour _instance;
        [SerializeField]
        private IntVariable _gameMode;
        private string _p1ControlScheme;
        private string _p2ControlScheme;
        private InputDevice[] _p1Devices;
        private InputDevice[] _p2Device;

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
                }

                return _instance;
            }
        }

        public IntVariable GameMode { get => _gameMode; set => _gameMode = value; }
        public string P1ControlScheme { get => _p1ControlScheme; set => _p1ControlScheme = value; }
        public string P2ControlScheme { get => _p2ControlScheme; set => _p2ControlScheme = value; }
        public InputDevice[] P1Devices { get => _p1Devices; set => _p1Devices = value; }
        public InputDevice[] P2Devices { get => _p2Device; set => _p2Device = value; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void SetGameMode(int mode)
        {
            _gameMode.Value = mode;
        }

        public void LoadBattleScene(int mode)
        {
            SetGameMode(mode);
            LoadScene(1);
        }

        public void LoadScene(int index)
        {
            SceneManager.LoadScene(index);
        }

        public void QuitApplication()
        {
            Application.Quit();
        }
    }
}