using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/Color")]
    public class ColorVariable : ScriptableObject
    {
        [SerializeField]
        private Color _val;
        public Color Value
        {
            get
            {
                return _val;
            }
            set
            {
                _val = value;
            }
        }

        public void SetColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out _val);
        }

        public void Init(Color value)
        {
            _val = value;
        }

        public static implicit operator Vector4(ColorVariable color) => color.Value;
        public static implicit operator Color(ColorVariable color) => color.Value;

        public static ColorVariable CreateInstance(Color value)
        {
            var data = CreateInstance<ColorVariable>();
            data.Init(value);
            return data;
        }
    }
}
