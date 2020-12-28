using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using GridGame.VariableScripts;

namespace GridGame.GamePlay.GridScripts
{
    public class GridBehaviour : MonoBehaviour
    {
       //The reference to player 1's panels
        [FormerlySerializedAs("p1Panels")] [FormerlySerializedAs("P1Panels")] [SerializeField] private  PanelList p1PanelsRef;
        //The reference to player 2's panels
        [FormerlySerializedAs("p2Panels")] [FormerlySerializedAs("P2Panels")] [SerializeField] private PanelList p2PanelsRef;
        public  GameObjectList bulletListP1;
        public  GameObjectList bulletListP2;
        private PanelList _originalP1Panels;
        private PanelList _originalP2Panels;
        public static PanelList globalPanelList;
        // player 1's current position on the grid
        [FormerlySerializedAs("P1Position")] [SerializeField]
        private Vector2Variable p1Position;
        // player 2's current position on the grid
        [FormerlySerializedAs("P2Position")] [SerializeField]
        private Vector2Variable p2Position;
        [FormerlySerializedAs("OnPanelsSwapped")] [SerializeField]
        private GridGame.Event onPanelsSwapped;
        //The amount of materials the players curretly have
        [FormerlySerializedAs("P1Materials")] [SerializeField]
        private IntVariable p1Materials;
        [FormerlySerializedAs("P2Materials")] [SerializeField]
        private IntVariable p2Materials;
        //the direction both players are facing
        [FormerlySerializedAs("P1Direction")] [SerializeField]
        private Vector2Variable p1Direction;
        [FormerlySerializedAs("P2Direction")] [SerializeField]
        private Vector2Variable p2Direction;
//The amount of materials the players have
        [SerializeField] private Material _p1Material;
        [SerializeField] private Material _p2Material;
        public GameObject proofPanel;
        [SerializeField] private PlayerSpawnBehaviour _player1;
        [SerializeField] private PlayerSpawnBehaviour _player2;
        public Event onStun;
        public Event onStopStun;
        [SerializeField]
        private GameObject _explosion;
        [SerializeField]
        private GridGame.Event _onExplosion;
        [SerializeField]
        private int _panelStealCost;
        [SerializeField]
        private float _panelReturnDelay;
        // Use this for initialization
        void Start()
        {
            
            P1AssignLists();
            P2AssignLists();
            BlackBoard.grid = this;
            _originalP1Panels = PanelList.CreateInstance(p1PanelsRef.Panels,"Player1");
            _originalP2Panels = PanelList.CreateInstance(p2PanelsRef.Panels,"Player2");
            globalPanelList = _originalP1Panels + _originalP2Panels;

        }

        private void Awake()
        {
            bulletListP1 =ScriptableObject.CreateInstance<GameObjectList>();
            bulletListP1.Init();
            bulletListP2 =ScriptableObject.CreateInstance<GameObjectList>();
            bulletListP2.Init();
            BlackBoard.p1PanelList = p1PanelsRef;
            BlackBoard.p2PanelList = p2PanelsRef;
        }

