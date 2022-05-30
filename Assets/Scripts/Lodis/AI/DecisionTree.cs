using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Lodis.AI
{
    public delegate void SaveLoadEvent();


    [System.Serializable]
    public class DecisionTree
    {
        [SerializeField]
        protected List<TreeNode> _nodeCache;
        protected string SaveLoadPath;
        [SerializeField]
        private TreeNode _root;
        [SerializeField]
        private float _compareThreshold;
        public SaveLoadEvent OnSave;
        public SaveLoadEvent OnLoad;
        public int MaxDecisionsCount = 400;

        /// <param name="root">The root decision of the tree</param>
        /// <param name="compareThreshold">How similar nodes have to be to a sitation to be choosen as the right answer</param>
        public DecisionTree(float compareThreshold = 0.95f)
        {
            _compareThreshold = compareThreshold;
            _nodeCache = new List<TreeNode>();
        }

        /// <summary>
        /// Gets a decision that closely matches the node
        /// </summary>
        /// <param name="similarNode">A node containing information about the current game state.</param>
        /// <param name="args">Any additional arguments that need to be passed to the nodes</param>
        /// <returns>A decision that fits the situation the most. May randomly choose a less accurate decision to avoid repetition.</returns>
        public TreeNode GetDecision(TreeNode similarNode, params object[] args)
        {
            TreeNode decision = _root;
            //Loop until the decision is assigned or a dead end is reached
            while (decision != null)
            {
                /// Return the decision if it's similar enough to the current situation.
                /// Subtracts it by a random value to add mix ups
                if (decision.Compare(similarNode) >= _compareThreshold - Random.Range(0, TreeNode.RandomDecisionConstant * 0.1f))
                    return decision;

                //Calculate the weight for both children
                float leftWeight = 0;
                float rightWeight = 0;
                if (decision.Left != null)
                    leftWeight = decision.Left.GetTotalWeight(_root, args) + decision.Left.Compare(similarNode);

                if (decision.Right != null)
                    rightWeight = decision.Right.GetTotalWeight(_root, args) + decision.Right.Compare(similarNode);

                //Go to the child with the larger weight.
                if (rightWeight > leftWeight || float.IsNaN(rightWeight))
                    decision = decision.Right;
                else
                    decision = decision.Left;
            }

            return decision;
        }

        /// <summary>
        /// Adds a new decision to the tree
        /// </summary>
        /// <param name="decision">The new decision node to add</param>
        public TreeNode AddDecision(TreeNode decision)
        {
            if (_nodeCache.Count >= MaxDecisionsCount) return null;

            if (_root == null)
            {
                _root = decision;
                Debug.Log("Added decision to cache" + _nodeCache.Count);
                _nodeCache.Add(_root);
                return _root;
            }

            TreeNode current = _root;
            TreeNode parent = _root;

            //Loop until the appropriate empty spot is found
            while (current != null)
            {
                if (decision.Compare(current) >= 0.7f)
                {
                    decision.Left = current.Left;
                    decision.Right = current.Right;
                    current = decision;
                    return current;
                }

                if (decision.Compare(current) >= 0.4f)
                {
                    parent = current;
                    current = current.Right;
                }
                else if (decision.Compare(current) < 0.4f)
                {
                    parent = current;
                    current = current.Left;
                }
            }

            //Make the decision a child of the parent based on how similar they are
            if (decision.Compare(parent) >= 0.7f)
            {
                parent.Right = decision;
                decision.Parent = parent;
            }
            else
            {
                parent.Left = decision;
                decision.Parent = parent;
            }

            _nodeCache.Add(decision);
            Debug.Log("Added decision to cache" + _nodeCache.Count);

            return decision;
        }

        public void RemoveDecision(TreeNode node)
        {
            if (node == null) return;
            //Initialize two iterators to find the data that will be copied and the node's parent
            TreeNode iter1 = node;
            TreeNode iter2 = node;

            ///If the node has a right child, find the node with the smallest value to the right of the node we want to remove
            if (node.Right != null)
            {
                //Sets the first iterator to the right of the node
                iter1 = node.Right;

                //Moves the first iterator to the left until it finds the smallest value
                while (iter1.Left != null)
                {
                    iter2 = iter1;
                    iter1 = iter1.Left;
                }

                //Once the smallest value has been found, copy the value to the node we want to remove
                node = node.CopyData(iter1);

                //Connect any children the smallest node may have to the parent of the smallest node.
                if (iter2.Left != null)
                {
                    if (iter2.Left.Compare(node) == 1)
                    {
                        iter2.Left = iter1.Right;
                    }
                }
                if (iter2.Right != null)
                {
                    if (iter2.Right.Compare(node) == 1)
                    {
                        iter2.Right = iter1.Right;
                    }
                }

                //Remove all connections to this node
                iter1.Parent = null;
                iter1.Right = null;
                iter1.Left = null;
            }
            else
            {
                TreeNode parent = node;

                if (node.Parent != null)
                    parent = node.Parent;

                //Connect any children the node that needs to be removed may have to its parent.
                if (parent.Left != null)
                {
                    if (parent.Compare(node) == 1)
                    {
                        parent.Left = node.Left;
                    }
                }
                if (parent.Right != null)
                {
                    if (parent.Compare(node) == 1)
                    {
                        parent.Right = node.Left;
                    }
                }
                //If the node we want to remove is the root, set the root to be its left child.
                if (_root.Compare(node) == 1)
                {
                    _root = node.Left;
                }
                //Delete the node we want to remove
                node.Parent = null;
                node.Right = null;
                node.Left = null;
                _nodeCache.Remove(node);
            }
        }

        public virtual void Save(string ownerName)
        {
            StreamWriter writer = new StreamWriter("Decisions/DecisionData" + ownerName + ".txt");
            string json = JsonConvert.SerializeObject(_nodeCache);
            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public virtual bool Load(string ownerName)
        {
            if (!File.Exists("Decisions/DecisionData" + ownerName + ".txt"))
                return false;

            StreamReader reader = new StreamReader("Decisions/DecisionData" + ownerName + ".txt");
            _nodeCache = JsonConvert.DeserializeObject<List<TreeNode>>(reader.ReadToEnd());
            reader.Close();

            for (int i = 0; i < _nodeCache.Count; i++)
                AddDecision(_nodeCache[i]);

            OnLoad?.Invoke();

            return true;
        }
    }
}
