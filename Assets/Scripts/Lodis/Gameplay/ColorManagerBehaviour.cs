using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    /// <summary>
    /// An object that stores a renderer and th color properties to adjust on that renderer.
    /// </summary>
    [System.Serializable]
    public class ColorObject
    {
        public Renderer ObjectRenderer;
        [HideInInspector]
        public Color[] DefaultColors;
        public string[] ShaderProperties;
        public bool OnlyChangeHue = true;

        /// <summary>
        /// Store the current colors that are on this object.
        /// </summary>
        public void CacheColors()
        {
            DefaultColors = new Color[ShaderProperties.Length];

            for (int i = 0; i < ShaderProperties.Length; i++)
                DefaultColors[i] = ObjectRenderer.material.GetColor(ShaderProperties[i]);
        }

        /// <summary>
        /// Override the current colors on this object with the colors currently cached.
        /// </summary>
        public void SetColorsToCache()
        {
            for (int i = 0; i < ShaderProperties.Length; i++)
                ObjectRenderer.material.SetColor(ShaderProperties[i], DefaultColors[i]);
        }
    }

    public class ColorManagerBehaviour : MonoBehaviour
    {
        [Tooltip("The character alignment that these objects will match the color of.")]
        [SerializeField] private GridScripts.GridAlignment _alignment;
        [Tooltip("Select this if the grid alignment should be found using an attached movement component.")]
        [SerializeField] private bool _autoDetectAlignment;
        [Tooltip("The objects that will have their colors changed to match the alignment.")]
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

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        public void SetColors()
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

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        /// <param name="alignmentID">The ID to use when finding the new alignement. 
        /// 0 = None
        /// 1 = Left
        /// 2 = Right
        /// 3 = Any</param>
        public void SetColors(int alignmentID)
        {
            GridScripts.GridAlignment alignment = (GridScripts.GridAlignment)alignmentID;
            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(alignment);

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                if (colorObject.OnlyChangeHue)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColors();
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            SetColors();
        }

    }
}
