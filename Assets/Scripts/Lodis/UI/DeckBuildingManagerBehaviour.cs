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
        private string _saveLoadPath;
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
            _deckFilePaths = DeckOptions;

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
            int index = Array.IndexOf(DeckOptions, deckName);
            string normalPath = _deckFilePaths[index];
            StreamReader reader = new StreamReader(normalPath);

            for (int i = 0; i < 9; i++)
            {
                string abilityName = reader.ReadLine();
                NormalDeck = Instantiate(Resources.Load<Deck>("Decks/Normals/P_" + deckName + "_Normals"));
                NormalDeck.DeckName = "Custom_Normals";
            }
            SpecialDeck = Instantiate(Resources.Load<Deck>("Decks/Specials/P_" + deckName + "_Specials"));
            SpecialDeck.DeckName = "Custom_Specials";
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