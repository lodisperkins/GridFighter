using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridGame;
using GridGame.GamePlay;
using GridGame.GamePlay.GridScripts;
using GridGame.VariableScripts;

namespace GridGame
{
    public static class BlackBoard
    {
        //Updated in blockbehaviour in InitializeBlock func and OnDestroy func
        public static List<BlockBehaviour> p1Blocks = new List<BlockBehaviour>();
        public static List<BlockBehaviour> p2Blocks = new List<BlockBehaviour>();
        //Updated in playermovementbehaviour in update func
        public static PanelBehaviour p1Position = new PanelBehaviour();
        public static PanelBehaviour p2Position = new PanelBehaviour();
        //Set in playermovementbehaviour in start func
        public static GameObject Player1;
        public static GameObject Player2;
        //Updates in gridbehaviour in update func
        public static PanelList p1PanelList;
        public static PanelList p2PanelList;
        //Set in corebehaviour in start func
        public static GameObject p1Core;
        public static GameObject p2Core;
        public static IntVariable altAmmoAmountP1;
        public static IntVariable altAmmoAmountP2;
        public static IntVariable energyAmountP1;
        public static IntVariable energyAmountP2;
        public static GamePlay.GridScripts.GridBehaviour grid;
        public static int maxBlockHealth = 75;
        public static GamePlay.OtherScripts.HitStopBehaviour hitStopHandler;
    }
}


