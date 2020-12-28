using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using GridGame.GamePlay.GridScripts;
namespace GridGame
{
    public class SmartBulletBehaviour : BulletBehaviour {
        private PanelBehaviour _goal;
        private List<PanelBehaviour> _currentPath;
        private PanelBehaviour _currentPanelInPath;
        private PanelList playerPanels;
        private SeekBehaviour seekScript;
        private int panelCounter;
        
        // Use this for initialization
        void Start() {
            base.Start();
            lifetime = 3;
            _currentPath = new List<PanelBehaviour>();
            SetTarget();
        }
        public void ReconstructPath()
        {
	        _currentPath =new List<PanelBehaviour>();
	        PanelBehaviour temp = _goal;
	        while (temp != currentPanel)
	        {
		        _currentPath.Insert(0,temp);
		        temp = temp.previousPanel;
	        }
            SeekTarget();
        }
        public float Manhattan(PanelBehaviour panel)
        {
            return Math.Abs(panel.Position.x - _goal.Position.x) + Math.Abs(panel.Position.y - _goal.Position.y);
        }
        public List<PanelBehaviour> SortPanels(List<PanelBehaviour> nodelist)
        {
            PanelBehaviour temp;
            for (int i = 0; i < nodelist.Count; i++)
            {
                for (int j = 0; j < nodelist.Count - i -1; j++)
                {
                    if (nodelist[j].F > nodelist[j+1].F)
                    {
                        temp = nodelist[j];
                        nodelist[j] = nodelist[j+1];
                        nodelist[j+1] = temp;
                    }
                }
            }
            return nodelist;
        }
        public List<PanelBehaviour> FindNeighbors()
        {
            List<PanelBehaviour> panelsInRange = new List<PanelBehaviour>();
            if (playerPanels.FindNeighborsForPanel(_currentPanelInPath,out panelsInRange))
            {
                return panelsInRange;
            }
            Debug.Log("Couldn't find neighboors");
            return new List<PanelBehaviour>();
        }
        public void FindBestPath()
        {
            PanelBehaviour panel;
            PanelBehaviour startPanel = currentPanel;
            List<PanelBehaviour> openList = new List<PanelBehaviour>();
            openList.Add(startPanel);
            List<PanelBehaviour> closedList = new List<PanelBehaviour>();
            currentPanel.F = Manhattan(currentPanel);
            while (openList.Count > 0)
            {
                openList = SortPanels(openList);
                panel = openList[0];
                if (panel == _goal)
                {
                    ReconstructPath();
                    return;
                }
                openList.Remove(panel);
                closedList.Add(panel);
                _currentPanelInPath = panel;
                List<PanelBehaviour> neighboors = FindNeighbors();
                foreach (PanelBehaviour neighbor in neighboors)
                {
                    if (closedList.Contains(neighbor) || openList.Contains(neighbor))
                    {
                        continue;
                    }
                    else if (neighbor.Occupied && neighbor != _goal)
                    {
                        continue;
                    }
                    else
                    {
                        neighbor.G = panel.G + neighbor.G;
                        neighbor.F = neighbor.G + Manhattan(neighbor);
                        neighbor.previousPanel = panel;
                        openList.Add(neighbor);
                    }
                }
            }
        }
        public override void Reflect(string ownerOfReflector, int damageIncrease = 2, float speedScale = 1.5f) 
        {
            if (Owner == ownerOfReflector)
            {
                return;
            }
            ReverseOwner();
            DamageVal += damageIncrease;
            panelCounter = 0;
            lifetime = 3;
            _currentPath = new List<PanelBehaviour>();
            SetTarget();
        }
        public void SetTarget()
        {
            playerPanels = BlackBoard.p1PanelList + BlackBoard.p2PanelList;
            
            if(Owner == "Player1")
            {
                _goal = BlackBoard.p2Position;
                _currentPanel = BlackBoard.p1Position;
            }
            else
            {
                _goal = BlackBoard.p1Position;
                _currentPanel = BlackBoard.p2Position;
            }
            FindBestPath();
        }
        public void GoToNextPanel()
        {
            panelCounter++;
            if(panelCounter < _currentPath.Count)
            {
                seekScript.SetTarget(_currentPath[panelCounter].transform.position, 1);
            }
        }
        public void SeekTarget()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            seekScript = GetComponent<SeekBehaviour>();
            seekScript.Init(_currentPath[0].transform.position, rigidbody.velocity, 10, 1);
        }
        private void Update()
        {
            float distanceFromLastPanel = Vector3.Distance(transform.position, _currentPath[_currentPath.Count - 1].transform.position);
            if(distanceFromLastPanel <= .2f)
            {
                Destroy(TempObject);
            }
        }
    }
}
