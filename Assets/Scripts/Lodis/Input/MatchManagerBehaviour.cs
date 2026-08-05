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
using Types;
using FixedPoints;
using Lodis.FX;
using UnityEngine.InputSystem;
using Lodis.GridScripts;
using System.IO;

namespace Lodis.Gameplay
{
    public enum MatchResult
    {
        DRAW,
        P1WINS,
        P2WINS,
        UNDECIDED
    }

    /// <summary>
    /// Handles player spawning, match state, and match events. Also contains some debug functions for testing purposes.
    /// </summary>
    public class MatchManagerBehaviour : SimulationBehaviour
    {
        private static MatchManagerBehaviour _instance;

        [Header("Grid Logic References")]
        [Tooltip("Primary grid manager used to build and reset the arena for the match.")]
        [SerializeField] private GridScripts.GridBehaviour _grid;
        [Tooltip("Left ring barrier used for ring-out and sudden death transitions.")]
        [SerializeField] private RingBarrierBehaviour _ringBarrierL;
        [Tooltip("Right ring barrier used for ring-out and sudden death transitions.")]
        [SerializeField] private RingBarrierBehaviour _ringBarrierR;
        [Tooltip("Controller that manages sudden death rules and timers.")]
        [SerializeField] private SuddenDeathBehaviour _suddenDeathManager;

        [Header("UI/Feedback References")]
        [Tooltip("Pause-menu button that should be selected first when the match is paused.")]
        [SerializeField] private Button _firstSelectedPauseButton;
        [Tooltip("Pause menu root object toggled during pause flow.")]
        [SerializeField] private GameObject _pauseMenu;
        [Tooltip("Material used when collider hitboxes are visualized for debugging.")]
        [SerializeField] private Material _hitBoxMaterial;
        [Tooltip("Material used when collider hurtboxes are visualized for debugging.")]
        [SerializeField] private Material _hurtBoxMaterial;

        [Header("Match Options")]
        [Tooltip("Target frame rate for the match scene.")]
        [SerializeField] private int _targetFrameRate;
        [Tooltip("Countdown duration before players gain control at the start of a round.")]
        [SerializeField] private FloatVariable _matchStartTime;
        [Tooltip("If enabled, ring barriers ignore damage and cannot be broken normally.")]
        [SerializeField] private bool _invincibleBarriers;
        [Tooltip("If enabled, gameplay systems may treat energy resources as always full.")]
        [SerializeField] private bool _infiniteEnergy;
        [Tooltip("If enabled, gameplay systems may treat burst resources as always full.")]
        [SerializeField] private bool _infiniteBurst;
        [Tooltip("Initial Unity time scale applied when the match scene starts.")]
        [SerializeField] private float _timeScale = 1;

        [Header("Music")]
        [Tooltip("Music track played during a normal round.")]
        [SerializeField] private AudioClip _matchMusic;
        [Tooltip("Music track played once sudden death begins.")]
        [SerializeField] private AudioClip _suddenDeathMusic;

        [Header("Match Events")]
        [Tooltip("Invoked right before the application quits from this manager.")]
        [SerializeField] private UnityEvent _onApplicationQuit;
        [Tooltip("Invoked when player control is enabled and the round officially starts.")]
        [SerializeField] private UnityEvent _onMatchStart;
        [Tooltip("Invoked when a round countdown begins before players gain control.")]
        [SerializeField] private UnityEvent _onMatchCountdownStart;
        [Tooltip("Invoked when the match enters the paused state.")]
        [SerializeField] private UnityEvent _onMatchPause;
        [Tooltip("Invoked when the match leaves the paused state.")]
        [SerializeField] private UnityEvent _onMatchUnpause;
        [Tooltip("Invoked whenever the round is reset or restarted.")]
        [SerializeField] private UnityEvent _onMatchRestart;
        [Tooltip("Invoked when the manager decides the round has ended.")]
        [SerializeField] private UnityEvent _onMatchOver;
        [Tooltip("Invoked when player one rings out.")]
        [SerializeField] private UnityEvent _onP1RingOut;
        [Tooltip("Invoked when player two rings out.")]
        [SerializeField] private UnityEvent _onP2RingOut;
        [Tooltip("Invoked when player one loses the round.")]
        [SerializeField] private UnityEvent _onP1Lose;
        [Tooltip("Invoked when player two loses the round.")]
        [SerializeField] private UnityEvent _onP2Lose;

