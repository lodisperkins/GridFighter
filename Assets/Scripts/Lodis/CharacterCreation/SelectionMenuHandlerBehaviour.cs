using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.UI;
using UnityEngine.UI;

namespace Lodis.CharacterCreation
{
    public class SelectionMenuHandlerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Text _instructionText;

        [SerializeField]
        private GameObject _cardSelection;
        [SerializeField]
        private GameObject _currentDeckCanvas;
        [SerializeField]
        private DeckBuildingUIManagerBehaviour _deckBuildingUIManager;

        [SerializeField]
        private GameObject _armorSelection;
        [SerializeField]
        private CustomCharacterUIManagerBehaviour _customCharacterUIManager;

        private bool _cardSelectionActive;

        public DeckBuildingUIManagerBehaviour DeckBuildingUIManager { get => _deckBuildingUIManager; }
        public CustomCharacterUIManagerBehaviour CustomCharacterUIManager { get => _customCharacterUIManager; }

        public void SetCardMenuActive(bool isActive)
        {
            _cardSelectionActive = isActive;    
        }

        public void DisplaySelectionScreen()
        {
            if (_cardSelectionActive)
            {
                SetCardSelectionActive();
                return;
            }

            SetArmorSelectionActive();
        }

        private void SetArmorSelectionActive()
        {
            _cardSelectionActive = false;


            _cardSelection.SetActive(false);
            _currentDeckCanvas.SetActive(false);
            _armorSelection.SetActive(true);

            CustomCharacterUIManager.UpdateAllIconSections();

            _instructionText.text = "Select armor set";
        }

        private void SetCardSelectionActive()
        {
            _cardSelectionActive = true;

            DeckBuildingUIManager.UpdateAllIconSections();

            _armorSelection.SetActive(false);
            _currentDeckCanvas.SetActive(true);
            _cardSelection.SetActive(true);

            DeckBuildingUIManager.UpdateDeck();
            _instructionText.text = "Select an ability";
        }
    }
}