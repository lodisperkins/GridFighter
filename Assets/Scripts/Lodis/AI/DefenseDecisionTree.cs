using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.AI
{
    [System.Serializable]
    public class DefenseDecisionTree : DecisionTree
    {
        public static string DecisionData;

        public DefenseDecisionTree(float compareThreshold = 0.95f) : base(compareThreshold)
        {
            SaveLoadPath = Application.persistentDataPath + "/DefenseDecisionData";
            _nodeCache = new List<TreeNode>();
            LoseThreshold = -2;
        }
    }
}
