using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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