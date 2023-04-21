using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System;

namespace Lodis.UI
{
    public class DeckBuildingManagerBehaviour : MonoBehaviour
    {
        private string[] _deckOptions;
        private Deck _normalDeck;
        private Deck _specialDeck;
        private Deck[] _replacementNormalDecks;
        private Deck[] _replacementSpecialDecks;
        private Deck _replacementAbilities;
        private string _deckName;
        private static string _saveLoadPath;
        private JsonSerializerSettings _settings;
        private int _currentAbilityType;
        private int _specialReplacementIndex;
        private string _replacementName;

        public string[] DeckOptions { get => _deckOptions; private set => _deckOptions = value; }

        private string[] _deckFilePaths;

        public Deck SpecialDeck { get => _specialDeck; private set => _specialDeck = value; }
        public Deck NormalDeck { get => _normalDeck; private set => _normalDeck = value; }
        public Deck ReplacementAbilities { get => _replacementAbilities; private set => _replacementAbilities = value; }
        public int CurrentAbilityType { get => _currentAbilityType; set => _currentAbilityType = value; }
        public string ReplacementName { get => _replacementName; set => _replacementName = value; }

        private void Awake()
        {
            _saveLoadPath = Application.persistentDataPath + "/CustomDecks";
            Directory.CreateDirectory(_saveLoadPath);

            _settings = new JsonSerializerSettings();

            ReplacementAbilities = Deck.CreateInstance<Deck>();
            LoadDeckNames();
            LoadReplacementDecks();
        }

        public void SetSpecialReplacementIndex(int index)
        {
            _specialReplacementIndex = index;
        }

        public void LoadDeckNames()
        {
            string[] files = Directory.GetFiles(_saveLoadPath);

            if (files.Length == 0)
                return;

            DeckOptions = new string[files.Length];
            _deckFilePaths = files;

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileName(files[i]);
                string deckName = files[i].Split('_')[0];

                string[] duplicates = Array.FindAll<string>(DeckOptions, word => string.Compare(word, deckName) == 0);

                if (duplicates.Length == 0)
                    DeckOptions[i] = deckName;
            }
        }

        private void LoadReplacementDecks()
        {
            List<AbilityData> data = null;
            _replacementNormalDecks = Resources.LoadAll<Deck>("Decks/Normals");
            _replacementSpecialDecks = Resources.LoadAll<Deck>("Decks/Specials");

            for (int i = 0; i < _replacementNormalDecks.Length; i++)
            {
                ReplacementAbilities.AbilityData.AddRange(_replacementNormalDecks[i].AbilityData);
                ReplacementAbilities.AbilityData.AddRange(_replacementSpecialDecks[i].AbilityData);
            }
        }

        public Deck GetNormalReplacementDeck(int index)
        {
            return _replacementNormalDecks[index];
        }

        public Deck GetSpecialReplacementDeck(int index)
        {
            return _replacementSpecialDecks[index];
        }

        public void LoadPresetDeck(string deckName)
        {
            NormalDeck = Instantiate(Resources.Load<Deck>("Decks/Normals/P_" + deckName + "_Normals"));
            NormalDeck.DeckName = "Custom_Normals";
            SpecialDeck = Instantiate(Resources.Load<Deck>("Decks/Specials/P_" + deckName + "_Specials"));
            SpecialDeck.DeckName = "Custom_Specials";
        }

        public void LoadCustomDeck(string deckName)
        {

            string normalPath = _saveLoadPath + "/" + deckName + "_Normals.txt";
            string specialPath = _saveLoadPath + "/" + deckName + "_Specials.txt";

            StreamReader reader = new StreamReader(normalPath);

            NormalDeck = Deck.CreateInstance<Deck>();
            for (int i = 0; i < 9; i++)
            {
                string abilityName = reader.ReadLine();
                NormalDeck.AbilityData.Add(Instantiate(Resources.Load<AbilityData>("AbilityData/" + abilityName)));
            }
            NormalDeck.DeckName = "Custom_Normals";

            reader.Close();

            reader = new StreamReader(specialPath);
            SpecialDeck = Deck.CreateInstance<Deck>();

            for (int i = 0; i < 8; i++)
            {
                string abilityName = reader.ReadLine();
                SpecialDeck.AbilityData.Add(Instantiate(Resources.Load<AbilityData>("AbilityData/" + abilityName)));
            }

            reader.Close();
            SpecialDeck.DeckName = "Custom_Specials";
        }
        
