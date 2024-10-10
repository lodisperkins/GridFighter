using FixedPoints;
using NaughtyAttributes;
using ParrelSync;
using SharedGame;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;
using UnityEngine.UI;
using UnityGGPO;

public class GridGameManager : GameManager
{
    [Tooltip("Starts a local game immediately when the game starts.")]
    [SerializeField] private bool _startLocalGame;
    [SerializeField] private Fixed32 _fixed32TestConversion;
    private GameManager gameManager => GameManager.Instance;
    private GgpoPerformancePanel perf;
    private GGPORunner game;

    public static bool LocalGameStarted { get; private set; }
    public static bool OnlineGameStarted { get; private set; }
    public static bool IsHost { get; private set; }

    public string inpIp;
    public string inpPort;
    public string txtIp;
    public string txtPort;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        //gameManager.OnRunningChanged += OnRunningChanged;
        GameObject gob = new GameObject("PerfPanel");
        gob.transform.parent = transform;
        perf = gob.AddComponent<GgpoPerformancePanel>();
        perf.Setup();

        if (_startLocalGame)
            StartLocalGame();
    }

    public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex)
    {
        game = new GGPORunner("gridlockgladiators", new GridGame(), perfPanel);
        game.Init(connections, playerIndex);
        StartGame(game);
        OnlineGameStarted = true;
        LocalGameStarted = false;
    }

    public override void StartLocalGame()
    {
        StartGame(new LocalRunner(new GridGame()));
        LocalGameStarted = true;
        OnlineGameStarted = false;
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
        game?.Shutdown();

        IsHost = !ClonesManager.IsClone();

        int playerIndex = IsHost ? 0 : 1;

        inpIp = "127.0.0.1";
        txtIp = "127.0.0.1";

        inpPort = "7000";
        txtPort = "7001";
        gameManager.StartGGPOGame(perf, GetConnections(), playerIndex);
    }

    public void OnLocalClick()
    {
        gameManager.StartLocalGame();
    }
}
