﻿using System.Collections;
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
        private Image _topBar;

        public void SetPrimaryColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            _primaryColor = color;
            _backgroundImage.material.SetColor("StartColor", color);
            _topBar.color = _primaryColor + _secondaryColor;
        }

        public void SetSecondaryColor(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            _primaryColor = color;
            _backgroundImage.material.SetColor("EndColor", color);
            _topBar.color = _primaryColor + _secondaryColor;
        }
    }
}