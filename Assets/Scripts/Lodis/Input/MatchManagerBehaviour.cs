﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Utility;
using DG.Tweening;
using UnityEngine.Events;
using System;
using Lodis.UI;
using Lodis.ScriptableObjects;
using Lodis.Sound;

namespace Lodis.Gameplay
{
    public enum MatchResult
    {
        DRAW,
        P1WINS,
        P2WINS
    }

    public class MatchManagerBehaviour : MonoBehaviour
    {
        private static MatchManagerBehaviour _instance;
        [Header("Environment References")]
        [SerializeField]
        private GridScripts.GridBehaviour _grid;
        private GameMode _mode;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierL;
        [SerializeField]
        private RingBarrierBehaviour _ringBarrierR;

        [Header("Match Options")]
        [SerializeField]
        private int _targetFrameRate;
        [SerializeField]
        private FloatVariable _matchStartTime;
        [SerializeField]
        private bool _invincibleBarriers;
        [SerializeField]
        private bool _infiniteEnergy;
        [SerializeField]
        private float _timeScale = 1;

        [Header("Music")]
        [SerializeField]
        private AudioClip _matchMusic;
        [SerializeField]
        private AudioClip _suddenDeathMusic;

        [Header("Match Events")]
        [SerializeField]
        private UnityEvent _onApplicationQuit;
        [SerializeField]
        private UnityEvent _onMatchStart;
        [SerializeField]
        private UnityEvent _onMatchPause;
        [SerializeField]
        private UnityEvent _onMatchUnpause;
        [SerializeField]
        private UnityEvent _onMatchRestart;
        [SerializeField]
        private UnityEvent _onMatchOver;

        [SerializeField]
        private GridGame.Event _matchRestartEvent;
        [SerializeField]
        private GridGame.Event _matchStartEvent;
        [SerializeField]
        private GridGame.Event _matchOverEvent;

        private PlayerSpawnBehaviour _playerSpawner;
        private bool _isPaused;
        private MatchResult _matchResult;
        private bool _canPause = true;
        private bool _suddenDeathActive;
        private bool _matchStarted;

        /// <summary>
        /// Gets the static instance of the black board. Creates one if none exists
        /// </summary>
        public static MatchManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(MatchManagerBehaviour)) as MatchManagerBehaviour;

                if (!_instance)
                {
                    GameObject manager = new GameObject("GameManager");
                    _instance = manager.AddComponent<MatchManagerBehaviour>();
                }

