using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.GridScripts;
using System;
using Lodis.Gameplay;

namespace Lodis.AI
{
    public sealed class AIUtilities
    {
        private class PanelNode
        {
            public PanelBehaviour panel;
            public PanelNode parent = null;
            public float gScore;
            public float hScore;
            public float fScore;
        }

        private AIUtilities() { }
        private static AIUtilities _instance = null;
        public static AIUtilities Instance
        {
            get
            {
                if (_instance == null)
                    return _instance = new AIUtilities();

                return _instance;
            }
        }

        public float CalculateManhattanDistance(PanelBehaviour panel, PanelBehaviour goal)
        {
            return Math.Abs(goal.Position.x - panel.Position.x) + Math.Abs(goal.Position.y - panel.Position.y);
        }

        private List<PanelNode> SortNodes(List<PanelNode> nodelist)
        {
            PanelNode temp;

            for (int i = 0; i < nodelist.Count; i++)
            {
                for (int j = 0; j < nodelist.Count; j++)
                {
                    if (nodelist[i].fScore > nodelist[j].fScore)
                    {
                        temp = nodelist[i];
                        nodelist[i] = nodelist[j];
                        nodelist[j] = temp;
                    }
                }
            }

            return nodelist;
        }

        private List<PanelBehaviour> ReconstructPath(PanelBehaviour startPanel, PanelBehaviour endPanel)
        {
            List<PanelBehaviour> currentPath = new List<PanelBehaviour>();
            PanelNode temp = new PanelNode { panel = endPanel };

            while (temp.panel != startPanel)
            {
                currentPath.Insert(0, temp.panel);
                temp = (PanelNode)temp.parent;
            }

            return currentPath;
        }

        private bool ContainsPanel(List<PanelNode> panelNodes, PanelBehaviour panel)
        {
            foreach (PanelNode node in panelNodes)
            {
                if (node.panel == panel)
                    return true;
            }

            return false;
        }

        public List<PanelBehaviour> GetPath(PanelBehaviour startPanel, PanelBehaviour endPanel, bool allowOccupiedPanels = false, GridAlignment alignment = GridAlignment.ANY)
        {
            PanelNode panelNode;
            List<PanelNode> openList = new List<PanelNode>();
            PanelNode start = new PanelNode { panel = startPanel };
            PanelNode end = new PanelNode { panel = endPanel };
            openList.Add(start);
            List<PanelNode> closedList = new List<PanelNode>();
            start.fScore =
                CalculateManhattanDistance(startPanel, endPanel);

            while (openList.Count > 0)
            {
                openList = SortNodes(openList);
                panelNode = openList[0];

                if (panelNode.panel == end.panel)
                {
                    return ReconstructPath(startPanel, endPanel);
                }

                openList.Remove(panelNode);
                closedList.Add(panelNode);

                foreach (PanelBehaviour neighbor in BlackBoardBehaviour.Instance.Grid.GetPanelNeighbors(panelNode.panel.Position))
                {
                    if (ContainsPanel(closedList, neighbor) || ContainsPanel(openList, neighbor))
                    {
                        continue;
                    }
                    else if (neighbor.Occupied && !allowOccupiedPanels)
                    {
                        continue;
                    }
                    else
                    {
                        PanelNode newNode = new PanelNode { panel = neighbor };
                        newNode.gScore += panelNode.gScore;
                        newNode.fScore = newNode.gScore + CalculateManhattanDistance(neighbor, endPanel);
                        newNode.parent = panelNode;
                        openList.Add(newNode);
                    }
                }
            }

            return new List<PanelBehaviour>();
        }
    }
}