        [Tooltip("Custom event raised when a match restart is triggered.")]
        [SerializeField] private CustomEventSystem.Event _matchRestartEvent;
        [Tooltip("Custom event raised when the active round officially starts.")]
        [SerializeField] private CustomEventSystem.Event _matchStartEvent;
        [Tooltip("Custom event raised when the active round ends.")]
        [SerializeField] private CustomEventSystem.Event _matchOverEvent;

        private PlayerSpawnBehaviour _playerSpawner;
        private bool _isPaused;
        private MatchResult _matchResult;
        private GameMode _mode;

        private bool _canPause = true;
        private bool _suddenDeathActive;
        private bool _matchStarted;
        private bool _playerOutOfRing;
        private bool _collidersEnabled;

        public delegate void ColliderVisualEnableEvent(bool enabled);
        public event ColliderVisualEnableEvent OnColliderVisualsEnabled;

        private LerpAction _fxTimeScaleTween;

        private LerpAction _physicsTimeScaleLerp;
        private FixedAction _physicsTimeScaleAction;
        private FixedAction _fxTimeScaleAction;

        private int _lhsWins;
        private int _rhsWins;

        private CharacterExplosionBehaviour _characterExplosionBehaviour;

        /// <summary>
        /// Gets the active match manager instance in the scene.
        /// </summary>
        public static MatchManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(MatchManagerBehaviour)) as MatchManagerBehaviour;

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

        public bool InfiniteEnergy { get => _infiniteEnergy; private set => _infiniteEnergy = value; }
        public bool InvincibleBarriers { get => _invincibleBarriers; set => _invincibleBarriers = value; }
        public bool SuddenDeathActive { get => _suddenDeathActive; private set => _suddenDeathActive = value; }
        public PlayerSpawnBehaviour PlayerSpawner { get => _playerSpawner; private set => _playerSpawner = value; }
        public bool SuperInUse { get; internal set; }
        public bool PlayerOutOfRing { get => _playerOutOfRing; private set => _playerOutOfRing = value; }
        public int LhsWins { get => _lhsWins; private set => _lhsWins = value; }
        public int RhsWins { get => _rhsWins; private set => _rhsWins = value; }
        public FloatVariable MatchStartTime { get => _matchStartTime; set => _matchStartTime = value; }
        public bool InfiniteBurst { get => _infiniteBurst; set => _infiniteBurst = value; }
        public bool IsPaused { get => _isPaused; private set => _isPaused = value; }
        public bool MatchStarted { get => _matchStarted; private set => _matchStarted = value; }
        public Material HitBoxMaterial { get => _hitBoxMaterial; }
        public Material HurtBoxMaterial { get => _hurtBoxMaterial; }
        public bool CollidersEnabled { get => _collidersEnabled; private set => _collidersEnabled = value; }

        public override string LogName => "MatchManagerBehaviour";

        protected override void Awake()
        {
            _mode = (GameMode)SceneManagerBehaviour.Instance.CurrentGameMode.Value;

            _characterExplosionBehaviour = GetComponent<CharacterExplosionBehaviour>();
            _characterExplosionBehaviour.OnCharacterExplosionStart += OnPlayerExplosionStart;

            _grid.DestroyTempPanels();
            _grid.InvincibleBarriers = InvincibleBarriers;

            //Initialize grid
            _grid.CreateGrid();

            PlayerSpawner = GetComponent<PlayerSpawnBehaviour>();
            PlayerSpawner.SpawnEntitiesByMode(_mode);

            _onMatchRestart.AddListener(PlayerSpawner.ResetPlayers);
            _onMatchRestart.AddListener(() =>
            {
                CameraBehaviour.Instance.AlignmentFocus = GridScripts.GridAlignment.ANY;

                if (SuddenDeathActive)
                {
                    SoundManagerBehaviour.Instance.SetMusic(_suddenDeathMusic);
                    _suddenDeathManager?.BeginTimers();
                }
                else
                {
                    SoundManagerBehaviour.Instance.SetMusic(_matchMusic);
                    _suddenDeathManager?.ResetAll();
                }
            });

            FixedPointTimer.StartNewConditionAction(() =>
            {
                SetMatchResult();
                _onMatchOver?.Invoke();
                _matchOverEvent?.Raise(gameObject);
                _canPause = false;
                if (_matchResult == MatchResult.DRAW)
                    FixedPointTimer.StartNewTimedAction(() => Restart(true), 2);
            },
            args => PlayerSpawner.P1HealthScript.HasExploded || PlayerSpawner.P2HealthScript.HasExploded || MatchTimerBehaviour.Instance.TimeUp);

            AddOnRingoutAction(() => PlayerOutOfRing = true);

            Application.targetFrameRate = _targetFrameRate;

            Time.timeScale = _timeScale;
        }


