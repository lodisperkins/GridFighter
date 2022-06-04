using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis
{
    public class AbilityImageDisplayBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Gameplay.MovesetBehaviour _moveSet;
        [SerializeField]
        private IntVariable _playerID;
        [SerializeField]
        private Image _abilitySlot1Image;
        [SerializeField]
        private Image _abilitySlot2Image;
        private float _ability1Cost;
        private float _ability2Cost;
        [SerializeField]
        private bool _updateAbilityColors;

        private void Start()
        {
            if (!_moveSet)
                _moveSet = BlackBoardBehaviour.Instance.GetPlayerFromID(_playerID).GetComponent<MovesetBehaviour>();

            _moveSet.AddOnUpdateHandAction(UpdateImages);
            UpdateImages();
        }

        public void UpdateImages()
        {
            if (!_moveSet || _moveSet.SpecialAbilitySlots.Length <= 0)
                return;

            Ability ability1 = _moveSet.GetAbilityInCurrentSlot(0);
            Ability ability2 = _moveSet.GetAbilityInCurrentSlot(1);


            if (ability1 != null)
            {
                _abilitySlot1Image.enabled = true;
                _ability1Cost = ability1.abilityData.EnergyCost;
                _abilitySlot1Image.sprite = ability1.abilityData.DisplayIcon;
            }
            else
                _abilitySlot1Image.enabled = false;

            if (ability2 != null)
            {
                _abilitySlot2Image.enabled = true;
                _ability2Cost = ability2.abilityData.EnergyCost;
                _abilitySlot2Image.sprite = ability2.abilityData.DisplayIcon;
            }
            else
                _abilitySlot2Image.enabled = false;
        }

        private void Update()
        {
            if (!_updateAbilityColors)
                return;

            if (_moveSet.Energy >= _ability1Cost)
                _abilitySlot1Image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_ability1Cost];
            else
                _abilitySlot1Image.color = Color.white;

            if (_moveSet.Energy >= _ability2Cost)
                _abilitySlot2Image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_ability2Cost];
            else
                _abilitySlot2Image.color = Color.white;
        }
    }
}