        public static Deck LoadCustomNormalDeck(string deckName)
        {
            _saveLoadPath = Application.persistentDataPath + "/CustomDecks";
            Directory.CreateDirectory(_saveLoadPath);
            string normalPath = _saveLoadPath + "/" + deckName + "_Normals.txt";

            StreamReader reader = new StreamReader(normalPath);

            Deck normalDeck = Deck.CreateInstance<Deck>();
            for (int i = 0; i < 9; i++)
            {
                string abilityName = reader.ReadLine();
                normalDeck.AbilityData.Add(Instantiate(Resources.Load<AbilityData>("AbilityData/" + abilityName)));
            }
            normalDeck.DeckName = "Custom_Normals";

            reader.Close();

            return normalDeck;
        }

        public static Deck LoadCustomSpecialDeck(string deckName)
        {
            _saveLoadPath = Application.persistentDataPath + "/CustomDecks";
            Directory.CreateDirectory(_saveLoadPath);
            string specialPath = _saveLoadPath + "/" + deckName + "_Specials.txt";

            StreamReader reader = new StreamReader(specialPath);

            Deck specialDeck = Deck.CreateInstance<Deck>();
            for (int i = 0; i < 9; i++)
            {
                string abilityName = reader.ReadLine();
                specialDeck.AbilityData.Add(Instantiate(Resources.Load<AbilityData>("AbilityData/" + abilityName)));
            }
            specialDeck.DeckName = "Custom_Specials";

            reader.Close();

            return specialDeck;
        }

        public void ReplaceAbility()
        {
            if (CurrentAbilityType < 0)
                Debug.LogError("Invalid type for ability replacement.");

            if (CurrentAbilityType <= 3 || CurrentAbilityType == 9)
                ReplaceNormalAbility(ReplacementName);
            else
                ReplaceSpecialAbility(ReplacementName);
        }

        public void ReplaceAbility(string name)
        {
            ReplacementName = name;
            if (CurrentAbilityType < 0)
                Debug.LogError("Invalid type for ability replacement.");

            if (CurrentAbilityType <= 3 || CurrentAbilityType == 9)
                ReplaceNormalAbility(ReplacementName);
            else
                ReplaceSpecialAbility(ReplacementName);
        }

        private void ReplaceNormalAbility(string name)
        {
            AbilityData data = ReplacementAbilities.GetAbilityDataByName(name);
            _normalDeck.SetAbilityDataByType((AbilityType)CurrentAbilityType, data);

            if (CurrentAbilityType < (int)AbilityType.UNBLOCKABLE)
            {
                string abilityName = data.name;
                abilityName.Remove(0);
                abilityName.Insert(0, "S");
                AbilityData strongData = Resources.Load<AbilityData>("AbilityData/" + abilityName);
                _normalDeck.SetAbilityDataByType((AbilityType)(CurrentAbilityType + 4), strongData); 
            }
        }

        private void ReplaceSpecialAbility(string name)
        {
            AbilityData data = ReplacementAbilities.GetAbilityDataByName(name);
            _specialDeck.AbilityData[_specialReplacementIndex] = data;
        }

        public void SaveDecks()
        {
            SaveDeck(NormalDeck);
            SaveDeck(SpecialDeck);
        }

        private void SaveDeck(Deck deck)
        {
            if (deck == null)
                return;
            
            if (!File.Exists(_saveLoadPath + "/" + deck.DeckName + ".txt"))
            {
                FileStream stream = File.Create(_saveLoadPath + "/" + deck.DeckName + ".txt");
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(_saveLoadPath + "/" + deck.DeckName + ".txt");

            foreach (AbilityData data in deck.AbilityData)
                writer.WriteLine(data.name);

            writer.Close();
        }
    }
}