        //Sets player1 panels to the appropriate material and sets their owner to be player 1
        public void P1AssignLists()
        {
            AssignPanelMaterials();
            p1PanelsRef.updateOwners();
        }
        public void ExplodePanel(PanelBehaviour panel,bool breakPanel = false,float panelBreakTime = 1)
        {
            Instantiate(_explosion, panel.transform);
            _onExplosion.Raise();
            if (breakPanel)
            {
                panel.BreakPanel(panelBreakTime);
            }
        }
        public void ExplodePanel(Vector2 panelPosition, bool breakPanel = false, float panelBreakTime = 1)
        {
            PanelBehaviour panel;
            globalPanelList.FindPanel(panelPosition, out panel);
            Instantiate(_explosion, panel.transform);
            _onExplosion.Raise();
            if(breakPanel)
            {
                panel.BreakPanel(panelBreakTime);
            }
        }
        public void StunPlayer(float stunTime,string playerName)
        {
            if(playerName == "Player1")
            {
                StartCoroutine(Stun(stunTime, BlackBoard.Player1)); 
            }
            else if(playerName == "Player2")
            {
                StartCoroutine(Stun(stunTime, BlackBoard.Player2));
            }
        }
        IEnumerator Stun(float stunTime,GameObject player)
        {
            player.GetComponent<InputCustom.InputButtonBehaviour>().enabled = false;
            onStun.Raise(player);
            yield return new WaitForSeconds(stunTime);
            player.GetComponent<InputCustom.InputButtonBehaviour>().enabled = true;
            onStopStun.Raise(player);
        }
        public void GiveBackPanelsToP2()
        {
            for (int i = 0; i< p1PanelsRef.Count;i++)
            {
                if (_originalP2Panels.Contains(p1PanelsRef[i]))
                {
                    p1PanelsRef.TransferPanel(p2PanelsRef,p1PanelsRef.FindIndex(p1PanelsRef[i]));
                    i--;
                }
            }
            p2PanelsRef.SortPanelsP2();
            p1PanelsRef.updateOwners();
            p2PanelsRef.updateOwners();
        }
        public void GiveBackPanelsToP1()
        {
            for (int i = 0; i< p2PanelsRef.Count;i++)
            {
                if (_originalP1Panels.Contains(p2PanelsRef[i]))
                {
                    p2PanelsRef.TransferPanel(p1PanelsRef,p2PanelsRef.FindIndex(p2PanelsRef[i]));
                    i--;
                }
            }
            p1PanelsRef.SortPanelsP1();
            p1PanelsRef.updateOwners();
            p2PanelsRef.updateOwners();
        }
        //Sets player2 panels to the appropriate material and sets their owner to be player 2
        public void P2AssignLists()
        {
            AssignPanelMaterials();
            p2PanelsRef.updateOwners();
        }
        //Removes an entire row from player1 and gives it to player2
        public void SurrenderRowP1()
        {
            p1PanelsRef.SurrenderRow(p2PanelsRef);
            p1PanelsRef.updateOwners();
            p2PanelsRef.updateOwners();
        }
        //Removes an entire row from player2 and gives it to player1
        public void SurrenderRowP2()
        {
            p2PanelsRef.SurrenderRow(p1PanelsRef);
            p1PanelsRef.updateOwners();
            p2PanelsRef.updateOwners();
        }

        public PanelBehaviour GetPanelFromGlobalList(int index)
        {
            return globalPanelList[index];
        }

        public PanelBehaviour GetPanelFromGlobalList(Vector2 position)
        {
            PanelBehaviour panel;
            globalPanelList.FindPanel(position,out panel);
            return panel;
        }
        public int GetPanelIndexFromGlobalList(Vector2 position)
        {
            int index = -1;
            globalPanelList.FindIndex(position, out index);
            return index;
        }
        public PanelBehaviour GetPanelFromP1List(int index)
        {
            return p1PanelsRef[index];
        }

        public int GetIndexFromP1List(Vector2 position)
        {
            int index = -1;
            p1PanelsRef.FindIndex(position, out index);
            return index;
        }

        public int CountP1
        {
            get { return p1PanelsRef.Count; }
        }
        public int CountP2
        {
            get { return p2PanelsRef.Count; }
        }
        
        public PanelBehaviour GetPanelFromP2List(int index)
        {
            return p2PanelsRef[index];
        }

