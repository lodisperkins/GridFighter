using CustomEventSystem;
using FixedPoints;
using Lodis.Gameplay;
using Lodis.Input;
using Lodis.Utility;
using NaughtyAttributes;
#if UNITY_EDITOR
using ParrelSync;
#endif
using SharedGame;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Types;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGGPO;
using Event = CustomEventSystem.Event;

public class GridGameManager : GameManager
{
    public enum RollbackDebugSessionMode
    {
        None,
        Record,
        Playback
    }

    public enum SyncTestType
    {
        None,
        LocalMultiplayer,
        LocalAI
    }

    [Tooltip("Starts a local game immediately when the game starts.")]
    [SerializeField] private bool _startLocalGame;

    [SerializeField] private bool _opponentSceneLoaded;
    [SerializeField] private bool host;
    [SerializeField] private Event _onStartLookingForOnlineMatch;
    [SerializeField] private Event _onFoundOnlineMatch;

    [Header("Debug")]

    [Tooltip("Enables certain features that are only active in online play.")]
    [SerializeField] private bool _testingLocalSaves;
    [Tooltip("Enables the AI to take control over the other client.")]
    [SerializeField] private bool _aiFightEnabled;
    [SerializeField] private Fixed32 _fixed32TestConversion;
    [SerializeField] private long _fixed32RawValueTestConversion;
    [ReadOnly]
    [SerializeField] private float _fixed32RawValueAsFloat;
    [SerializeField] private SyncTestType _syncTestType;
    [SerializeField] private RollbackDebugSessionMode _rollbackDebugSessionMode;
    [SerializeField] private string _lhsRollbackDebugRecordingName = "SyncTestP1";
    [SerializeField] private string _rhsRollbackDebugRecordingName = "SyncTestP2";
    [SerializeField] private bool _rollbackDebugPlaybackPlayOnce;

    //---
    private GameManager _gameManager => GameManager.Instance;
    private GgpoPerformancePanel _perf;

    private int _lastFrameNumberLoaded;
    private int _lastFrameNumberSaved;

    private static GGPORunner _onlineGame;
    private static LocalRunner _localGame;
    private static bool _isHost;

    private bool _hasSaved;
    private int _framesSinceLastRollback;
    private bool _localSceneLoaded;

    public static bool LocalGameStarted { get; private set; }

    private Coroutine _lookForMatchRoutine;

    public static bool OnlineGameStarted { get; private set; }
    public static bool AIFightEnabled { get; private set; }
    /// <summary>
    /// True once both the local client and the remote client have reported that
    /// their online battle scenes are fully loaded and ready to activate.
    /// </summary>
    public bool BothScenesReady => _localSceneLoaded && _opponentSceneLoaded;
    public static int FrameNumber
    {
        get
        {
            if (OnlineGameStarted)
                return _onlineGame.Game.Framenumber;
            else
                return _localGame.Game.Framenumber;
        }
    }

    public static bool IsHost 
    {
        get
        {
            if (!OnlineGameStarted)
                return false;

            return _isHost;
        }
        private set => _isHost = value;
    }

    public static string LocalIP
    {
        get
        {
            return inpIp;
        }
    }

    public static string RemoteIP
    {
        get
        {
            return txtIp;
        }
    }

    public static bool TestingLocalSaves { get; set; }
    public static SyncTestType CurrentSyncTestType { get; set; }
    public static RollbackDebugSessionMode CurrentRollbackDebugSessionMode { get; private set; }
    public static bool CurrentRollbackDebugPlaybackPlayOnce { get; private set; }

    public static bool ShouldUseRollbackDebugSession
    {
        get
        {
#if SYNC_TEST
            return CurrentRollbackDebugSessionMode != RollbackDebugSessionMode.None;
#else
            return false;
#endif
        }
    }

    public static bool IsResimulating
    {
        get
        {
            if (OnlineGameStarted)
                return _onlineGame.IsResimulating;

            return false;
        }
    }

    public static string inpIp;
    public static string inpPort;
    public static string txtIp;
    public static string txtPort;
    public static int localPlayerIndex;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        //gameManager.OnRunningChanged += OnRunningChanged;
        GameObject gob = new GameObject("PerfPanel");
        gob.transform.parent = transform;
        _perf = gob.AddComponent<GgpoPerformancePanel>();
        _perf.Setup();
        TestingLocalSaves = _testingLocalSaves;
        InputSystem.settings.maxEventBytesPerUpdate = 0;
        AIFightEnabled = _aiFightEnabled;

        SceneManagerBehaviour.Instance.OnLoadScene += GridGame.OnSceneChange;
        SceneManager.sceneLoaded += OnSceneLoaded;

        CurrentSyncTestType = _syncTestType;
        CurrentRollbackDebugSessionMode = _rollbackDebugSessionMode;
        CurrentRollbackDebugPlaybackPlayOnce = _rollbackDebugPlaybackPlayOnce;

