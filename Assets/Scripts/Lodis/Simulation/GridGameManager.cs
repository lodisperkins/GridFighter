using SharedGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGGPO;

public class GridGameManager : GameManager
{
    [Tooltip("Starts a local game immediately when the game starts.")]
    [SerializeField] private bool _startLocalGame;
    public static bool LocalGameStarted { get; private set; }
    public static bool OnlineGameStarted { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

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
}
