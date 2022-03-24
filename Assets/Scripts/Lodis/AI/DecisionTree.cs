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
        [SerializeField]
        private TreeNode _root;
        [SerializeField]
        private float _compareThreshold;
        public SaveLoadEvent OnSave;
        public SaveLoadEvent OnLoad;

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
            if (_nodeCache.Count >= 500) return null;

            if (_root == null)
            {
                _root = decision;
                _nodeCache.Add(_root);
                return _root;
            }

            TreeNode current = _root;
            TreeNode parent = _root;

            //Loop until the appropriate empty spot is found
            while (current != null)
            {
                if (decision.Compare(current) >= 0.6f)
                {
                    decision.Left = current.Left;
                    decision.Right = current.Right;
                    current = decision;
                    return current;
                }

                if (decision.Compare(current) >= 0.8f)
                {
                    parent = current;
                    current = current.Right;
                }
                else if (decision.Compare(current) < 0.8f)
                {
                    parent = current;
                    current = current.Left;
                }
            }

            //Make the decision a child of the parent based on how similar they are
            if (decision.Compare(parent) >= 0.9f)
                parent.Right = decision;
            else
                parent.Left = decision;

            _nodeCache.Add(decision);

            return decision;
        }

        public virtual void Save()
        {
            StreamWriter writer = new StreamWriter("Decisions/DecisionData.txt");
            string json = JsonConvert.SerializeObject(_nodeCache);
            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public virtual bool Load()
        {
            if (!File.Exists("Decisions/DecisionData.txt"))
                return false;

            StreamReader reader = new StreamReader("Decisions/DecisionData.txt");
            _nodeCache = JsonConvert.DeserializeObject<List<TreeNode>>(reader.ReadToEnd());
            reader.Close();

            for (int i = 0; i < _nodeCache.Count; i++)
                AddDecision(_nodeCache[i]);

            OnLoad?.Invoke();

            return true;
        }
    }
}
