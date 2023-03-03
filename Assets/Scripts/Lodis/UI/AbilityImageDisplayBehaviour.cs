﻿using Lodis.Gameplay;
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
        [SerializeField]
        private bool _displayNext;
        [SerializeField]
        private Text _costText1;
        [SerializeField]
        private Text _costText2;

        private void Start()
        {
            if (!_moveSet)
            {
                GameObject target = BlackBoardBehaviour.Instance.GetPlayerFromID(_playerID);

                if (!target) return;
                _moveSet = target.GetComponent<MovesetBehaviour>();
            }

            _moveSet.AddOnUpdateHandAction(UpdateImages);
            UpdateImages();
        }

        public void UpdateImages()
        {
            if (!_moveSet || _moveSet.SpecialAbilitySlots.Length <= 0)
                return;

            Ability ability1 = null;
            Ability ability2 = null;

            if (!_displayNext)
            {
                ability1 = _moveSet.GetAbilityInCurrentSlot(0);
                ability2 = _moveSet.GetAbilityInCurrentSlot(1);
            }
            else
                ability1 = _moveSet.NextAbilitySlot;


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
            if (!_updateAbilityColors || !_moveSet)
                return;

            if (_moveSet.Energy >= _ability1Cost)
                _abilitySlot1Image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_ability1Cost];
            else
                _abilitySlot1Image.color = Color.white;

            if (_moveSet.Energy >= _ability2Cost)
                _abilitySlot2Image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_ability2Cost];
            else
                _abilitySlot2Image.color = Color.white;

            if (_abilitySlot1Image.enabled)
            {
                _costText1.text = _ability1Cost.ToString();
                _costText1.color = _abilitySlot1Image.color;
            }
            else
            {
                _costText1.text = "X";
                _costText1.color = Color.grey;
            }
            if (_abilitySlot2Image.enabled)
            {
                _costText2.text = _ability2Cost.ToString();
                _costText2.color = _abilitySlot2Image.color;
            }
            else
            {
                _costText2.text = "X";
                _costText2.color = Color.grey;
            }
        }
    }
}
