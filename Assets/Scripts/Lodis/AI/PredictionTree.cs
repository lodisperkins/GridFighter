using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.AI
{
    [System.Serializable]
    public class PredictionDecisionTree : DecisionTree
    {
        public static string DecisionData;

        public PredictionDecisionTree(float compareThreshold = 0.95f) : base(compareThreshold)
        {
            SaveLoadPath = Application.persistentDataPath + "/PredictionDecisionData";
            _nodeCache = new List<TreeNode>();
            LoseThreshold = -5;
        }

        public override void Save(string ownerName)
        {
            base.Save("");
        }

        public override bool Load(string ownerName)
        {
            return base.Load("");
        }
    }
}
