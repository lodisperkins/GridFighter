using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.AI
{
    public class AttackDecisionTree : DecisionTree
    {
        private new List<AttackNode> _nodeCache;

        public AttackDecisionTree(float compareThreshold = 0.75f) : base(compareThreshold)
        {
            _nodeCache = new List<AttackNode>();
        }

        public override void Save()
        {
            StreamWriter writer = new StreamWriter("Decisions/AttackDecisionData.txt");
            string json = JsonUtility.ToJson(_nodeCache);
            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public override bool Load()
        {
            if (!File.Exists("Decisions/AttackDecisionData.txt"))
                return false;

            StreamReader reader = new StreamReader("Decisions/AttackDecisionData.txt");
            _nodeCache = JsonUtility.FromJson<List<AttackNode>>(reader.ReadToEnd());
            reader.Close();

            for (int i = 0; i < _nodeCache.Count; i++)
                AddDecision(_nodeCache[i]);

            OnLoad?.Invoke();

            return true;
        }
    }
}