        /// <summary>
        /// Starts the match countdown and enables player control when the countdown finishes.
        /// </summary>
        private void Start()
        {
            SetPlayerControlsActive(false);
            _canPause = false;

            _onMatchCountdownStart?.Invoke();

            FixedPointTimer.StartNewTimedAction(() =>
            {
                _canPause = true;
                SetPlayerControlsActive(true);
                MatchStarted = true;
                _onMatchStart?.Invoke();
                _matchStartEvent.Raise();
            }, MatchStartTime.FixedValue);
        }

        /// <summary>
        /// Evaluates the current match state and records the correct round result.
        /// </summary>
        private void SetMatchResult()
        {
            if (PlayerSpawner.P1HealthScript.HasExploded && PlayerSpawner.P2HealthScript.HasExploded && _suddenDeathActive)
            {
                _matchResult = MatchResult.UNDECIDED;
            }
            else if (PlayerSpawner.P1HealthScript.HasExploded && PlayerSpawner.P2HealthScript.HasExploded)
            {
                _matchResult = MatchResult.DRAW;
            }
            else if (PlayerSpawner.P2HealthScript.HasExploded)
            {
                _matchResult = MatchResult.P1WINS;
                _lhsWins++;
                _onP2Lose?.Invoke();
                CameraBehaviour.Instance.AlignmentFocus = GridScripts.GridAlignment.LEFT;
            }
            else if (PlayerSpawner.P1HealthScript.HasExploded)
            {
                _matchResult = MatchResult.P2WINS;
                _rhsWins++;
                _onP1Lose?.Invoke();
                CameraBehaviour.Instance.AlignmentFocus = GridScripts.GridAlignment.RIGHT;
            }
            else if (!_suddenDeathActive)
            {
                _matchResult = MatchResult.DRAW;
            }
        }

        /// <summary>
        /// Handles ring-out feedback when a character explosion begins.
        /// </summary>
        /// <param name="index">The exploded player index.</param>
        private void OnPlayerExplosionStart(int index)
        {
            if (index == 0)
                _onP1RingOut?.Invoke();
            else if (index == 1)
                _onP2RingOut?.Invoke();
        }

        /// <summary>
        /// Forces a match result value, then reevaluates the result side effects.
        /// </summary>
        /// <param name="resultID">Integer value matching the <see cref="MatchResult"/> enum.</param>
        public void SetMatchResult(int resultID)
        {
            _matchResult = (MatchResult)resultID;
            SetMatchResult();
        }

        /// <summary>
        /// Enables or disables collider debug visuals and notifies listeners.
        /// </summary>
        /// <param name="enabled">Whether collider visuals should be shown.</param>
        public void EnableColliderVisuals(bool enabled)
        {
            CollidersEnabled = enabled;
            OnColliderVisualsEnabled?.Invoke(CollidersEnabled);
        }

        /// <summary>
        /// Toggles collider debug visuals and notifies listeners of the new state.
        /// </summary>
        public void ToggleColliderVisuals()
        {
            CollidersEnabled = !CollidersEnabled;
            OnColliderVisualsEnabled?.Invoke(CollidersEnabled);
        }

