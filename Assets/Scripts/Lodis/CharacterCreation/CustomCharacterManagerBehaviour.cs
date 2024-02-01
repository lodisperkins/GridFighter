using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Lodis.Utility;

namespace Lodis.CharacterCreation
{
    public class CustomCharacterManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private List<ArmorData> _defaultSetsReference;
        private static List<ArmorData> _defaultSets;
        private List<ArmorData> _characterArmorPieces;
        private MeshReplacementBehaviour _customCharacter;
        private List<ArmorData> _replacementArmorData;
        private string _characterName;
        private static string _saveLoadPath;
        private int _currentArmorType;
        private string _replacementName;
        private bool _creatingNewCharacter;

        public List<ArmorData> CharacterArmorPieces { get => _characterArmorPieces; private set => _characterArmorPieces = value; }
        public int CurrentArmorType { get => _currentArmorType; set => _currentArmorType = value; }
        public string ReplacementName { get => _replacementName; set => _replacementName = value; }
        public string CharacterName { get => _characterName; set => _characterName = value; }
        public List<ArmorData> ReplacementArmorData { get => _replacementArmorData; set => _replacementArmorData = value; }
        public MeshReplacementBehaviour CustomCharacter { get => _customCharacter; set => _customCharacter = value; }
        public string ArmorPath
        {
            get
            {
                return _saveLoadPath + "/" + CharacterName + "_ArmorSet.txt";
            }
        }


        // Start is called before the first frame update
        void Awake()
        {
            _saveLoadPath = Application.persistentDataPath + "/CustomCharacters";

            Directory.CreateDirectory(_saveLoadPath);

            _defaultSets = _defaultSetsReference;

            _characterArmorPieces = new List<ArmorData>();
            ReplacementArmorData = new List<ArmorData>();
            LoadReplacementArmor();
        }

        public void SetArmorListToDefault()
        {
            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            CustomCharacter.ReplaceMeshes(_defaultSetsReference);
        }

        public void LoadReplacementArmor()
        {
            ArmorData[] armors = Resources.LoadAll<ArmorData>("ArmorData");

            ReplacementArmorData.AddRange(armors);
        }

        public void SetCreatingNewCharacter(bool value)
        {
            _creatingNewCharacter = value;
        }

        public void SetCharacterName(string name)
        {
            if (name == "")
                name = "Gladiator";

            _characterName = name;
        }

        public void SetCharacterName(Text text)
        {
            string newName = text.text;

            if (newName == "")
                newName = "Gladiator";

            string path = ArmorPath;

            if (File.Exists(path))
                File.Move(path, _saveLoadPath + "/" + newName + "_ArmorSet.txt");

            CharacterName = text.text;
        }

        public void LoadCustomCharacter(string characterName)
        {
            CharacterName = characterName;
            string armorPath = ArmorPath;

            StreamReader reader = new StreamReader(armorPath);

            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            List<ArmorData> replacements = new List<ArmorData>();
            string armorName = reader.ReadLine();

            while (armorName != "EndArmor")
            {
                replacements.Add(Resources.Load<ArmorData>("ArmorData/" + armorName));
                armorName = reader.ReadLine();
            }

            for (int i = 0; i < _defaultSetsReference.Count && replacements.Count != _defaultSetsReference.Count; i++)
            {
                if (!replacements.Find(set => set.BodySection == _defaultSetsReference[i].BodySection))
                    replacements.Add(_defaultSetsReference[i]);
            }

            CustomCharacter.ReplaceMeshes(replacements);
            Vector4 hairValue = JsonConvert.DeserializeObject<Vector4>(reader.ReadLine());
            Vector4 faceValue = JsonConvert.DeserializeObject<Vector4>(reader.ReadLine());

            CustomCharacter.HairColor = hairValue.ToColor();
            CustomCharacter.FaceColor = faceValue.ToColor();

            reader.Close();
        }

        public void DeleteCustomCharacter()
        {
            string armorPath = ArmorPath;

            File.Delete(armorPath);

            CustomCharacter = null;
        }

        public static void LoadCustomCharacter(string characterName, out List<ArmorData> replacements, out Color hairColor, out Color faceColor)
        {

            string armorPath = _saveLoadPath + "/" + characterName + "_ArmorSet.txt";

            StreamReader reader = new StreamReader(armorPath);
            replacements = new List<ArmorData>();
            string armorName = reader.ReadLine();

            while (armorName != "EndArmor")
            {
                replacements.Add(Resources.Load<ArmorData>("ArmorData/" + armorName));
                armorName = reader.ReadLine();
            }

            for (int i = 0; i < _defaultSets.Count && replacements.Count != _defaultSets.Count; i++)
            {
                if (!replacements.Find(set => set.BodySection == _defaultSets[i].BodySection))
                    replacements.Add(_defaultSets[i]);
            }

            Vector4 hairValue = JsonConvert.DeserializeObject<Vector4>(reader.ReadLine());
            Vector4 faceValue = JsonConvert.DeserializeObject<Vector4>(reader.ReadLine());

            hairColor = hairValue.ToColor();
            faceColor = faceValue.ToColor();

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

        public void ReplaceFaceColor(Color color)
        {
            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            CustomCharacter.FaceColor = color;
        }

        public void ReplaceHairColor(Color color)
        {
            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            CustomCharacter.HairColor = color;
        }

        private string CreateUniqueArmorPath()
        {
            string uniquePath = ArmorPath;
            int num = 0;

            while (File.Exists(uniquePath))
            {
                num++;
                uniquePath = _saveLoadPath + "/" + CharacterName + " " + num.ToString() + "_ArmorSet.txt";
            }

            FileStream stream = File.Create(uniquePath);
            stream.Close();

            return uniquePath;
        }

        public void SaveCharacter()
        {
            if (CustomCharacter?.ArmorReplacements == null)
                return;

            string path = _creatingNewCharacter ? CreateUniqueArmorPath() : ArmorPath;

            StreamWriter writer = new StreamWriter(path);

            foreach (ArmorData data in CustomCharacter.ArmorReplacements)
                writer.WriteLine(data.name);

            writer.WriteLine("EndArmor");

            string colorJson = JsonConvert.SerializeObject((Vector4)CustomCharacter.HairColor);

            writer.WriteLine(colorJson);

            colorJson = JsonConvert.SerializeObject((Vector4)CustomCharacter.FaceColor);

            writer.WriteLine(colorJson);

            writer.Close();
        }
    }
}