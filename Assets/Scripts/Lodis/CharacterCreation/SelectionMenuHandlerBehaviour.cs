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
        private GameObject _colorSelection;
        [SerializeField]
        private GameObject _firstColorSelection;
        [SerializeField]
        private GameObject _infoPanel;
        [SerializeField]
        private CustomCharacterUIManagerBehaviour _customCharacterUIManager;

        private string _activeSubMenu = "deck";

        public DeckBuildingUIManagerBehaviour DeckBuildingUIManager { get => _deckBuildingUIManager; }
        public CustomCharacterUIManagerBehaviour CustomCharacterUIManager { get => _customCharacterUIManager; }

        public void SetSubMenuActive(string selection)
        {
            _activeSubMenu = selection.ToLower();   
        }

        public void DisplaySelectionScreen()
        {

            switch (_activeSubMenu)
            {
                case "deck":
                    SetCardSelectionActive();
                    break;
                case "armor":
                    SetArmorSelectionActive();
                    break;
                case "color":
                    SetColorSelectionActive();
                    break;
            }
        }

        private void SetColorSelectionActive()
        {
            _cardSelection.SetActive(false);
            _currentDeckCanvas.SetActive(false);
            _armorSelection.SetActive(false);
            _infoPanel.SetActive(false);

            _colorSelection.SetActive(true);
            DeckBuildingUIManager.EventManager.SetSelectedGameObject(_firstColorSelection);

            _instructionText.text = "Select new color";
        }

        private void SetArmorSelectionActive()
        {
            _cardSelection.SetActive(false);
            _currentDeckCanvas.SetActive(false);
            _armorSelection.SetActive(true);
            _infoPanel.SetActive(false);

            CustomCharacterUIManager.UpdateAllIconSections();

            _instructionText.text = "Select armor set";
        }

        private void SetCardSelectionActive()
        {
            DeckBuildingUIManager.UpdateAllIconSections();

            _armorSelection.SetActive(false);
            _currentDeckCanvas.SetActive(true);
            _cardSelection.SetActive(true);
            _infoPanel.SetActive(true);

            DeckBuildingUIManager.UpdateDeck();
            _instructionText.text = "Select an ability";
        }
    }
}