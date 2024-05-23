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
        private MeshReplacementBehaviour _customCharacter;
        private string _characterName;
        private static string _saveLoadPath;
        private int _currentArmorType;
        private string _replacementName;
        private bool _creatingNewCharacter;

        public int CurrentArmorType { get => _currentArmorType; set => _currentArmorType = value; }
        public string ReplacementName { get => _replacementName; set => _replacementName = value; }
        public string CharacterName { get => _characterName; set => _characterName = value; }

        public MeshReplacementBehaviour CustomCharacter 
        {
            get
            {
                if (!_customCharacter)
                    _customCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

                return _customCharacter;
            }
            set => _customCharacter = value;
        }
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

            if (Application.version != "0.2")
                Directory.Delete(_saveLoadPath);

            Directory.CreateDirectory(_saveLoadPath);
        }

        public void SetArmorListToDefault()
        {
            if (!CustomCharacter)
                CustomCharacter = GameObject.FindObjectOfType<MeshReplacementBehaviour>();

            CustomCharacter.SetOutfitToDefault();
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

            CustomCharacter.SetOutfitToDefault();
            CustomCharacter.LoadOutfit(reader);

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

        public static void LoadCustomCharacter(string characterName, MeshReplacementBehaviour meshReplacer, out Color hairColor, out Color faceColor)
        {
            string armorPath = _saveLoadPath + "/" + characterName + "_ArmorSet.txt";

            StreamReader reader = new StreamReader(armorPath);

            meshReplacer.LoadOutfit(reader);

            Vector4 hairValue = JsonConvert.DeserializeObject<Vector4>(reader.ReadLine());
            Vector4 faceValue = JsonConvert.DeserializeObject<Vector4>(reader.ReadLine());

            hairColor = hairValue.ToColor();
            faceColor = faceValue.ToColor();

            reader.Close();
        }

        public void ReplaceWearable(string ID)
        {
            if (CurrentArmorType < 0)
                Debug.LogError("Invalid type for armor replacement.");

            CustomCharacter.ReplaceWearable(ID);
        }

        public void ReplaceFaceColor(Color color)
        {
            CustomCharacter.FaceColor = color;
        }

        public void ReplaceHairColor(Color color)
        {
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
            if (CustomCharacter.HasDefaultOutfit == true)
                return;

            string path = _creatingNewCharacter ? CreateUniqueArmorPath() : ArmorPath;

            StreamWriter writer = new StreamWriter(path);

            CustomCharacter.SaveOutfit(writer);

            string colorJson = JsonConvert.SerializeObject((Vector4)CustomCharacter.HairColor);

            writer.WriteLine(colorJson);

            colorJson = JsonConvert.SerializeObject((Vector4)CustomCharacter.FaceColor);

            writer.WriteLine(colorJson);

            writer.Close();
        }
    }
}