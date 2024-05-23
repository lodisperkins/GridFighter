using Lodis.Sound;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;

namespace Lodis.UI
{
    [RequireComponent(typeof(Slider))]
    public class EventSliderBehaviour : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Slider Events")]
        [SerializeField]
        private UnityEvent _onSelect;
        [SerializeField]
        private UnityEvent _onDeselect;
        [SerializeField]
        private UnityEvent _onHighlight;
        [SerializeField]
        private UnityEvent _onUnhighlight;
        private Slider _slider;

        [Header("Slider Sounds")]
        [SerializeField]
        private AudioClip _selectSound;

        private Image _image;
        private int _counter;
        private bool _framePassed;

        public SliderEvent onValueChanged { get => _slider.onValueChanged; }
        public float value { get => _slider.value; set => _slider.value = value; }

        public Slider UISlider { get => _slider; set => _slider = value; }

        private void Awake()
        {
            Init();
        }

        public void Update()
        {
            _counter = 0;
        }

        public void Init()
        {
            UISlider = GetComponent<Slider>();
        }

        public void OnSelect()
        {
            if (!UISlider)
                UISlider = GetComponent<Slider>();

            UISlider.OnSelect(null);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _onDeselect?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _onHighlight?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _onUnhighlight?.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SoundManagerBehaviour.Instance.PlaySound(_selectSound);
            _onSelect?.Invoke();
        }

        public void AddOnSelectEvent(UnityAction action)
        {
            _onSelect.AddListener(action);
        }

        public void AddOnDeselectEvent(UnityAction action)
        {
            _onDeselect.AddListener(action);
        }
    }
}