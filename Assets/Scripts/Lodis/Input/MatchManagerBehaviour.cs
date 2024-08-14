using System.Collections;
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
using UnityEngine.UI;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using CustomEventSystem;
using SharedGame;
using Unity.Collections;

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
        [SerializeField]
        private Button _firstSelectedPauseButton;

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
        private bool _infiniteBurst;
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
        private UnityEvent _onMatchCountdownStart;
        [SerializeField]
        private UnityEvent _onMatchPause;
        [SerializeField]
        private UnityEvent _onMatchUnpause;
        [SerializeField]
        private UnityEvent _onMatchRestart;
        [SerializeField]
        private UnityEvent _onMatchOver;
        [SerializeField]
        private GameEventListener _onP1RingOut;
        [SerializeField]
        private GameEventListener _onP2RingOut;

        [SerializeField]
        private CustomEventSystem.Event _matchRestartEvent;
        [SerializeField]
        private CustomEventSystem.Event _matchStartEvent;
        [SerializeField]
        private CustomEventSystem.Event _matchOverEvent;

        private PlayerSpawnBehaviour _playerSpawner;
        private bool _isPaused;
        private MatchResult _matchResult;
        private bool _canPause = true;
        private bool _suddenDeathActive;
        private bool _matchStarted;
        private bool _playerOutOfRing;
        private TweenerCore<float, float, FloatOptions> _timeScaleTween;
        private DelayedAction _timeScaleAction;
        private int _lhsWins;
        private int _rhsWins;

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
        public PlayerSpawnBehaviour PlayerSpawner { get => _playerSpawner; private set => _playerSpawner = value; }
        public bool SuperInUse { get; internal set; }
        public bool PlayerOutOfRing { get => _playerOutOfRing; private set => _playerOutOfRing = value; }
        public int LhsWins { get => _lhsWins; private set => _lhsWins = value; }
        public int RhsWins { get => _rhsWins; private set => _rhsWins = value; }
        public FloatVariable MatchStartTime { get => _matchStartTime; set => _matchStartTime = value; }
        public bool InfiniteBurst { get => _infiniteBurst; set => _infiniteBurst = value; }

        private void Awake()
        {
            _mode = (GameMode)SceneManagerBehaviour.Instance.GameMode.Value;

            _grid.DestroyTempPanels();
            _grid.InvincibleBarriers = InvincibleBarriers;
            InfiniteEnergy = _infiniteEnergy;

            //Initialize grid
            _grid.CreateGrid();

            PlayerSpawner = GetComponent<PlayerSpawnBehaviour>();
            PlayerSpawner.SpawnEntitiesByMode(_mode);

            _onMatchRestart.AddListener(PlayerSpawner.ResetPlayers);
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
            args => PlayerSpawner.P1HealthScript.HasExploded || PlayerSpawner.P2HealthScript.HasExploded || MatchTimerBehaviour.Instance.TimeUp);

            AddOnRingoutAction(() => PlayerOutOfRing = true);

            Application.targetFrameRate = _targetFrameRate;

            Time.timeScale = _timeScale;
        }


        private void Start()
        {
            SetPlayerControlsActive(false);
            _canPause = false;

            _onMatchCountdownStart?.Invoke();

            RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {
                _canPause = true;
                SetPlayerControlsActive(true);
                _matchStarted = true;
                _onMatchStart?.Invoke();
                _matchStartEvent.Raise();
            }, TimedActionCountType.SCALEDTIME, MatchStartTime.Value);
        }

        private void SetMatchResult()
        {
            if (PlayerSpawner.P2HealthScript.HasExploded)
            {
                _matchResult = MatchResult.P1WINS;
                _lhsWins++;
            }
            else if (PlayerSpawner.P1HealthScript.HasExploded)
            {
                _matchResult = MatchResult.P2WINS;
                _rhsWins++;
            }
            else if (!_suddenDeathActive)
            {
                _matchResult = MatchResult.DRAW;
            }
            //else if (!_ringBarrierR.IsAlive)
            //{
            //    _matchResult = MatchResult.P1WINS;
            //    _lhsWins++;
            //}
            //else if (!_ringBarrierL.IsAlive)
            //{
            //    _matchResult = MatchResult.P2WINS;
            //    _rhsWins++;
            //}
        }

        public void SetMatchResult(int resultID)
        {
            _matchResult = (MatchResult)resultID;
            SetMatchResult();
        }

        /// <summary>
        /// Temporarily changes the speed of time for the game.
        /// </summary>
        /// <param name="newTimeScale">The new time scale. 0 being no time passes and 1 being the normal speed.</param>
        /// <param name="speed">How long it takes to transition into the new time scale.</param>
        /// <param name="duration">How long the timescale will be this speed.</param>
        public void ChangeTimeScale(float newTimeScale, float speed, float duration)
        {
            _timeScaleTween = DOTween.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, speed / 2).SetUpdate(true);
            _timeScaleAction = RoutineBehaviour.Instance.StartNewTimedAction(args => Time.timeScale = 1, TimedActionCountType.UNSCALEDTIME, duration);
        }

        /// <summary>
        /// Temporarily changes the speed of time for the game.
        /// </summary>
        /// <param name="newTimeScale">The new time scale. 0 being no time passes and 1 being the normal speed.</param>
        /// <param name="speed">How long it takes to transition into the new time scale.</param>
        /// <param name="duration">How long the timescale will be this speed.</param>
        public void ChangeTimeScale(float newTimeScale, float speed, Condition condition)
        {
            _timeScaleTween = DOTween.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, speed / 2).SetUpdate(true);
            _timeScaleAction = RoutineBehaviour.Instance.StartNewConditionAction(args => Time.timeScale = 1, condition);
        }

        public void ResetTimeScale()
        {
            Time.timeScale = 1;
            _timeScaleTween.Kill();
            RoutineBehaviour.Instance.StopAction(_timeScaleAction);
        }

        public void SetPlayerControlsActive(bool value)
        {
            BlackBoardBehaviour.Instance.Player1Controller.Enabled = value;
            BlackBoardBehaviour.Instance.Player2Controller.Enabled = value;
        }

        public void ToggleInfiniteEnergy()
        {
            InfiniteEnergy = !InfiniteEnergy;
        }

        public void ToggleInvincibleBarriers()
        {
            InvincibleBarriers = !InvincibleBarriers;

            if (InvincibleBarriers)
            {
                _ringBarrierL.SetInvincibilityByCondition(condition => !InvincibleBarriers);
                _ringBarrierR.SetInvincibilityByCondition(condition => !InvincibleBarriers);
            }
        }
        public void ToggleInfiniteBurstEnergy()
        {
            InfiniteBurst = !InfiniteBurst;
        }

        public void TogglePauseMenu()
        {
            if (!_canPause)
                return;

            _isPaused = !_isPaused;
            Time.timeScale = Convert.ToInt32(!_isPaused);
            _timeScale = Time.timeScale;

            SetPlayerControlsActive(!_isPaused);

            if (_isPaused)
            {
                _onMatchPause?.Invoke();
                _firstSelectedPauseButton.OnSelect(null);
            }
            else
                _onMatchUnpause.Invoke();
        }

        public void Restart(bool suddenDeathActive = false)
        {
            PlayerSpawner.SuddenDeathActive = suddenDeathActive;
            SuddenDeathActive = suddenDeathActive;
            MatchTimerBehaviour.Instance.IsInfinite = suddenDeathActive;

            _onMatchRestart?.Invoke();
            _matchRestartEvent.Raise(gameObject);
            _matchStarted = false;
            PlayerOutOfRing = false;

            if (suddenDeathActive)
            {
                _ringBarrierL.Deactivate(false);
                _ringBarrierR.Deactivate(false);
            }

            if (_isPaused)
                TogglePauseMenu();

            SetPlayerControlsActive(false);
            _canPause = false;

            _onMatchCountdownStart?.Invoke();
            RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {

                _canPause = true;
                SetPlayerControlsActive(true);
                _matchStarted = true;
                _onMatchStart?.Invoke();
                _matchStartEvent.Raise();
            }, TimedActionCountType.SCALEDTIME, MatchStartTime.Value);

            RoutineBehaviour.Instance.StartNewConditionAction(args =>
            {
                SetMatchResult();
                _onMatchOver?.Invoke();
                _matchOverEvent?.Raise(gameObject);
                _canPause = false;

                if (_matchResult == MatchResult.DRAW)
                    RoutineBehaviour.Instance.StartNewTimedAction(values => Restart(true), TimedActionCountType.SCALEDTIME, 2);
            },
            args => PlayerSpawner.P1HealthScript.HasExploded || PlayerSpawner.P2HealthScript.HasExploded || MatchTimerBehaviour.Instance.TimeUp);
        }

        public void LoadCharacterSelect()
        {
            Time.timeScale = 1;
            SceneManagerBehaviour.Instance.LoadScene("CharacterSelect");
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1;
            SceneManagerBehaviour.Instance.LoadScene(1);
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

        public void AddOnMatchCountdownStartAction(UnityAction action)
        {
            _onMatchCountdownStart.AddListener(action);
        }

        public void AddOnMatchPauseAction(UnityAction action)
        {
            _onMatchPause.AddListener(action);
        }

        public void AddOnMatchUnpauseAction(UnityAction action)
        {
            _onMatchUnpause.AddListener(action);
        }

        public void AddOnP1RingoutAction(UnityAction action)
        {
            _onP1RingOut.AddAction(action);
        }

        public void AddOnP2RingoutAction(UnityAction action)
        {
            _onP2RingOut.AddAction(action);
        }


        public void AddOnRingoutAction(UnityAction action)
        {
            _onP1RingOut.AddAction(action);
            _onP2RingOut.AddAction(action);
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


