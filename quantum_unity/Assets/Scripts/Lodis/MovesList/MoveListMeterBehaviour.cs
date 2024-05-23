using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Lodis.UI
{
    public class MoveListMeterBehaviour : MonoBehaviour
    {
        [SerializeField]
        private CursorLerpBehaviour _playerCursor;
        [SerializeField]
        private Slider _slider;
        [SerializeField]
        private Text _energyTextCounter;
        [SerializeField]
        private Image _fill;
        [SerializeField]
        private Image _energyTextCounterImage;
        [SerializeField]
        private FloatVariable _maxEnergy;
        [SerializeField]
        private float _lerpDuration = 0.1f;

        // Start is called before the first frame update
        void Start()
        {
            _playerCursor.AddOnSelectionUpdatedAction(UpdateMeterValue);
            _slider.maxValue = _maxEnergy;
        }

        private void UpdateMeterValue()
        {
            if (_playerCursor.EventSystem.currentSelectedGameObject == null)
                return;

            AbilityData data = _playerCursor.EventSystem.currentSelectedGameObject.GetComponent<MoveDescriptionBehaviour>()?.Data;
            if (data != null)
                _slider.DOValue(data.EnergyCost, _lerpDuration).SetUpdate(true);
            else return;

            _energyTextCounter.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)data.EnergyCost];

            int currentEnergy = (int)data.EnergyCost;

            if (currentEnergy == 0)
                _energyTextCounter.color = new Vector4(_energyTextCounter.color.r, _energyTextCounter.color.g, _energyTextCounter.color.b, 0.5f);
            else
                _energyTextCounter.color = new Vector4(_energyTextCounter.color.r, _energyTextCounter.color.g, _energyTextCounter.color.b, 1);

            _energyTextCounter.text = currentEnergy.ToString();
            _fill.color = BlackBoardBehaviour.Instance.AbilityCostColors[currentEnergy];
            _energyTextCounterImage.color = _fill.color;
        }
    }
}