        /// <summary>
        /// Temporarily changes the speed of time for the game.
        /// </summary>
        /// <param name="newTimeScale">The new time scale. 0 being no time passes and 1 being the normal speed.</param>
        /// <param name="speed">How long it takes to transition into the new time scale.</param>
        /// <param name="duration">How long the timescale will be this speed.</param>
        public void ChangeTimeScale(Fixed32 newTimeScale, Fixed32 speed, Fixed32 duration)
        {
            _fxTimeScaleTween = FixedLerp.To(() => Time.timeScale, x => Time.timeScale = x, newTimeScale, speed / 2);
            _fxTimeScaleTween.Unit = FixedTimeAction.UnitOfTime.Unscaled;

            _physicsTimeScaleLerp = FixedLerp.To(() => GridGame.TimeScale, x => GridGame.TimeScale = x, newTimeScale, speed);

            _fxTimeScaleAction = FixedPointTimer.StartNewTimedAction(() => Time.timeScale = 1, duration, FixedTimeAction.UnitOfTime.Unscaled);

            _physicsTimeScaleAction = FixedPointTimer.StartNewTimedAction(() => GridGame.TimeScale = 1, duration, FixedTimeAction.UnitOfTime.Unscaled);
        }

        /// <summary>
        /// Temporarily changes the speed of time for the game.
        /// </summary>
        /// <param name="newTimeScale">The new time scale. 0 being no time passes and 1 being the normal speed.</param>
        /// <param name="speed">How long it takes to transition into the new time scale.</param>
        /// <param name="duration">How long the timescale will be this speed.</param>
        public void ChangeSimulationTimeScale(Fixed32 newTimeScale, Fixed32 speed, Fixed32 duration)
        {
            _physicsTimeScaleLerp = FixedLerp.To(() => GridGame.TimeScale, x => GridGame.TimeScale = x, newTimeScale, speed);

            _physicsTimeScaleAction = FixedPointTimer.StartNewTimedAction(() => GridGame.TimeScale = 1, duration, FixedTimeAction.UnitOfTime.Unscaled);
        }

        /// <summary>
        /// Stops all active time-scale effects and restores both Unity and simulation time to normal.
        /// </summary>
        private void StopTimeScale()
        {
            GridGame.TimeScale = 1;
            Time.timeScale = 1;

            _physicsTimeScaleLerp?.Kill();
            _physicsTimeScaleAction?.Stop();
            _fxTimeScaleTween?.Kill();
            FixedPointTimer.StopAction(_fxTimeScaleAction);
        }

        /// <summary>
        /// Restores Unity and simulation time to normal without restarting the round.
        /// </summary>
        public void ResetTimeScale()
        {
            Time.timeScale = 1;
            GridGame.TimeScale = 1;
            _fxTimeScaleTween.Kill();
            FixedPointTimer.StopAction(_fxTimeScaleAction);
        }

        /// <summary>
        /// Enables or disables player input controllers for both match participants.
        /// </summary>
        /// <param name="value">Whether player controls should be active.</param>
        public void SetPlayerControlsActive(bool value)
        {
            BlackBoardBehaviour.Instance.Player1Controller.Enabled = value;
            BlackBoardBehaviour.Instance.Player2Controller.Enabled = value;
        }

        /// <summary>
        /// Toggles the infinite energy debug option.
        /// </summary>
        public void ToggleInfiniteEnergy()
        {
            InfiniteEnergy = !InfiniteEnergy;
        }

        /// <summary>
        /// Toggles barrier invincibility and updates the arena barriers to match.
        /// </summary>
        public void ToggleInvincibleBarriers()
        {
            InvincibleBarriers = !InvincibleBarriers;

            if (InvincibleBarriers)
            {
                _ringBarrierL.SetInvincibilityByCondition(condition => !InvincibleBarriers);
                _ringBarrierR.SetInvincibilityByCondition(condition => !InvincibleBarriers);
            }
        }

        /// <summary>
        /// Toggles the infinite burst debug option.
        /// </summary>
        public void ToggleInfiniteBurstEnergy()
        {
            InfiniteBurst = !InfiniteBurst;
        }

