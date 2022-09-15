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
            string json = _nodeCache.Count.ToString() + "\n";

            for (int i = 0; i < _nodeCache.Count; i++)
                json += JsonConvert.SerializeObject((DefenseNode)_nodeCache[i]) + "\n";

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

            int count = JsonConvert.DeserializeObject<int>(reader.ReadLine(), new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            });

            for (int i = 0; i < count; i++)
            {
                AddDecision(JsonConvert.DeserializeObject<DefenseNode>(reader.ReadLine()));
            }

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