                return _instance;
            }
        }

        public MatchResult LastMatchResult
        {
            get { return _matchResult; }
        }

        public int TargetFrameRate
        {
            get { return _targetFrameRate; }
        }

        public static bool InfiniteEnergy { get; private set; }
        public bool InvincibleBarriers { get => _invincibleBarriers; set => _invincibleBarriers = value; }
        public bool SuddenDeathActive { get => _suddenDeathActive; private set => _suddenDeathActive = value; }

        private void Awake()
        {
            _mode = (GameMode)SceneManagerBehaviour.Instance.GameMode.Value;

            _grid.DestroyTempPanels();
            _grid.InvincibleBarriers = InvincibleBarriers;
            InfiniteEnergy = _infiniteEnergy;

            //Initialize grid
            _grid.CreateGrid();

            _playerSpawner = GetComponent<PlayerSpawnBehaviour>();
            _playerSpawner.SpawnEntitiesByMode(_mode);

            _onMatchRestart.AddListener(_playerSpawner.ResetPlayers);
            _onMatchRestart.AddListener(() =>
            {
                if (SuddenDeathActive)
                    SoundManagerBehaviour.Instance.SetMusic(_suddenDeathMusic);
                else
                    SoundManagerBehaviour.Instance.SetMusic(_matchMusic);

            });

            RoutineBehaviour.Instance.StartNewConditionAction(args =>
            {
                SetMatchResult();
                _onMatchOver?.Invoke();
                _matchOverEvent?.Raise(gameObject);
                _canPause = false;
                if (_matchResult == MatchResult.DRAW)
                    RoutineBehaviour.Instance.StartNewTimedAction(values => Restart(true), TimedActionCountType.SCALEDTIME, 2);
            },
            args => _playerSpawner.P1HealthScript.HasExploded || _playerSpawner.P2HealthScript.HasExploded || MatchTimerBehaviour.Instance.TimeUp);

            Application.targetFrameRate = _targetFrameRate;

            Time.timeScale = _timeScale;
        }


        private void Start()
        {
            SetPlayerControlsActive(false);
            _canPause = false;

            RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                _canPause = true;
                SetPlayerControlsActive(true);
                _matchStarted = true;
                _onMatchStart?.Invoke();
                _matchStartEvent.Raise();
            }, TimedActionCountType.SCALEDTIME, _matchStartTime.Value);
        }

        private void SetMatchResult()
        {
            if (_ringBarrierL.IsAlive == _ringBarrierR.IsAlive && !_suddenDeathActive)
                _matchResult = MatchResult.DRAW;
            else if (_playerSpawner.P2HealthScript.HasExploded)
                _matchResult = MatchResult.P1WINS;
            else if (_playerSpawner.P1HealthScript.HasExploded)
                _matchResult = MatchResult.P2WINS;
            else if (!_ringBarrierR.IsAlive)
                _matchResult = MatchResult.P1WINS;
            else if (!_ringBarrierL.IsAlive)
                _matchResult = MatchResult.P2WINS;
        }

        /// <summary>
        /// Temporarily changes the speed of time for the game.
        /// </summary>
        /// <param name="newTimeScale">The new time scale. 0 being no time passes and 1 being the normal speed.</param>
        /// <param name="speed">How long it takes to transition into the new time scale.</param>
        /// <param name="duration">How long the timescale will be this speed.</param>
        public void ChangeTimeScale(float newTimeScale, float speed, float duration)
        {
            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, speed / 2).SetUpdate(true);
            RoutineBehaviour.Instance.StartNewTimedAction(args => Time.timeScale = 1, TimedActionCountType.UNSCALEDTIME, duration);
        }

        public void SetPlayerControlsActive(bool value)
        {
            BlackBoardBehaviour.Instance.Player1Controller.Enabled = value;
            BlackBoardBehaviour.Instance.Player2Controller.Enabled = value;
        }

        public void TogglePause()
        {
            if (!_canPause)
                return;

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

        public void Restart(bool suddenDeathActive = false)
        {
            _playerSpawner.SuddenDeathActive = suddenDeathActive;
            SuddenDeathActive = suddenDeathActive;
            MatchTimerBehaviour.Instance.IsInfinite = suddenDeathActive;

            _onMatchRestart?.Invoke();
            _matchRestartEvent.Raise(gameObject);
            _matchStarted = false;


            if (suddenDeathActive)
            {
                _ringBarrierL.Deactivate();
                _ringBarrierR.Deactivate();
            }

            if (_isPaused)
                TogglePause();

            SetPlayerControlsActive(false);
            _canPause = false;
            RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {

                _canPause = true;
                SetPlayerControlsActive(true);
                _matchStarted = true;
                _onMatchStart?.Invoke();
                _matchStartEvent.Raise();
            }, TimedActionCountType.SCALEDTIME, _matchStartTime.Value);

            RoutineBehaviour.Instance.StartNewConditionAction(args =>
            {
                SetMatchResult();
                _onMatchOver?.Invoke();
                _matchOverEvent?.Raise(gameObject);
                _canPause = false;

                if (_matchResult == MatchResult.DRAW)
                    RoutineBehaviour.Instance.StartNewTimedAction(values => Restart(true), TimedActionCountType.SCALEDTIME, 2);
            },
            args => _playerSpawner.P1HealthScript.HasExploded || _playerSpawner.P2HealthScript.HasExploded || MatchTimerBehaviour.Instance.TimeUp);
        }

        public void ReturnToMainMenu()
        {
            SceneManagerBehaviour.Instance.LoadScene(0);
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

        public void AddOnMatchStartAction(UnityAction action)
        {
            _onMatchStart.AddListener(action);
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

    [CustomEditor(typeof(MatchManagerBehaviour))]
    class GameManagerEditor : Editor
    {
        private MatchManagerBehaviour _manager;

        private void Awake()
        {
            _manager = (MatchManagerBehaviour)target;
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


