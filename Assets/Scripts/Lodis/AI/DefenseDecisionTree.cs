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
        public DefenseDecisionTree(float compareThreshold = 0.95f) : base(compareThreshold)
        {
            _nodeCache = new List<TreeNode>();
        }

        public override void Save(string ownerName)
        {
            if (_nodeCache.Count == 0) return;

            if (!File.Exists("Decisions/DefenseDecisionData" + ownerName + ".txt"))
            {
                FileStream stream = File.Create("Decisions/DefenseDecisionData" + ownerName + ".txt");
                stream.Close();
            }

            StreamWriter writer = new StreamWriter("Decisions/DefenseDecisionData" + ownerName + ".txt");
            string json = _nodeCache.Count.ToString() + "\n";

            for (int i = 0; i < _nodeCache.Count; i++)
                json += JsonConvert.SerializeObject((DefenseNode)_nodeCache[i]) + "\n";

            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public override bool Load(string ownerName)
        {
            if (!File.Exists("Decisions/DefenseDecisionData" + ownerName + ".txt"))
                return false;

            StreamReader reader = new StreamReader("Decisions/DefenseDecisionData" + ownerName + ".txt");
            int count = JsonConvert.DeserializeObject<int>(reader.ReadLine());

            for (int i = 0; i < count; i++)
            {
                _nodeCache.Add(JsonConvert.DeserializeObject<DefenseNode>(reader.ReadLine()));
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
