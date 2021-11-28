using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.GridScripts;
using System;
using Lodis.Gameplay;

namespace Lodis.AI
{
    /// <summary>
    /// Class for AI utility function like pathfinding, 
    /// </summary>
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

        /// <summary>
        /// The static instance of this class
        /// </summary>
        public static AIUtilities Instance
        {
            get
            {
                if (_instance == null)
                    return _instance = new AIUtilities();

                return _instance;
            }
        }

        /// <summary>
        /// Calculates distance between two panels without including diagnols
        /// </summary>
        /// <param name="panel">The panel to start from</param>
        /// <param name="goal">The panel the path ends</param>
        public float CalculateManhattanDistance(PanelBehaviour panel, PanelBehaviour goal)
        {
            return Math.Abs(panel.Position.x - goal.Position.x) + Math.Abs(panel.Position.y - goal.Position.y);
        }

        /// <summary>
        /// A custom heuristic for path finding that gets the world distance between two panels
        /// </summary>
        /// <param name="panel">The starting panel</param>
        /// <param name="goal">The end of the path</param>
        private float CustomHeuristic(PanelBehaviour panel, PanelBehaviour goal)
        {
            return Vector3.Distance(goal.Position, panel.Position);
        }

        /// <summary>
        /// Finds the distance between two panels while including diagnol distance
        /// </summary>
        /// <param name="panel">The panel the path starts at</param>
        /// <param name="goal">The panel the path ends with</param>
        /// <returns></returns>
        public float CalculateDiagnolDistance(PanelBehaviour panel, PanelBehaviour goal)
        {
            float dx = Math.Abs(panel.Position.x - goal.Position.x);
            float dy = Math.Abs(panel.Position.y - goal.Position.y);
            return 2 * (dx + dy) + (3 - 2 * 2) * Math.Min(dx, dy);
        }

        /// <summary>
        /// Sorts nodes to be in order from lowest to highest f score using bubble sort
        /// </summary>
        /// <param name="nodelist">The list of nodes to sort</param>
        /// <returns>The sorted list</returns>
        private List<PanelNode> SortNodes(List<PanelNode> nodelist)
        {
            PanelNode temp;

            for (int i = 0; i < nodelist.Count - 1; i++)
            {
                for (int j = 0; j < nodelist.Count - i - 1; j++)
                {
                    if (nodelist[j].fScore > nodelist[j + 1].fScore)
                    {
                        temp = nodelist[j + 1];
                        nodelist[j + 1] = nodelist[j];
                        nodelist[j] = temp;
                    }
                }
            }

            return nodelist;
        }

        /// <summary>
        /// Creates a list of panels that represent the path found
        /// </summary>
        /// <param name="startPanel">The panel the path starts from</param>
        /// <param name="endPanel">The panel the path ends with</param>
        private List<PanelBehaviour> ReconstructPath(PanelNode startPanel, PanelNode endPanel)
        {
            List<PanelBehaviour> currentPath = new List<PanelBehaviour>();

            //Travels backwards from goal node using the node parent until it reaches the starting node
            PanelNode temp =  endPanel;
            while (temp != null)
            {
                //Insert each panel at the beginning of the list so that the path is in the correct order
                currentPath.Insert(0, temp.panel);
                temp = temp.parent;
            }

            return currentPath;
        }

        /// <summary>
        /// Gets whether or not the panel is in the given list
        /// </summary>
        /// <param name="panelNodes">The list to look for the panel in</param>
        /// <param name="panel">The panel to search for in the list</param>
        /// <returns>Whether or not the panel was within the list</returns>
        private bool ContainsPanel(List<PanelNode> panelNodes, PanelBehaviour panel)
        {
            //Loop until a panel that matches the argument is found
            foreach (PanelNode node in panelNodes)
            {
                if (node.panel == panel)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Uses A* to find a path from the starting panel to the end panel
        /// </summary>
        /// <param name="startPanel">The panel where the path will start</param>
        /// <param name="endPanel">The panel where the path will end</param>
        /// <param name="allowOccupiedPanels">Whether or not the path should avoid panels that are occupied</param>
        /// <param name="alignment">The grid alignment this path can go through</param>
        /// <returns>A list containing the constructed path</returns>
        public List<PanelBehaviour> GetPath(PanelBehaviour startPanel, PanelBehaviour endPanel, bool allowOccupiedPanels = false, GridAlignment alignment = GridAlignment.ANY)
        {
            PanelNode panelNode;
            List<PanelNode> openList = new List<PanelNode>();
            PanelNode start = new PanelNode { panel = startPanel };
            PanelNode end = new PanelNode { panel = endPanel };
            openList.Add(start);
            List<PanelNode> closedList = new List<PanelNode>();
            start.fScore =
                CustomHeuristic(startPanel, endPanel);

            while (openList.Count > 0)
            {
                openList = SortNodes(openList);
                panelNode = openList[0];

                if (panelNode.panel == end.panel)
                {
                    return ReconstructPath(start, panelNode);
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
                        newNode.fScore = newNode.gScore + CustomHeuristic(neighbor, endPanel);
                        newNode.parent = panelNode;
                        openList.Add(newNode);
                    }
                }
            }

            return new List<PanelBehaviour>();
        }
    }
}