        public int GetIndexFromP2List(Vector2 position)
        {
            int index = -1;
            p2PanelsRef.FindIndex(position, out index);
            return index;
        }
        //Sets both players materials to either their red or blue variants
        public void AssignPanelMaterials()
        {
            foreach (PanelBehaviour panel in p1PanelsRef)
            {
                int counter = 0;
                if (panel == null)
                {
                    counter++;
                    continue;
                }
                panel.Init(_p1Material,_p2Material);
                counter++;
            }
            foreach (PanelBehaviour panel in p2PanelsRef)
            {
                int counter = 0;
                if (panel == null)
                {
                    counter++;
                    continue;
                }
                panel.Init(_p1Material,_p2Material);
                counter++;
            }
        }
        //Takes one panel from player two and gives it player one
        public void StealPanelP1()
        {
            Vector2 panelPosition = new Vector2((int)p1Position.Val.x + (int)p1Direction.X, (int)p1Position.Val.y+ (int)p1Direction.Y);
            int index = 0;
            //This checks for a diagnol input and returns if one is detected;
            if (Math.Abs(p1Direction.X) == 1 && Math.Abs(p1Direction.Y) == 1)
            {
                return;
            }
            if (p2PanelsRef.FindIndex(panelPosition, out index))
            {
                if (p1Materials.Val <30|| p2PanelsRef[index].GetComponent<PanelBehaviour>().Occupied)
                {
                    UnHighlightPanelsP1();
                    return;
                }
                p1Materials.Val -= 30;
                p2PanelsRef.TransferPanel(p1PanelsRef, index);
                UnHighlightPanelsP1();
                p1PanelsRef.updateOwners();
                p2PanelsRef.updateOwners();
                onPanelsSwapped.Raise();
                
            }
        }

        private IEnumerator ReturnPanel(int player, Vector2 panelPosition)
        {
            yield return new WaitForSeconds(_panelReturnDelay);
            int index = 0;
            if (player == 1)
            {
                if (p2PanelsRef.FindIndex(panelPosition, out index) && _originalP1Panels.Contains(panelPosition))
                {
                    p2PanelsRef.TransferPanel(p1PanelsRef, index);
                    p1PanelsRef.updateOwners();
                    p2PanelsRef.updateOwners();
                    onPanelsSwapped.Raise();
                }
            }
            else if (player == 2)
            {
                if (p1PanelsRef.FindIndex(panelPosition, out index) && _originalP2Panels.Contains(panelPosition))
                {
                    p1PanelsRef.TransferPanel(p2PanelsRef, index);
                    p2PanelsRef.updateOwners();
                    p1PanelsRef.updateOwners();
                    onPanelsSwapped.Raise();
                }
            }
        }

        public bool TempStealPanelP1(PanelBehaviour panel)
        {
            int index = 0;
            if (p2PanelsRef.FindIndex(panel.Position, out index))
            {
                if (p1Materials.Val < _panelStealCost || p2PanelsRef[index].GetComponent<PanelBehaviour>().Occupied)
                {
                    return false;
                }
                p1Materials.Val -= _panelStealCost;
                p2PanelsRef.TransferPanel(p1PanelsRef, index);
                p1PanelsRef.updateOwners();
                p2PanelsRef.updateOwners();
                onPanelsSwapped.Raise();

            }
            StartCoroutine(ReturnPanel(2, panel.Position));
            return true;
        }

        //takes one panel from player 1 and gives it to player two
        public void StealPanelP2()
        {
            Vector2 panelPosition = new Vector2((int)p2Position.Val.x + (int)p2Direction.X, (int)p2Position.Val.y + (int)p2Direction.Y);
            //This checks for a diagnol input and returns if one is detected;
            if (Math.Abs(p2Direction.X) == 1 && Math.Abs(p2Direction.Y) == 1)
            {
                return;
            }
            int index = 0;
            if (p1PanelsRef.FindIndex(panelPosition, out index))
            {
                if (p2Materials.Val < 30||p1PanelsRef[index].GetComponent<PanelBehaviour>().Occupied)
                {
                    onPanelsSwapped.Raise();
                    UnHighlightPanelsP2();
                    return;
                }
                p2Materials.Val -= 30;
                p1PanelsRef.TransferPanel(p2PanelsRef, index);
                UnHighlightPanelsP2();
                p2PanelsRef.updateOwners();
                p1PanelsRef.updateOwners();
                onPanelsSwapped.Raise();
            }
        }

