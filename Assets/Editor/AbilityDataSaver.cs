using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Lodis.ScriptableObjects;
using Lodis.Gameplay;
using Types;

public class AbilityDataSaver : EditorWindow
{
    // Path where the data will be saved
    private string _saveFilePath = "Assets/AbilityDataExport.json";

    [MenuItem("Tools/Save/Load Ability Data")]
    public static void ShowWindow()
    {
        GetWindow<AbilityDataSaver>("Save/Load Ability Data");
    }

    void OnGUI()
    {
        GUILayout.Label("Save or Load Ability Data", EditorStyles.boldLabel);

        _saveFilePath = EditorGUILayout.TextField("Save/Load File Path:", _saveFilePath);

        if (GUILayout.Button("Save All Abilities to File"))
        {
            SaveAllAbilities();
        }

        if (GUILayout.Button("Load All Abilities from File (Fixed32 Values Only)"))
        {
            LoadAllAbilities();
        }
    }

    // This function will collect all AbilityData assets and save them to a JSON file.
    private void SaveAllAbilities()
    {
        // Load all AbilityData objects from the Resources folder
        AbilityData[] allAbilities = Resources.LoadAll<AbilityData>("");

        if (allAbilities.Length == 0)
        {
            Debug.LogError("No AbilityData found in the Resources folder.");
            return;
        }

        // Create a list to hold all serialized ability data
        List<SerializedAbilityData> serializedAbilities = new List<SerializedAbilityData>();

        // Iterate through each ability and collect its data
        foreach (var ability in allAbilities)
        {
            SerializedAbilityData serializedAbility = new SerializedAbilityData(ability);
            serializedAbilities.Add(serializedAbility);
        }

        // Convert the list of serialized abilities to JSON format
        string json = JsonUtility.ToJson(new AbilityDataCollection { Abilities = serializedAbilities }, true);

        // Save the JSON string to a file
        File.WriteAllText(_saveFilePath, json);

        // Refresh the Unity Asset Database to reflect changes
        AssetDatabase.Refresh();

        Debug.Log($"Successfully saved {allAbilities.Length} abilities to {_saveFilePath}");
    }

    private void LoadAllAbilities()
    {
        if (!File.Exists(_saveFilePath))
        {
            Debug.LogError($"No file found at {_saveFilePath}");
            return;
        }

        string json = File.ReadAllText(_saveFilePath);
        AbilityDataCollection abilityCollection = JsonUtility.FromJson<AbilityDataCollection>(json);

        AbilityData[] allAbilities = Resources.LoadAll<AbilityData>("");

        if (allAbilities.Length == 0)
        {
            Debug.LogError("No AbilityData found in the Resources folder.");
            return;
        }

        Dictionary<string, AbilityData> abilityDict = new Dictionary<string, AbilityData>();
        foreach (var ability in allAbilities)
        {
            abilityDict[ability.abilityName] = ability;
        }

        foreach (var serializedAbility in abilityCollection.Abilities)
        {
            if (abilityDict.TryGetValue(serializedAbility.abilityName, out AbilityData ability))
            {
                // Update fields in the correct order
                ability.abilityName = serializedAbility.abilityName;
                ability.abilityDescription = serializedAbility.abilityDescription;
                ability.AbilityType = (AbilityType)System.Enum.Parse(typeof(AbilityType), serializedAbility.abilityType);
                ability.maxActivationAmount = serializedAbility.maxActivationAmount;
                ability.EnergyCost = (int)serializedAbility.energyCost;

                // Fixed32 values
                ability.startUpTime = (Fixed32)serializedAbility.startUpTime;
                ability.timeActive = (Fixed32)serializedAbility.timeActive;
                ability.recoverTime = (Fixed32)serializedAbility.recoverTime;

                // Update custom stats
                if (serializedAbility.stats != null && serializedAbility.stats.Length == ability.CustomStats.Length)
                {
                    for (int i = 0; i < serializedAbility.stats.Length; i++)
                    {
                        ability.CustomStats[i].name = serializedAbility.stats[i].name;
                        ability.CustomStats[i].value = (Fixed32)serializedAbility.stats[i].value;
                    }
                }

                // Update collider data
                if (serializedAbility.collisionInfo != null && serializedAbility.collisionInfo.Length == ability.ColliderData.Length)
                {
                    for (int i = 0; i < serializedAbility.collisionInfo.Length; i++)
                    {
                        var colliderData = serializedAbility.collisionInfo[i];
                        ability.ColliderData[i].TimeActive = (Fixed32)colliderData.TimeActive;
                        ability.ColliderData[i].MultiHitWaitTime = (Fixed32)colliderData.MultiHitWaitTime;
                        ability.ColliderData[i].Damage = (Fixed32)colliderData.Damage;
                        ability.ColliderData[i].BaseKnockBack = (Fixed32)colliderData.BaseKnockBack;
                        ability.ColliderData[i].KnockBackScale = (Fixed32)colliderData.KnockBackScale;
                        ability.ColliderData[i].HitAngle = (Fixed32)colliderData.HitAngle;
                        ability.ColliderData[i].HitStunTime = (Fixed32)colliderData.HitStunTime;
                        ability.ColliderData[i].Priority = (Fixed32)colliderData.Priority;
                    }
                }

                // Effects and sounds (ignored, as we don't handle GameObject serialization here)
                // Update them if necessary, but be aware that GameObjects and sounds will need to be loaded separately

                Debug.Log($"Updated {ability.abilityName} with data from the saved file.");
                EditorUtility.SetDirty(ability);
            }
            else
            {
                Debug.LogWarning($"Ability {serializedAbility.abilityName} not found in the Resources folder.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }



    // Class to hold all abilities in a single collection for JSON serialization
    [System.Serializable]
    public class AbilityDataCollection
    {
        public List<SerializedAbilityData> Abilities;
    }

    // Helper class to serialize AbilityData in a simplified form
    [System.Serializable]
    public class SerializedAbilityData
    {
        public string abilityName;
        public string abilityDescription;
        public string abilityType;
        public float timeActive;
        public float recoverTime;
        public float startUpTime;
        public int maxActivationAmount;
        public float energyCost;
        public string[] effects;
        public string[] sounds;
        public string icon;
        public AbilityData.Stat[] stats;
        public HitColliderData[] collisionInfo;

        public SerializedAbilityData(AbilityData ability)
        {
            abilityName = ability.abilityName;
            abilityDescription = ability.abilityDescription;
            abilityType = ability.AbilityType.ToString();
            timeActive = ability.timeActive;
            recoverTime = ability.recoverTime;
            startUpTime = ability.startUpTime;
            maxActivationAmount = ability.maxActivationAmount;
            energyCost = ability.EnergyCost;
            stats = ability.CustomStats;
            collisionInfo = ability.ColliderData;

            // Convert effects and sounds into string arrays
            effects = ConvertGameObjectArrayToNames(ability.Effects);
            sounds = ConvertAudioClipArrayToNames(ability.Sounds);
            icon = ability.DisplayIcon != null ? ability.DisplayIcon.name : "None";
        }

        private string[] ConvertGameObjectArrayToNames(GameObject[] objects)
        {
            if (objects == null) return new string[0];
            string[] names = new string[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                names[i] = objects[i] != null ? objects[i].name : "None";
            }
            return names;
        }

        private string[] ConvertAudioClipArrayToNames(AudioClip[] clips)
        {
            if (clips == null) return new string[0];
            string[] names = new string[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                names[i] = clips[i] != null ? clips[i].name : "None";
            }
            return names;
        }
    }
}
