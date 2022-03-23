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
        public AttackDecisionTree(float compareThreshold = 0.95f) : base(compareThreshold)
        {
            _nodeCache = new List<TreeNode>();
        }

        public override void Save()
        {
            if (!File.Exists("Decisions/AttackDecisionData.txt"))
                File.Create("Decisions/AttackDecisionData.txt");

            StreamWriter writer = new StreamWriter("Decisions/AttackDecisionData.txt");
            string json = JsonConvert.SerializeObject(_nodeCache);
            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public override bool Load()
        {
            if (!File.Exists("Decisions/AttackDecisionData.txt"))
                return false;

            StreamReader reader = new StreamReader("Decisions/AttackDecisionData.txt");
            _nodeCache = JsonConvert.DeserializeObject<List<TreeNode>>(reader.ReadToEnd());
            reader.Close();

            if (_nodeCache == null)
            {
                _nodeCache = new List<TreeNode>();
                return false;
            }

            for (int i = 0; i < _nodeCache.Count; i++)
                AddDecision(_nodeCache[i]);

            OnLoad?.Invoke();

            return true;
        }
    }
}