        public bool TempStealPanelP2(PanelBehaviour panel)
        {
            int index = 0;
            if (p1PanelsRef.FindIndex(panel.Position, out index))
            {
                if (p2Materials.Val < _panelStealCost|| p1PanelsRef[index].GetComponent<PanelBehaviour>().Occupied)
                {
                    onPanelsSwapped.Raise();
                    return false;
                }
                p2Materials.Val -= _panelStealCost;
                p1PanelsRef.TransferPanel(p2PanelsRef, index);
                p2PanelsRef.updateOwners();
                p1PanelsRef.updateOwners();
                onPanelsSwapped.Raise();
            }
            StartCoroutine(ReturnPanel(1, panel.Position));
            return true;
        }

        //finds and highlights all nearby panels in player 2's list for player1
        public void FindNeighborsP1()
        {
            //Used to find the position the block can be placed
            Vector2 DisplacementX = new Vector2(1, 0);
            Vector2 DisplacementY = new Vector2(0, 1);
            p1PanelsRef.tempPanels = new List<PanelBehaviour>();
            //Loops through all panels to find those whose position is the
            //player current position combined with x or y displacement
            foreach (PanelBehaviour panel in p2PanelsRef)
            {
                var coordinate = panel.GetComponent<PanelBehaviour>().Position;
                if ((p1Position.Val + DisplacementX) == coordinate)
                {
                    p1PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
                else if ((p1Position.Val - DisplacementX) == coordinate)
                {
                    p1PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
                else if ((p1Position.Val + DisplacementY) == coordinate)
                {
                    p1PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
                else if ((p1Position.Val - DisplacementY) == coordinate)
                {
                    p1PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
            }
        }
        //unhighlights all previously highlighted panels for player1
        public void UnHighlightPanelsP1()
        {
            foreach (PanelBehaviour panel in p1PanelsRef.tempPanels)
            {
                panel.GetComponent<PanelBehaviour>().SelectionColor = Color.green;
                panel.GetComponent<PanelBehaviour>().Selected = false;
            }
        }
        //unhighlights all previously highlighted panels for player1
        public void UnHighlightPanelsP2()
        {
            foreach (PanelBehaviour panel in p2PanelsRef.tempPanels)
            {
                panel.GetComponent<PanelBehaviour>().SelectionColor = Color.green;
                panel.GetComponent<PanelBehaviour>().Selected = false;
            }
        }
        //finds and highlights all nearby panels in player 2's list for player1
        public void FindNeighborsP2()
        {
            //Used to find the position the block can be placed
            Vector2 DisplacementX = new Vector2(1, 0);
            Vector2 DisplacementY = new Vector2(0, 1);
            p2PanelsRef.tempPanels = new List<PanelBehaviour>();
            //Loops through all panels to find those whose position is the
            //player current position combined with x or y displacement
            foreach (PanelBehaviour panel in p1PanelsRef)
            {
                var coordinate = panel.GetComponent<PanelBehaviour>().Position;
                if ((p2Position.Val + DisplacementX) == coordinate)
                {
                    p2PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
                else if ((p2Position.Val - DisplacementX) == coordinate)
                {
                    p2PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
                else if ((p2Position.Val + DisplacementY) == coordinate)
                {
                    p2PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
                else if ((p2Position.Val - DisplacementY) == coordinate)
                {
                    p2PanelsRef.tempPanels.Add(panel);
                    panel.SelectionColor = Color.magenta;
                    panel.Selected = true;
                }
            }
        }
        private void Update()
        {
            BlackBoard.p1PanelList = p1PanelsRef;
            BlackBoard.p2PanelList = p2PanelsRef;
        }
    }
}