        /// <summary>
        /// Toggles the pause state, pause menu, and player control availability.
        /// </summary>
        public void TogglePauseMenu()
        {
            if (!_canPause || (_characterExplosionBehaviour.ExplodingPlayer1 || _characterExplosionBehaviour.ExplodingPlayer2))
                return;

            IsPaused = !IsPaused;
            Time.timeScale = Convert.ToInt32(!IsPaused);
            GridGame.TimeScale = Convert.ToInt32(!IsPaused);

            if (FXManagerBehaviour.Instance.SuperMoveEffectActive)
            {
                GridGame.TimeScale = 0;
            }

            GridGame.IsPaused = IsPaused;
            _timeScale = Time.timeScale;

            SetPlayerControlsActive(!IsPaused);

            if (IsPaused)
            {
                _onMatchPause?.Invoke();
                _firstSelectedPauseButton.OnSelect(null);
            }
            else
            {
                _onMatchUnpause.Invoke();
            }
        }

        /// <summary>
        /// Restarts the round and optionally starts it in sudden death mode.
        /// </summary>
        /// <param name="suddenDeathActive">Whether the restarted round should use sudden death rules.</param>
        public void Restart(bool suddenDeathActive = false)
        {
            PlayerSpawner.SuddenDeathActive = suddenDeathActive;
            SuddenDeathActive = suddenDeathActive;
            MatchTimerBehaviour.Instance.IsInfinite = suddenDeathActive;

            _onMatchRestart?.Invoke();
            _matchRestartEvent.Raise(gameObject);
            MatchStarted = false;
            PlayerOutOfRing = false;

            if (suddenDeathActive)
            {
                _ringBarrierL.Deactivate(false);
                _ringBarrierR.Deactivate(false);
            }

            if (IsPaused)
                TogglePauseMenu();

            SetPlayerControlsActive(false);
            _canPause = false;

            _onMatchCountdownStart?.Invoke();
            RoutineBehaviour.Instance.StartNewTimedAction(args =>
            {

                _canPause = true;
                SetPlayerControlsActive(true);
                MatchStarted = true;
                _onMatchStart?.Invoke();
                _matchStartEvent.Raise();
            }, TimedActionCountType.SCALEDTIME, MatchStartTime.FixedValue);

            RoutineBehaviour.Instance.StartNewConditionAction(args =>
            {
                SetMatchResult();
                _onMatchOver?.Invoke();
                _matchOverEvent?.Raise(gameObject);
                _canPause = false;

                if (_matchResult == MatchResult.DRAW)
                    RoutineBehaviour.Instance.StartNewTimedAction(values => Restart(true), TimedActionCountType.SCALEDTIME, 2);
            },CheckCanEndMatch);

            SoundManagerBehaviour.Instance.ResetMusicVolume();
            StopTimeScale();
        }

        /// <summary>
        /// Restarts the round after an unscaled delay.
        /// </summary>
        /// <param name="delay">Delay before restarting the round.</param>
        public void Restart(float delay)
        {
            RoutineBehaviour.Instance.StartNewTimedAction(args => Restart(), TimedActionCountType.UNSCALEDTIME, delay);
        }

        /// <summary>
        /// Determines whether current round conditions allow the match to end.
        /// </summary>
        /// <param name="args">Unused condition callback arguments.</param>
        /// <returns>True when a player has exploded or the timer has expired.</returns>
        private bool CheckCanEndMatch(params object[] args)
        {
            bool p1Exploded = PlayerSpawner.P1HealthScript.HasExploded && !_characterExplosionBehaviour.ExplodingPlayer2;
            bool p2Exploded = PlayerSpawner.P2HealthScript.HasExploded && !_characterExplosionBehaviour.ExplodingPlayer1;
            bool timeUp = MatchTimerBehaviour.Instance.TimeUp;

            return p1Exploded || p2Exploded || timeUp;
        }

        /// <summary>
        /// Leaves the match scene and loads the character select scene.
        /// </summary>
        public void LoadCharacterSelect()
        {
            IsPaused = false;
            Time.timeScale = 1;
            GridGame.TimeScale = 1;
            GridGame.IsPaused = false;
            _timeScale = Time.timeScale;

            if (IsPaused)
            {
                _onMatchPause?.Invoke();
                _firstSelectedPauseButton.OnSelect(null);
            }
            else
            {
                _onMatchUnpause.Invoke();
            }

            SceneManagerBehaviour.Instance.LoadScene("CharacterSelect");
        }

