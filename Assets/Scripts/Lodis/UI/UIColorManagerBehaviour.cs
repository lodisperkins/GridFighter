﻿using Lodis.Gameplay;
using Lodis.GridScripts;
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
        [Tooltip("When receiving new colors under this value, it will always override everything including the hue. Happens even if OnlyChangeHue is set to true.")]
        public float SaturationThreshold = 10;

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

    /// <summary>
    /// An object that stores a renderer and th color properties to adjust on that renderer.
    /// </summary>
    [System.Serializable]
    public class TextColorObject
    {
        public Text ObjectText;
        private Color _defaultColor;
        public bool OnlyChangeHue = true;
        [Tooltip("When receiving new colors under this value, it will always override everything including the hue. Happens even if OnlyChangeHue is set to true.")]
        public float SaturationThreshold = 10;

        /// <summary>
        /// Store the current colors that are on this object.
        /// </summary>
        public virtual void CacheColor()
        {
            _defaultColor = ObjectText.color;
        }

        /// <summary>
        /// Override the current colors on this object with the colors currently cached.
        /// </summary>
        public virtual void SetColorToCache()
        {
            ObjectText.color = _defaultColor;
        }
    }
    public class UIColorManagerBehaviour : MonoBehaviour
    {
        [Tooltip("The character alignment that these objects will match the color of.")]
        [SerializeField] private GridScripts.GridAlignment _alignment;
        [Tooltip("The objects that will have their colors changed to match the alignment.")]
        [SerializeField] private ColorObject[] _objectsToColor;
        [Tooltip("The objects that will have their colors changed to match the alignment.")]
        [SerializeField] private TextColorObject[] _textToColor;
        private Color _ownerColor;
        [SerializeField]
        private bool _setColorsOnStart = true;

        public ColorObject[] ObjectsToColor { get => _objectsToColor; private set => _objectsToColor = value; }
        public GridAlignment Alignment { get => _alignment; set => _alignment = value; }
        public TextColorObject[] TextToColor { get => _textToColor; set => _textToColor = value; }

        private void SetHue(ColorObject objectToColor)
        {
            objectToColor.ObjectImage.ChangeHue(_ownerColor);
        }

        private void SetColor(ColorObject objectToColor)
        {
            objectToColor.ObjectImage.color = _ownerColor;
        }

        private void SetHue(TextColorObject objectToColor)
        {
            objectToColor.ObjectText.ChangeHue(_ownerColor);
        }

        private void SetColor(TextColorObject objectToColor)
        {
            objectToColor.ObjectText.color = _ownerColor;
        }

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        public void SetColors()
        {
            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(Alignment);

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColor();
            }

            if (TextToColor == null || TextToColor.Length == 0)
                return;

            foreach (TextColorObject colorObject in TextToColor)
            {
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
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
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColor();
            }

            if (TextToColor == null || TextToColor.Length == 0)
                return;

            foreach (TextColorObject colorObject in TextToColor)
            {
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
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
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColor();
            }

            if (TextToColor == null || TextToColor.Length == 0)
                return;

            foreach (TextColorObject colorObject in TextToColor)
            {
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
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