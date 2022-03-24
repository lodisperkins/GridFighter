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
            if (_nodeCache.Count == 0) return;

            if (!File.Exists("Decisions/AttackDecisionData.txt"))
            {
                FileStream stream = File.Create("Decisions/AttackDecisionData.txt");
                stream.Close();
            }

            StreamWriter writer = new StreamWriter("Decisions/AttackDecisionData.txt");
            string json = _nodeCache.Count.ToString() + "\n";

            for (int i = 0; i < _nodeCache.Count; i++)
                json += JsonConvert.SerializeObject((AttackNode)_nodeCache[i]) + "\n";

            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public override bool Load()
        {
            if (!File.Exists("Decisions/AttackDecisionData.txt"))
                return false;

            StreamReader reader = new StreamReader("Decisions/AttackDecisionData.txt");
            int count = JsonConvert.DeserializeObject<int>(reader.ReadLine());

            for (int i = 0; i < count; i++)
            {
                _nodeCache.Add(JsonConvert.DeserializeObject<AttackNode>(reader.ReadLine()));
            }

            reader.Close();
            int loadCount = _nodeCache.Count;

            if (_nodeCache == null)
            {
                _nodeCache = new List<TreeNode>();
                return false;
            }

            for (int i = 0; i < loadCount; i++)
                AddDecision(_nodeCache[i]);

            OnLoad?.Invoke();

            return true;
        }
    }
}