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
        [HideInInspector]
        public Color[] DefaultColors;
        public string[] ShaderProperties;
        public bool OnlyChangeHue = true;

        public void CacheColors()
        {
            DefaultColors = new Color[ShaderProperties.Length];

            for (int i = 0; i < ShaderProperties.Length; i++)
                DefaultColors[i] = ObjectRenderer.material.GetColor(ShaderProperties[i]);
        }

        public void SetColorsToCache()
        {
            for (int i = 0; i < ShaderProperties.Length; i++)
                ObjectRenderer.material.SetColor(ShaderProperties[i], DefaultColors[i]);
        }
    }

    public class ColorManagerBehaviour : MonoBehaviour
    {
        [SerializeField] private GridScripts.GridAlignment _alignment;
        [SerializeField] private bool _autoDetectAlignment;
        [SerializeField] private ColorObject[] _objectsToColor;
        private Color _ownerColor;

        public ColorObject[] ObjectsToColor { get => _objectsToColor; private set => _objectsToColor = value; }

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

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                if (colorObject.OnlyChangeHue)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColors();
            }
        }

    }
}
