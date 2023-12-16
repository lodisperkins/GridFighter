using Lodis.CharacterCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    [RequireComponent(typeof(Button))]
    public class ColorButtonBehaviour : EventButtonBehaviour
    {
        enum CustColorType
        {
            FACE,
            HAIR
        }

        [SerializeField]
        private CustColorType _colorType;
        [SerializeField]
        private CustomCharacterManagerBehaviour _custManager;

        // Start is called before the first frame update
        void Start()
        {
            AddOnClickEvent(UpdateColor);
        }

        private void UpdateColor()
        {
            switch (_colorType)
            {
                case CustColorType.FACE:
                    _custManager.ReplaceFaceColor(ButtonImage.color);
                    break;
                case CustColorType.HAIR:
                    _custManager.ReplaceHairColor(ButtonImage.color);
                    break;
            }
        }
    }
}