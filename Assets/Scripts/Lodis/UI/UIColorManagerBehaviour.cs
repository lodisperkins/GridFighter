using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    /// <summary>
    /// An object that stores a renderer and th color properties to adjust on that renderer.
    /// </summary>
    [System.Serializable]
    public class ColorObject
    {
        public Image ObjectImage;
        private Color _defaultColor;
        public bool OnlyChangeHue = true;

        /// <summary>
        /// Store the current colors that are on this object.
        /// </summary>
        public virtual void CacheColor()
        {
            _defaultColor = ObjectImage.color;
        }

        /// <summary>
        /// Override the current colors on this object with the colors currently cached.
        /// </summary>
        public virtual void SetColorToCache()
        {
            ObjectImage.color = _defaultColor;
        }
    }
    public class UIColorManagerBehaviour : MonoBehaviour
    {
        [Tooltip("The character alignment that these objects will match the color of.")]
        [SerializeField] private GridScripts.GridAlignment _alignment;
        [Tooltip("The objects that will have their colors changed to match the alignment.")]
        [SerializeField] private ColorObject[] _objectsToColor;
        private Color _ownerColor;
        [SerializeField]
        private bool _setColorsOnStart = true;

        public ColorObject[] ObjectsToColor { get => _objectsToColor; private set => _objectsToColor = value; }

        private void SetHue(ColorObject objectToColor)
        {
            objectToColor.ObjectImage.ChangeHue(_ownerColor);
        }

        private void SetColor(ColorObject objectToColor)
        {
            objectToColor.ObjectImage.color = _ownerColor;
        }

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        public void SetColors()
        {
            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(_alignment);

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                if (colorObject.OnlyChangeHue)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColor();
            }
        }

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        public void SetColors(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#" + hex, out color);
            _ownerColor = color;

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                if (colorObject.OnlyChangeHue)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColor();
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

                colorObject.CacheColor();
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (_setColorsOnStart)
                SetColors();
        }
    }
}