using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Lodis.ScriptableObjects;

namespace Lodis.UI
{
    public class DeckBuildingManagerBehaviour : MonoBehaviour
    {
        private string[] _deckOptions;
        private Deck _normalDeck;
        private Deck _specialDeck;
        private Deck[] _replacementNormalDecks;
        private Deck[] _replacementSpecialDecks;
        private string _deckName;
        private string _saveLoadPath;
        private JsonSerializerSettings _settings;
        private int _currentNormalType;
        private int _replacementDeckIndex;

        public string[] DeckOptions { get => _deckOptions; private set => _deckOptions = value; }
        public Deck SpecialDeck { get => _specialDeck; private set => _specialDeck = value; }
        public Deck NormalDeck { get => _normalDeck; private set => _normalDeck = value; }

        private void Awake()
        {
            _saveLoadPath = Application.persistentDataPath + "/CustomDecks";
            Directory.CreateDirectory(_saveLoadPath);

            _settings = new JsonSerializerSettings();
            _settings.TypeNameHandling = TypeNameHandling.All;

            LoadDeckNames();
            LoadReplacementDecks();
        }

        public void LoadDeckNames()
        {
            string[] files = Directory.GetFiles("Assets/Resources/Decks", "*.txt");

            if (files.Length == 0)
                return;

            DeckOptions = new string[files.Length];    

            for (int i = 0; i < files.Length; i++)
            {
                DeckOptions[i] = files[i].Split('.')[0];
            }
        }

        private void LoadReplacementDecks()
        {
            _replacementNormalDecks = Resources.LoadAll<Deck>("Decks/Normals");
            _replacementSpecialDecks = Resources.LoadAll<Deck>("Decks/Specials");
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
            SpecialDeck = Instantiate(Resources.Load<Deck>("Decks/Specials/P_" + deckName + "_Specials"));
        }

        public void ReplaceNormalAbility(int abilityType)
        {
            AbilityData data = _replacementNormalDecks[_replacementDeckIndex].GetAbilityDataByType((AbilityType)abilityType);
            _normalDeck.SetAbilityDataByType((AbilityType)abilityType, data);

        }

        public void ReplaceSpecialAbility(int index)
        {
            AbilityData data = _replacementSpecialDecks[_replacementDeckIndex].AbilityData[index];
            _specialDeck.AbilityData[_replacementDeckIndex] = data;

        }


        public void SaveDeck(Deck deck)
        {
            if (deck == null)
                return;
            
            if (!File.Exists(_saveLoadPath + deck.DeckName + ".txt"))
            {
                FileStream stream = File.Create(_saveLoadPath + deck.DeckName + ".txt");
                stream.Close();
            }

            StreamWriter writer = new StreamWriter(_saveLoadPath + deck.DeckName + ".txt");
            string json = JsonConvert.SerializeObject(deck, _settings);

            writer.Write(json);
            writer.Close();
        }
    }
}