using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;
using Lodis.Utility;
using Lodis.Input;
using DG.Tweening;
using UnityEngine.Events;
using System;

namespace Lodis.Gameplay
{
    public enum GameMode
    {
        SINGLEPLAYER,
        PRACTICE,
        MULTIPLAYER,
        SIMULATE
    }

    public class GameManagerBehaviour : MonoBehaviour
    {
        private static GameManagerBehaviour _instance;
        [SerializeField]
        private GridScripts.GridBehaviour _grid;
        [SerializeField]
        private GameMode _mode;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierL;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierR;
        [SerializeField]
        private int _targetFrameRate;
        [SerializeField]
        private bool _invincibleBarriers;
        [SerializeField]
        private bool _infiniteEnergy;
        [SerializeField]
        private float _timeScale = 1;
        [SerializeField]
        private UnityEvent _onApplicationQuit;
        [SerializeField]
        private UnityEvent _onMatchPause;
        [SerializeField]
        private UnityEvent _onMatchUnpause;
        [SerializeField]
        private UnityEvent _onMatchRestart;
        [SerializeField]
        private UnityEvent _onMatchOver;
        private PlayerSpawnBehaviour _playerSpawner;
        private bool _isPaused;

        /// <summary>
        /// Gets the static instance of the black board. Creates one if none exists
        /// </summary>
        public static GameManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(GameManagerBehaviour)) as GameManagerBehaviour;

                if (!_instance)
                {
                    GameObject manager = new GameObject("GameManager");
                    _instance = manager.AddComponent<GameManagerBehaviour>();
                }

                return _instance;
            }
        }

        public int TargetFrameRate
        {
            get { return _targetFrameRate; }
        }

        public static bool InfiniteEnergy { get; private set; }
        public bool InvincibleBarriers { get => _invincibleBarriers; set => _invincibleBarriers = value; }

        private void Awake()
        {
            _grid.DestroyTempPanels();
            _grid.InvincibleBarriers = InvincibleBarriers;
            InfiniteEnergy = _infiniteEnergy;

            //Initialize grid
            _grid.CreateGrid();

            _playerSpawner = GetComponent<PlayerSpawnBehaviour>();
            _playerSpawner.SpawnEntitiesByMode(_mode);
            _onMatchRestart.AddListener(_playerSpawner.ResetPlayers);

            RoutineBehaviour.Instance.StartNewConditionAction(args => _onMatchOver?.Invoke(), args => !_playerSpawner.P1HealthScript.IsAlive || !_playerSpawner.P2HealthScript.IsAlive);

            Application.targetFrameRate = _targetFrameRate;

            Time.timeScale = _timeScale;
        }

        /// <summary>
        /// Temporarily changes the speed of time for the game.
        /// </summary>
        /// <param name="newTimeScale">The new time scale. 0 being no time passes and 1 being the normal speed.</param>
        /// <param name="time">The amount of time the game will stay in the temporary time scale.</param>
        public void ChangeTimeScale(float newTimeScale, float time)
        {
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, time).SetUpdate(true).onComplete += () => Time.timeScale = 1;
        }

        public void SetPlayerControlsActive(bool value)
        {
            BlackBoardBehaviour.Instance.Player1Controller.Enabled = value;
            BlackBoardBehaviour.Instance.Player2Controller.Enabled = value;
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = Convert.ToInt32(!_isPaused);

            if (_isPaused)
            {
                SetPlayerControlsActive(false);
                _timeScale = 0;
                _onMatchPause?.Invoke();
            }
            else
            {
                SetPlayerControlsActive(true);
                _timeScale = 1;
                _onMatchUnpause.Invoke();
            }
        }

        public void Restart()
        {
            _onMatchRestart?.Invoke();

            if (_isPaused)
                TogglePause();

            RoutineBehaviour.Instance.StartNewConditionAction(args => _onMatchOver?.Invoke(), args => !_playerSpawner.P1HealthScript.IsAlive || !_playerSpawner.P2HealthScript.IsAlive);
        }

        public void QuitApplication()
        {
            _onApplicationQuit?.Invoke();
            Application.Quit();
        }

        public void AddOnApplicationQuitAction(UnityAction action)
        {
            _onApplicationQuit.AddListener(action);
        }

        public void AddOnMatchRestartAction(UnityAction action)
        {
            _onMatchRestart.AddListener(action);
        }

        public void AddOnMatchOverAction(UnityAction action)
        {
            _onMatchOver.AddListener(action);
        }

        public void AddOnMatchPauseAction(UnityAction action)
        {
            _onMatchPause.AddListener(action);
        }

        public void AddOnMatchUnpauseAction(UnityAction action)
        {
            _onMatchUnpause.AddListener(action);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(GameManagerBehaviour))]
    class GameManagerEditor : Editor
    {
        private GameManagerBehaviour _manager;

        private void Awake()
        {
            _manager = (GameManagerBehaviour)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Reset Game"))
            {
                _manager.Restart();
            }
        }
    }

#endif
}


