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

        public AttackDecisionTree(float compareThreshold = 0.8f) : base(compareThreshold)
        {
            SaveLoadPath = Application.persistentDataPath + "/AttackDecisionData";
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
            string json =  JsonConvert.SerializeObject(_nodeCache, _settings);

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

            //int count = JsonConvert.DeserializeObject<int>(reader.ReadLine(), new JsonSerializerSettings
            //{
            //    Error = (sender, args) =>
            //    {
            //        Debug.LogError(args.ErrorContext.Error.Message);
            //        args.ErrorContext.Handled = true;
            //    }
            //});

            //for (int i = 0; i < count; i++)
            //{
            //    AddDecision(JsonConvert.DeserializeObject<AttackNode>(reader.ReadLine()));
            //}

            _nodeCache = JsonConvert.DeserializeObject<List<TreeNode>>(reader.ReadToEnd(), _settings);

            Debug.Log("Loaded " + _nodeCache.Count + " attack decisions.");
            reader.Close();

            if (_nodeCache.Count == 0)
                return false;

            OnLoad?.Invoke();

            return true;
        }
    }
}