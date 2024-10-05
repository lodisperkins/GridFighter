using FixedPoints;
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

    public static bool LocalGameStarted { get; private set; }
    public static bool OnlineGameStarted { get; private set; }
    public static bool IsHost { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        //gameManager.OnRunningChanged += OnRunningChanged;
        GameObject gob = new GameObject("PerfPanel");
        perf = gob.AddComponent<GgpoPerformancePanel>();
        perf.Setup();

        if (_startLocalGame)
            StartLocalGame();
    }

    public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex)
    {
        GGPORunner game = new GGPORunner("gridlockgladiators", new GridGame(), perfPanel);
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

    public string inpIp;
    public string inpPort;
    public string txtIp;
    public string txtPort;

    private GameManager gameManager => GameManager.Instance;
    private GgpoPerformancePanel perf;

    private void OnDestroy()
    {
        gameManager.OnRunningChanged -= OnRunningChanged;
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

    public void OnHostClick()
    {
        inpIp = "192.168.0.141";
        inpPort = "7000";
        txtIp = "169.254.160.242";
        txtPort = "7001";
        gameManager.StartGGPOGame(perf, GetConnections(), 0);
        IsHost = true;
    }

    public void OnRemoteClick()
    {
        inpIp = "169.254.160.242";
        inpPort = "7000";
        txtIp = "192.168.0.141";
        txtPort = "7000";
        gameManager.StartGGPOGame(perf, GetConnections(), 1);
    }

    private void OnLocalClick()
    {
        gameManager.StartLocalGame();
    }

    private void OnRunningChanged(bool isRunning)
    {
        gameObject.SetActive(!isRunning);
    }
}