        /// <summary>
        /// Leaves the match scene and returns to the main menu scene.
        /// </summary>
        public void ReturnToMainMenu()
        {
            IsPaused = false;
            Time.timeScale = 1;
            GridGame.TimeScale = 1;
            GridGame.IsPaused = false;
            _timeScale = Time.timeScale;

            if (IsPaused)
            {
                _onMatchPause?.Invoke();
                _firstSelectedPauseButton.OnSelect(null);
            }
            else
            {
                _onMatchUnpause.Invoke();
            }
            SceneManagerBehaviour.Instance.LoadScene(1);
        }

        /// <summary>
        /// Invokes quit callbacks and requests the application to close.
        /// </summary>
        public void QuitApplication()
        {
            _onApplicationQuit?.Invoke();
            Application.Quit();
        }

        /// <summary>
        /// Registers a callback for application quit.
        /// </summary>
        public void AddOnApplicationQuitAction(UnityAction action)
        {
            _onApplicationQuit.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for match restarts.
        /// </summary>
        public void AddOnMatchRestartAction(UnityAction action)
        {
            _onMatchRestart.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for match over events.
        /// </summary>
        public void AddOnMatchOverAction(UnityAction action)
        {
            _onMatchOver.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for match start.
        /// </summary>
        public void AddOnMatchStartAction(UnityAction action)
        {
            _onMatchStart.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for the round countdown start.
        /// </summary>
        public void AddOnMatchCountdownStartAction(UnityAction action)
        {
            _onMatchCountdownStart.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for match pause.
        /// </summary>
        public void AddOnMatchPauseAction(UnityAction action)
        {
            _onMatchPause.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for match unpause.
        /// </summary>
        public void AddOnMatchUnpauseAction(UnityAction action)
        {
            _onMatchUnpause.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for player one ring-outs.
        /// </summary>
        public void AddOnP1RingoutAction(UnityAction action)
        {
            _onP1RingOut.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for player two ring-outs.
        /// </summary>
        public void AddOnP2RingoutAction(UnityAction action)
        {
            _onP2RingOut.AddListener(action);
        }


        /// <summary>
        /// Registers the same callback for both player ring-out events.
        /// </summary>
        public void AddOnRingoutAction(UnityAction action)
        {
            _onP1RingOut.AddListener(action);
            _onP2RingOut.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for player one losses.
        /// </summary>
        public void AddOnP1LoseAction(UnityAction action)
        {
            _onP1Lose.AddListener(action);
        }

        /// <summary>
        /// Registers a callback for player two losses.
        /// </summary>
        public void AddOnP2LoseAction(UnityAction action)
        {
            _onP2Lose.AddListener(action);
        }

        /// <summary>
        /// Handles lightweight keyboard-only debug shortcuts while the match is running.
        /// </summary>
        private void Update()
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                _playerSpawner.P1HealthScript.ResetHealth();
                _playerSpawner.P2HealthScript.ResetHealth();

                _ringBarrierL.ResetHealth();
                _ringBarrierR.ResetHealth();
            }
        }

        protected override string[] GetLogItems()
        {
            return new string[]
            {
                $"SuddenDeathActive={_suddenDeathActive}",
                $"MatchStarted={_matchStarted}",
                $"PlayerOutOfRing={_playerOutOfRing}",
                $"MatchResult={_matchResult}",
                $"LhsWins={_lhsWins}",
                $"RhsWins={_rhsWins}"
            };
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(_suddenDeathActive);
            bw.Write(_matchStarted);
            bw.Write(_playerOutOfRing);
            bw.Write((int)_matchResult);
            bw.Write(_lhsWins);
            bw.Write(_rhsWins);
        }

        public override void Deserialize(BinaryReader br)
        {
            _suddenDeathActive = br.ReadBoolean();
            _matchStarted = br.ReadBoolean();
            _playerOutOfRing = br.ReadBoolean();
            _matchResult = (MatchResult)br.ReadInt32();
            _lhsWins = br.ReadInt32();
            _rhsWins = br.ReadInt32();
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


