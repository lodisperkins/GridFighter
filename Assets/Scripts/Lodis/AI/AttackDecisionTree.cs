using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Lodis.AI
{
    [System.Serializable]
    public class AttackDecisionTree : DecisionTree
    {
        public static string DecisionData;

        public AttackDecisionTree(float compareThreshold = 0.3f) : base(compareThreshold)
        {
            SaveLoadPath = Application.persistentDataPath + "/AttackDecisionData";
            _nodeCache = new List<TreeNode>();
            LoseThreshold = -1;
        }

    }
}