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

        public override void Save(string ownerName)
        {
            if (_nodeCache.Count == 0) return;

            if (!File.Exists(SaveLoadPath + ownerName + ".txt"))
            {
                FileStream stream = File.Create(SaveLoadPath + ownerName + ".txt");
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(SaveLoadPath + ownerName + ".txt");
            string json = JsonConvert.SerializeObject(_nodeCache, _settings);

            writer.Write(json);
            writer.Close();

            OnSave?.Invoke();
        }

        public override bool Load(string ownerName)
        {
            if (!File.Exists(SaveLoadPath + ownerName + ".txt"))
                return false;

            _nodeCache = new List<TreeNode>();
            
            StreamReader reader = new StreamReader(SaveLoadPath + ownerName + ".txt");


            _nodeCache = JsonConvert.DeserializeObject<List<TreeNode>>(reader.ReadToEnd(), _settings);

            Debug.Log("Loaded " + _nodeCache.Count + " defense decisions.");
            reader.Close();

            if (_nodeCache.Count == 0)
            {
                return false;
            }
            OnLoad?.Invoke();

            return true;
        }
    }
}
