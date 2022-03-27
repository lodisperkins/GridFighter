using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Lodis.AI
{
    [System.Serializable]
    public  class TreeNode
    {
        public int VisitCount;
        public int Wins;
        public float BaseWeight;
        [JsonIgnore]
        public TreeNode Left;
        [JsonIgnore]
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
            if (VisitCount == 0)
                return Mathf.Infinity;

            float averageWeight = Wins / VisitCount;

            BaseWeight = averageWeight + RandomDecisionConstant * (Mathf.Sqrt(Mathf.Log(root.VisitCount) / VisitCount));

            return BaseWeight;
        }

        public virtual float Compare(TreeNode node) { return 0; }

        public virtual void Save(StreamWriter writer) { }
        public virtual void Load(StreamReader reader) { }

    }
}
