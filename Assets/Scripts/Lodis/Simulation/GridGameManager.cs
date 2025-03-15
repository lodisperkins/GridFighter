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
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGGPO;

public class GridGameManager : GameManager
{
    [Tooltip("Starts a local game immediately when the game starts.")]
    [SerializeField] private bool _startLocalGame;
    [Tooltip("Enables certain features that are only active in online play.")]
    [SerializeField] private bool _testingLocalSaves;
    [Tooltip("Enables the AI to take control over the other client.")]
    [SerializeField] private bool _aiFightEnabled;
    [SerializeField] private Fixed32 _fixed32TestConversion;
    [SerializeField] private bool _testLatency;
    [ShowIf("_testLatency")]
    [SerializeField] private int _saveDelay;
    [ShowIf("_testLatency")]
    [SerializeField] private int _loadDelay;
    [SerializeField] private bool host;


    //---
    private GameManager _gameManager => GameManager.Instance;
    private GgpoPerformancePanel _perf;

    private int _lastFrameNumberLoaded;
    private int _lastFrameNumberSaved;

    private static GGPORunner _onlineGame;
    private static LocalRunner _localGame;
    private static bool _isHost;

    public static bool LocalGameStarted { get; private set; }
    public static bool OnlineGameStarted { get; private set; }
    public static bool AIFightEnabled { get; private set; }
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

    public static string inpIp;
    public static string inpPort;
    public static string txtIp;
    public static string txtPort;
    [SerializeField] private bool _canResimulate;
    private bool _hasSaved;

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

        SceneManager.sceneUnloaded += a => GridGame.OnSceneUnloaded();

        if (_startLocalGame)
        {
           StartLocalGame();
        }
    }

    public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex)
    {
        _onlineGame = new GGPORunner("gridlockgladiators", new GridGame(), perfPanel);
        _onlineGame.Init(connections, playerIndex);
        StartGame(_onlineGame);
        OnlineGameStarted = true;
        LocalGameStarted = false;
        _isHost = host;
    }

    public override void StartLocalGame()
    {
        _localGame = new LocalRunner(new GridGame());
        StartGame(_localGame);
        LocalGameStarted = true;
        OnlineGameStarted = false;
    }

    protected override void OnPreRunFrame()
    {
        base.OnPreRunFrame();
        currentFrame = _localGame.Game.Framenumber;
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
    public void OnOnlineClick()
    {
        _onlineGame?.Shutdown();

        //_isHost = !ClonesManager.IsClone();
        SceneManagerBehaviour.Instance.SetGameMode(GameMode.ONLINE);

        int playerIndex = IsHost ? 0 : 1;

        inpIp = "192.168.0.141";
        //Rose Ip
        //txtIp = "169.254.160.242";
        txtIp = "192.168.0.141";
        inpPort = "7000";
        txtPort = "7001";
        //_gameManager.StartGGPOGame(_perf, GetConnections(), playerIndex);
    }

    public void OnLocalClick()
    {
        _gameManager.StartLocalGame();
    }
    
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
        Resimulating = true;
        FramesToResimulate = FrameNumber - _lastFrameNumberSaved;
        _lastFrameNumberLoaded = FrameNumber;
        _localGame.OnTestLoad();
    }

    private void LateUpdate()
    {
        //Debug.Log(InputSystem.settings.updateMode);
        if (MatchManagerBehaviour.Instance == null || !_testLatency)
        {
            return;
        }

        if (FrameNumber - _lastFrameNumberSaved > _saveDelay && !_hasSaved)
        {
            OnTestSave();
            _hasSaved = true;
        }


        if (FrameNumber - _lastFrameNumberLoaded > _loadDelay && _hasSaved)
        {
            OnTestLoad();
            _hasSaved = false;
        }

    }
}
