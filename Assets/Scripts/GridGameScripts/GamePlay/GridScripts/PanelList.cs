using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GridGame.GamePlay.GridScripts
{
    public delegate bool Condition(object[] args);
    [CreateAssetMenu(menuName = "Variables/PanelList")]
    public class PanelList : ScriptableObject
    {
        //raised when the a panel is somehow null
        [SerializeField] private Event OnPanelAssignmentFailed;
        //the list of panels tthis object holds
        [SerializeField]
        private List<PanelBehaviour> panels;
        public List<PanelBehaviour> Panels
        {
            get
            {
                return panels;
            }
            set
            {
                panels = value;
            }
        }
        //a list of panels meant to be in the players possesion only temporarily
        public List<PanelBehaviour> tempPanels;
        //the owner of the list of panels
        public string Owner;
        //initializes this panel list to have a populated list and an owner
        public void Init(List<PanelBehaviour> startPanels, string playername)
        {
            panels = new List<PanelBehaviour>();
            foreach (PanelBehaviour panel in startPanels)
            {
                panels.Add(panel);
            }
            Owner = playername;
            updateOwners();
        }
        public void Init(List<GameObject> startPanels, string playername)
        {
            panels = new List<PanelBehaviour>();
            foreach (GameObject panel in startPanels)
            {
                panels.Add(panel.GetComponent<PanelBehaviour>());
            }
            Owner = playername;
            updateOwners();
        }
        //creates an instance of this scriptable object
        public static PanelList CreateInstance(List<PanelBehaviour> startpanels, string playername)
        {
            var data = ScriptableObject.CreateInstance<PanelList>();
            data.Init(startpanels, playername);
            data.updateOwners();
            return data;
        }
        //Adds a panel to the list
        public void Add(PanelBehaviour panel)
        {
            panels.Add(panel);
        }
        //changes the owner of each panel in the list
        public void updateOwners()
        {
            int counter = 0;
            foreach (var panel in panels)
            {
                if (panel == null)
                {
                    Debug.Log("The panel at the index of " + counter + "is null");
                    OnPanelAssignmentFailed.Raise();
                    counter++;
                    continue;
                }
                panel.GetComponent<PanelBehaviour>().UpdateOwner(Owner);
                counter++;
            }
        }

        public bool Contains(PanelBehaviour panel)
        {
            return panels.Contains(panel);
        }

        public bool Contains(Vector2 position)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i].GetComponent<PanelBehaviour>();
                if (panel.Position == position)
                {
                    return true;
                }
            }
            return false;
        }

        public bool FindNeighborsForPanel(PanelBehaviour panelInFocus,out Dictionary<string,PanelBehaviour> panelsInRange)
        {
            panelsInRange = new Dictionary<string, PanelBehaviour>();
            //Used to find the position the block can be placed
            Vector2 DisplacementX = new Vector2(1, 0);
            Vector2 DisplacementY = new Vector2(0, 1);
            //Loops through all panels to find those whose position is the
            //player current position combined with x or y displacement
            foreach (PanelBehaviour panel in panels)
            {
                var coordinate = panel.Position;
                if ((panelInFocus.Position + DisplacementX) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add("Forward", panel);
                }
                else if ((panelInFocus.Position - DisplacementX) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add("Behind", panel);
                }
                else if ((panelInFocus.Position + DisplacementY) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add("Above", panel);
                }
                else if ((panelInFocus.Position - DisplacementY) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add("Below", panel);
                }
            }
            if(panelsInRange.Count <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public bool FindNeighborsForPanel(PanelBehaviour panelInFocus, out List<PanelBehaviour> panelsInRange)
        {
            panelsInRange = new List<PanelBehaviour>();
            //Used to find the position the block can be placed
            Vector2 DisplacementX = new Vector2(1, 0);
            Vector2 DisplacementY = new Vector2(0, 1);
            //Loops through all panels to find those whose position is the
            //player current position combined with x or y displacement
            foreach (PanelBehaviour panel in panels)
            {
                var coordinate = panel.Position;
                if ((panelInFocus.Position + DisplacementX) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add(panel);
                }
                else if ((panelInFocus.Position - DisplacementX) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add(panel);
                }
                else if ((panelInFocus.Position + DisplacementY) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add(panel);
                }
                else if ((panelInFocus.Position - DisplacementY) == coordinate)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add(panel);
                }
            }
            if (panelsInRange.Count <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public bool FindPanelsInRange(PanelBehaviour panelInFocus, out List<PanelBehaviour> panelsInRange,int range =1)
        {
            panelsInRange = new List<PanelBehaviour>();
            //Loops through all panels to find those whose position is the
            //player current position combined with x or y displacement
            foreach (PanelBehaviour panel in panels)
            {
                if(panel == panelInFocus)
                {
                    continue;
                }
                if (Mathf.Abs(panel.Position.x - panelInFocus.Position.x) <= range)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add(panel);
                }
                else if (Mathf.Abs(panel.Position.y - panelInFocus.Position.y) <= range)
                {
                    if (panelInFocus.IsBroken)
                    {
                        continue;
                    }
                    panelsInRange.Add(panel);
                }
            }
            if (panelsInRange.Count <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public bool GetPanels(Condition func, out List<PanelBehaviour> panelList)
        {
            panelList = new List<PanelBehaviour>();
            foreach (PanelBehaviour panel in panels)
            {
                object[] arg = { panel};
                if (func(arg))
                {
                    panelList.Add(panel.GetComponent<PanelBehaviour>());
                }
            }
            return panelList.Count > 0;
        }
        public IEnumerator GetEnumerator()
        {
            return panels.GetEnumerator();
        }
        /// <summary>
        /// removes the desired panel from the 
        /// lit of panels.
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        public bool RemovePanel(PanelBehaviour panel)
        {
            if (panels.Remove(panel))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Transfers a single panel from one player list of
        /// panels to another
        /// </summary>
        /// <param name="opponent_Panel_List"></param>
        /// <param name="panel_Index"></param>
        public void TransferPanel(PanelList opponent_Panel_List, int panel_Index)
        {
            PanelBehaviour temp  = panels[panel_Index];
            if (RemovePanel(panels[panel_Index]))
            {
                opponent_Panel_List.panels.Insert(0,temp);
            }
        }
        public void TransferPanel(PanelList opponent_Panel_List, PanelBehaviour panel)
        {
            PanelBehaviour temp = panel;
            if (RemovePanel(panel))
            {
                opponent_Panel_List.panels.Insert(0, temp);
            }
        }
        public void TransferPanel(PanelList opponent_Panel_List, int panel_Index,int insertionIndex)
        {
            PanelBehaviour temp  = panels[panel_Index];
            if (RemovePanel(panels[panel_Index]))
            {
                opponent_Panel_List.panels.Insert(insertionIndex,temp);
            }
        }

        public void SortPanelsP1()
        {
            int count = 0;
            int index;
            PanelBehaviour temp;
            for (int i = 4; i >= 0; i--)
            {
                for (int j = 0; j <= 3; j++)
                {
                    FindIndex(new Vector2(i, j),out index);
                    temp = panels[count];
                    panels[count] = panels[index];
                    panels[index] = temp;
                }
            }
        }
        
        public void SortPanelsP2()
        {
            int count = 0;
            int index;
            PanelBehaviour temp;
            for (int i = 5; i<= 9; i++)
            {
                for (int j = 0; j <= 3; j++)
                {
                    FindIndex(new Vector2(i, j),out index);
                    temp = panels[count];
                    panels[count] = panels[index];
                    panels[index] = temp;
                }
            }
        }
        /// <summary>
        /// Transfers the last row of panels from one player to another
        /// </summary>
        /// <param name="opponent_Panel_List"></param>
        public void SurrenderRow(PanelList opponent_Panel_List)
        {
            for(int i =0; i < 4 ; i ++)
            {
                TransferPanel(opponent_Panel_List, 0);
            }
        }
        //Returns the amount of panels currently in the list
        public int Count
        {
            get
            {
                return panels.Count;
            }
        }
        //Returns a panel at the given index
        public PanelBehaviour this[int index]
        {
            get
            {
                return panels[index];
            }
        }
        public bool FindPanel(Vector2 position, out PanelBehaviour outPanel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i].GetComponent<PanelBehaviour>();
                if (panel.Position == position)
                {
                    outPanel = panel;
                    return true;
                }
            }
            outPanel = null;
            return false;
        }
        //returns the index of a panel at the given coordinates
        public bool FindIndex(Vector2 position, out int index)
        {
            for(int i = 0; i < panels.Count; i++)
            {
                var panel = panels[i].GetComponent<PanelBehaviour>();
                if(panel.Position == position)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
        public static PanelList operator + (PanelList lhs,PanelList rhs)
        {
            PanelList newList = PanelList.CreateInstance(new List<PanelBehaviour>(),"");
            foreach(PanelBehaviour panel in lhs)
            {
                newList.Add(panel);
            }
            foreach (PanelBehaviour panel in rhs)
            {
                newList.Add(panel);
            }
            newList.Owner = lhs.Owner;
            return newList;
        }
        public int FindIndex(PanelBehaviour _panel)
        {
            return panels.IndexOf(_panel);
        }
    }


}
