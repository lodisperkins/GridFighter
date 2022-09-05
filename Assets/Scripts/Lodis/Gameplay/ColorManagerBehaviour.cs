using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    [System.Serializable]
    public class ColorObject
    {
        public Renderer ObjectRenderer;
        public string[] ShaderProperties;
        public bool OnlyChangeHue = true;
    }

    public class ColorManagerBehaviour : MonoBehaviour
    {
        [SerializeField] private GridScripts.GridAlignment _alignment;
        [SerializeField] private bool _autoDetectAlignment;
        [SerializeField] private ColorObject[] _objectsToColor;
        private Color _ownerColor;


        private void SetHue(ColorObject objectToColor)
        {
            foreach (string property in objectToColor.ShaderProperties)
            {
                objectToColor.ObjectRenderer.material.ChangeHue(_ownerColor, property);
            }
        }

        private void SetColor(ColorObject objectToColor)
        {
            foreach (string property in objectToColor.ShaderProperties)
            {
                objectToColor.ObjectRenderer.material.SetColor(property, _ownerColor);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (_autoDetectAlignment)
                _alignment = GetComponent<GridMovementBehaviour>().Alignment;

            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(_alignment);    

            foreach (ColorObject colorObject in _objectsToColor)
            {
                if (colorObject.OnlyChangeHue)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);
            }
        }

    }
}
