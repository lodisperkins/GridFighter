using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Lodis.UI
{
    public class MoveDescriptionBehaviour : MonoBehaviour
    {
        [SerializeField]
        private AbilityType _abilityType;
        [SerializeField]
        private Image _iconImage;
        private Text _description;
        private VideoPlayer _videoPlayer;
        private AbilityData _data;
        private EventButtonBehaviour _button;

        public AbilityType AbilityType { get => _abilityType; private set => _abilityType = value; }
        public AbilityData Data { get => _data; set => _data = value; }

        private void Awake()
        {
            _button = GetComponent<EventButtonBehaviour>();
            _button.AddOnSelectEvent(SetAbility);
        }

        public void Init(Text description, VideoPlayer videoPlayer, AbilityData data)
        {
            _data = data;
            _description = description;
            _videoPlayer = videoPlayer;
            _iconImage.sprite = _data.DisplayIcon;
            _iconImage.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_data.EnergyCost];
        }

        public void SetAbility()
        {
            string colorBegin = "<color=#" + ColorUtility.ToHtmlStringRGBA(_iconImage.color) + ">";
            string colorEnd = "</color>";
            _description.text =  colorBegin + _data.abilityName  + colorEnd + "\nEnergy Cost: " + colorBegin + _data.EnergyCost + colorEnd + "\n" + _data.abilityDescription;
            _videoPlayer.clip = _data.exampleClip;
        }
    }
}