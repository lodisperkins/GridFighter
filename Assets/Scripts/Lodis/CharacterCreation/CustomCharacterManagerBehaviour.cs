using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.CharacterCreation
{
    public class CustomCharacterManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private List<ArmorData> _defaultSets;
        private List<ArmorData> _characterArmorPieces;
        private MeshReplacementBehaviour _customCharacter;
        private List<ArmorData> _replacementArmorData;
        private string _characterName;
        private static string _saveLoadPath;
        private int _currentArmorType;
        private string _replacementName;

        public List<ArmorData> CharacterArmorPieces { get => _characterArmorPieces; private set => _characterArmorPieces = value; }
        public int CurrentArmorType { get => _currentArmorType; set => _currentArmorType = value; }
        public string ReplacementName { get => _replacementName; set => _replacementName = value; }
        public string CharacterName { get => _characterName; set => _characterName = value; }
        public List<ArmorData> ReplacementArmorData { get => _replacementArmorData; set => _replacementArmorData = value; }
        public MeshReplacementBehaviour CustomCharacter { get => _customCharacter; set => _customCharacter = value; }

        // Start is called before the first frame update
        void Awake()
        {
            _saveLoadPath = Application.persistentDataPath + "/CustomCharacters";
            Directory.CreateDirectory(_saveLoadPath);

            _characterArmorPieces = new List<ArmorData>();
            ReplacementArmorData = new List<ArmorData>();
            LoadReplacementArmor();
        }

        public void SetArmorListToDefault()
        {
            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            CustomCharacter.ReplaceMeshes(_defaultSets);
        }

        public void LoadReplacementArmor()
        {
            ArmorData[] armors = Resources.LoadAll<ArmorData>("ArmorData");

            ReplacementArmorData.AddRange(armors);
        }

        public void SetCharacterName(string name)
        {
            _characterName = name;
        }

        public void SetCharacterName(Text text)
        {
            string newName = text.text;
            string path = _saveLoadPath + "/" + CharacterName + "_ArmorSet.txt";

            if (File.Exists(path))
                File.Move(path, _saveLoadPath + "/" + newName + "_ArmorSet.txt");

            CharacterName = text.text;
        }

        public void LoadCustomCharacter(string characterName)
        {
            CharacterName = characterName;
            string armorPath = _saveLoadPath + "/" + CharacterName + "_ArmorSet.txt";

            StreamReader reader = new StreamReader(armorPath);

            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            List<ArmorData> replacements = new List<ArmorData>();

            while (!reader.EndOfStream)
            {
                string armorName = reader.ReadLine();
                replacements.Add(Resources.Load<ArmorData>("ArmorData/" + armorName));
            }

            for (int i = 0; i < _defaultSets.Count && replacements.Count != _defaultSets.Count; i++)
            {
                if (!replacements.Find(set => set.BodySection == _defaultSets[i].BodySection))
                    replacements.Add(_defaultSets[i]);
            }

            CustomCharacter.ReplaceMeshes(replacements);

            reader.Close();
        }

        public void ReplaceArmorPiece(ArmorData data)
        {
            if (CurrentArmorType < 0)
                Debug.LogError("Invalid type for armor replacement.");

            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            CustomCharacter.ReplaceMesh(data);
        }

        public void SaveCharacter()
        {
            if (CustomCharacter.ArmorReplacements == null)
                return;

            string path = _saveLoadPath + "/" + CharacterName + "_ArmorSet.txt";
            if (!File.Exists(path))
            {
                FileStream stream = File.Create(path);
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(path);

            foreach (ArmorData data in CustomCharacter.ArmorReplacements)
                writer.WriteLine(data.name);

            writer.Close();
        }
    }
}