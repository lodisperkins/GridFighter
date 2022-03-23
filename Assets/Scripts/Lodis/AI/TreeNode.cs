using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.AI
{
    [System.Serializable]
    public  class TreeNode
    {
        public int VisitCount;
        public int Wins;
        public float BaseWeight;
        public TreeNode Left;
        public TreeNode Right;
        public TreeNode Parent;
        public static int RandomDecisionConstant;

        public TreeNode(TreeNode left, TreeNode right)
        {
            Left = left;
            Right = right;
        }

        public virtual float GetTotalWeight(TreeNode root, params object[] args)
        {
            float averageWeight = BaseWeight / VisitCount;

            return averageWeight + RandomDecisionConstant * (Mathf.Sqrt(Mathf.Log(root.VisitCount) / VisitCount));
        }

        public virtual float Compare(TreeNode node) { return 0; }
    }
}
