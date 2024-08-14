using SharedGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGGPO;

public class GridGameManager : GameManager
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void StartGGPOGame(IPerfUpdate perfPanel, IList<Connections> connections, int playerIndex)
    {
        GGPORunner game = new GGPORunner("gridlockgladiators", new GridGame(), perfPanel);
        game.Init(connections, playerIndex);
        StartGame(game);
    }

    public override void StartLocalGame()
    {
        StartGame(new LocalRunner(new GridGame()));
    }
}
