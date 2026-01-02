using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class TextFlashBehaviour : MonoBehaviour
    {
        private Color _baseColor;
        [SerializeField]
        private Color _flashColor;
        [SerializeField]
        private float _flashActiveTime;
        [SerializeField]
        private float _flashInactiveTime;
        [SerializeField]
        private bool _flashOnStart;
        [SerializeField]
        private bool _flashOnEnable;
        [SerializeField]
        private bool _flashActive;
        [SerializeField]
        private bool _useImage; // If true, flash Image; else, flash Text

        private Text _text;
        private Image _image;

        public bool FlashActive { get => _flashActive; private set => _flashActive = value; }
        public Color BaseColor { get => _baseColor; set => _baseColor = value; }

        void Awake()
        {
            if (_useImage)
            {
                _image = GetComponent<Image>();
                if (_image != null)
                    BaseColor = _image.color;
            }
            else
            {
                _text = GetComponent<Text>();
                if (_text != null)
                    BaseColor = _text.color;
            }
        }

        private void Start()
        {
            if (_flashOnStart)
                StartFlash();
        }

        private void OnEnable()
        {
            if (_flashOnEnable)
                StartFlash();
        }

        public void StartFlash()
        {
            _flashActive = true;
            StartCoroutine(FlashRoutine());
        }

        public void StopFlash()
        {
            _flashActive = false;
            StopAllCoroutines();
            SetColor(BaseColor);
        }

        private IEnumerator FlashRoutine()
        {
            while (_flashActive)
            {
                SetColor(_flashColor);
                yield return new WaitForSeconds(_flashActiveTime);
                SetColor(BaseColor);
                yield return new WaitForSeconds(_flashInactiveTime);
            }
        }

        private void SetColor(Color color)
        {
            if (_useImage && _image != null)
                _image.color = color;
            else if (!_useImage && _text != null)
                _text.color = color;
        }
    }
}