using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class BackgroundColorBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Color _primaryColor;
        [SerializeField]
        private Color _secondaryColor;
        [SerializeField]
        private Image _backgroundImage;
        [SerializeField]
        private Image[] _primaryColorImages;
        [SerializeField]
        private Image[] _secondaryColorImages;
        [SerializeField]
        private Image[] _blendColorImages;
        [SerializeField]
        private Image _displayBorder;

        private void Awake()
        {
            _backgroundImage.material = Instantiate(_backgroundImage.material);

            UpdatePrimaryColorImages();
            UpdateSecondaryColorImages();
            UpdateBlendColorImages();

            _backgroundImage.material.SetColor("StartColor", _primaryColor);
            _backgroundImage.material.SetColor("EndColor", _secondaryColor);

        }

        private void UpdatePrimaryColorImages()
        {
            foreach (Image image in _primaryColorImages)
            {
                image.color = _primaryColor;
            }
        }

        private void UpdateSecondaryColorImages()
        {
            foreach (Image image in _secondaryColorImages)
            {
                image.color = _secondaryColor;
            }
        }

        private void UpdateBlendColorImages()
        {
            foreach (Image image in _blendColorImages)
            {
                image.color = _primaryColor + _secondaryColor;
            }
        }

        public void SetPrimaryColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            _primaryColor = color;
            _backgroundImage.material.SetColor("StartColor", color);
            UpdatePrimaryColorImages();
            UpdateBlendColorImages();
        }

        public void SetSecondaryColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            _backgroundImage.material.SetColor("EndColor", color);
            _secondaryColor = color;
            UpdateSecondaryColorImages();
            UpdateBlendColorImages();
        }

        public void SetDisplayBorderColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            _displayBorder.color = color;
        }
    }
}