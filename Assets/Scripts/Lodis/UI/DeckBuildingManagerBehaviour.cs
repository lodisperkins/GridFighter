using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Lodis.UI
{
    public class DeckBuildingManagerBehaviour : MonoBehaviour
    {
        private string[] _deckOptions;
        private Deck _normalDeck;
        private Deck _specialDeck;
        private string _deckName;
        private string _saveLoadPath;
        protected JsonSerializerSettings _settings;

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

        public void LoadPresetDeck(string deckName)
        {
            NormalDeck = Resources.Load<Deck>("Decks/P_" + deckName + "_Normals");
            SpecialDeck = Resources.Load<Deck>("Decks/P_" + deckName + "_Specials");
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