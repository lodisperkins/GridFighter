using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace GridGame.VariableScripts
{
    [CreateAssetMenu(menuName = "Variables/MenuItem")]
    public class MenuItem : ScriptableObject
    {
        [SerializeField]
        private Image _image;
        [SerializeField]
        private Color _highlightedColor;
        [SerializeField]
        private Color _defaultColor;
        private bool _selected;
        public UnityAction commands;

        public bool Selected
        {
            get
            {
                return _selected;
            }

            set
            {
                if (value)
                {
                    _image.color = _highlightedColor;
                }
                else
                {
                    _image.color = _defaultColor;
                }
                _selected = value;
            }
        }
    }
}