        if (_startLocalGame)
        {
           StartLocalGame();
        }
    }

    private void OnValidate()
    {
        // Keep an inspector-friendly reverse conversion next to the Fixed32 test field so
        // raw values can be pasted in directly and viewed as regular floats.
        _fixed32RawValueAsFloat = (float)(double)new Fixed32(_fixed32RawValueTestConversion);
        CurrentRollbackDebugSessionMode = _rollbackDebugSessionMode;
        CurrentRollbackDebugPlaybackPlayOnce = _rollbackDebugPlaybackPlayOnce;
    }

    public static string GetRollbackDebugRecordingName(int playerNumber, string fallbackName = null)
    {
        string sceneName = playerNumber == 1
            ? SceneManagerBehaviour.Instance?.LhsRecordingName
            : SceneManagerBehaviour.Instance?.RhsRecordingName;

        if (!string.IsNullOrWhiteSpace(sceneName))
            return sceneName;

        GridGameManager manager = GameManager.Instance as GridGameManager;
        string inspectorName = playerNumber == 1
            ? manager?._lhsRollbackDebugRecordingName
            : manager?._rhsRollbackDebugRecordingName;

        if (!string.IsNullOrWhiteSpace(inspectorName))
            return inspectorName;

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return playerNumber == 1 ? fallbackName + "_P1" : fallbackName + "_P2";

        return playerNumber == 1 ? "SyncTestP1" : "SyncTestP2";
    }

    protected override void OnPreRunFrame()
    {
        base.OnPreRunFrame();
        currentFrame = _localGame.Game.Framenumber;
    }

    //----Local Stuff
    public override void StartLocalGame()
    {
        _localGame = new LocalRunner(new GridGame());
        StartGame(_localGame);
        LocalGameStarted = true;
        OnlineGameStarted = false;
    }

    public void OnLocalClick()
    {
        _gameManager.StartLocalGame();
    }

    //----Online Stuff
    public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex)
    {
        _onlineGame = new GGPORunner("gridlockgladiators", new GridGame(), perfPanel);
        _onlineGame.Init(connections, localPlayerIndex);
        StartGame(_onlineGame);
        OnlineGameStarted = true;
        LocalGameStarted = false;
        _isHost = host;
    }

    private List<Connections> GetConnections()
    {
        var list = new List<Connections>();
        list.Add(new Connections()
        {
            ip = inpIp,
            port = ushort.Parse(inpPort),
            spectator = false
        });
        list.Add(new Connections()
        {
            ip = txtIp,
            port = ushort.Parse(txtPort),
            spectator = false
        });
        return list;
    }

    [Button]
    public void OnOnlineClick(int onlineMatchType)
    {
        _onlineGame?.Shutdown();

        //_isHost = !ClonesManager.IsClone();
        SceneManagerBehaviour.Instance.SetGameMode(GameMode.ONLINE);

        LocalGameStarted = false;

        if (_lookForMatchRoutine == null)
            _lookForMatchRoutine = StartCoroutine(LookForOnlineMatch(onlineMatchType));
    }

    public void OnCancelMatchSearch()
    {
        _isHost = false;
        if (_lookForMatchRoutine != null)
        {
            StopCoroutine(_lookForMatchRoutine);
            _lookForMatchRoutine = null;
        }
    }

    public void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        //We only care about this if we're in online mode and we've loaded the battle scene
        if (!SceneManagerBehaviour.Instance.IsOnlineGameMode || !SceneManagerBehaviour.Instance.StartingFight)
            return;

        NotifyLocalSceneLoaded();

        StartCoroutine(StartOnlineGame());
    }

    private IEnumerator StartOnlineGame()
    {
        yield return new WaitUntil(() => BothScenesReady);

        StartGGPOGame(_perf, GetConnections(), localPlayerIndex);
    }

    /// <summary>
    /// Clears the local and remote scene-ready flags before starting a fresh
    /// online battle scene load.
    /// </summary>
    public void ResetSceneReadyState()
    {
        // Clear the scene-ready handshake whenever we begin loading a new online
        // battle scene so stale state from a previous load cannot carry over.
        _localSceneLoaded = false;
        _opponentSceneLoaded = false;
    }

    /// <summary>
    /// Marks this client's battle scene as loaded to Unity's ready-to-activate
    /// point so the online handshake can wait for the remote client.
    /// </summary>
    public void NotifyLocalSceneLoaded()
    {
        // Called once this client has loaded the battle scene to Unity's
        // ready-to-activate state (progress 0.9, before Awake/Start fire).
        _localSceneLoaded = true;

        // TODO: Replace this with a real network message so the remote client can
        // call NotifyOpponentSceneLoaded when it receives our ready signal.

#if SYNC_TEST
        _opponentSceneLoaded = true;
#endif
    }

    /// <summary>
    /// Marks the remote client's battle scene as fully loaded and ready to activate.
    /// This should be called by the networking layer after receiving the remote ready signal.
    /// </summary>
    public void NotifyOpponentSceneLoaded()
    {
        // Called by the networking layer after the remote client confirms its
        // battle scene is also fully loaded and ready to activate.
        _opponentSceneLoaded = true;
    }

    private IEnumerator LookForOnlineMatch(int onlineMatchType)
    {
        //TO DO: Replace this with actually getting match info from a server or something

        _onStartLookingForOnlineMatch.Raise();

        //For now, just wait a few seconds to simulate looking for a match, then start the game
        yield return new WaitForSeconds(3f);

#if SYNC_TEST 
        localPlayerIndex = 0;
#else
        localPlayerIndex = IsHost ? 0 : 1;
#endif

        inpIp = "192.168.0.141";
        //Rose Ip
        //txtIp = "169.254.160.242";
        txtIp = "192.168.0.141";
        inpPort = "7000";
        txtPort = "7001";

        _lookForMatchRoutine = null;

        _onFoundOnlineMatch.Raise();

        SceneManagerBehaviour.Instance.LoadCharacterSelectWithDelay(5);
    }


    //---Debug
    [Button]
    public void OnTestSave()
    {
        TestingLocalSaves = true;
        _lastFrameNumberSaved = FrameNumber;
        _localGame.OnTestSave();
        InputBehaviour.TestInputList.Clear();
    }

    [Button]
    public void OnTestLoad()
    {
        TestingLocalSaves = true;
        _lastFrameNumberLoaded = FrameNumber;

        _localGame.OnTestLoad();
    }
}
