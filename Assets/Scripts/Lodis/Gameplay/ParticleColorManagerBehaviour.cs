using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ParticleColorManagerBehaviour : MonoBehaviour
    {
        [SerializeField] 
        [Tooltip("The grid alignment that this particle will set its color to match.")]
        private GridScripts.GridAlignment _alignment;
        [SerializeField]
        [Tooltip("If true, only the hue will be effected by the color change.")]
        private bool _onlyChangeHue;

        [Header("Color Change Toggles")]

        [SerializeField]
        private bool _changeStartColor;
        [SerializeField]
        private bool _changeColorOverLifetime;
        [SerializeField]
        private bool _changeColorBySpeed;

        [Space]
        [SerializeField]
        [Tooltip("All of the particle effects that will have their colors changed. Mainly used to change children,but can be used to edit any particle effect.")]
        private ParticleSystem[] _particleSystems;
        private Color _color;

        /// <summary>
        /// The grid alignment that this particle will set its color to match.
        /// </summary>
        public GridAlignment Alignment { get => _alignment; set => _alignment = value; }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize colors in start to be sure the alignment colors have been set up already.
            _color = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(Alignment);
            SetColors();
        }

        /// <param name="oldColor">The current color of the property that is being changed.</param>
        /// <returns>A color value that only matches the hue of the alignment color.</returns>
        public Color GetHue(Color oldColor)
        {
            Color newColor = _color;
            Color propertyColor = oldColor;
            Vector3 propertyHSV = new Vector3();
            Vector3 targetHSV = new Vector3();

            //Converts the current color and the desired color to HSV in order to get the hue value for both.
            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(newColor, out targetHSV.x, out targetHSV.y, out targetHSV.z);

            //Set the hue of the current color to that of the alignment color.
            propertyHSV.x = targetHSV.x;

            //Return the new color converted back to standard RGB format.
            newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);
            return newColor;
        }

        /// <summary>
        /// Changes the color of each property that is allowed to be changed in each particle system.
        /// </summary>
        public void SetColors()
        {
            //Iterate through all systems to change the colors for each.
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                //If allowed to change the start color...
                if (_changeStartColor)
                {
                    ParticleSystem.MainModule main = particleSystem.main;

                    //...either just change the hue or the entire color.
                    if (_onlyChangeHue)
                        main.startColor = GetHue(main.startColor.color);
                    else
                        main.startColor = _color;
                }

                //If allowed to change the "ColorOverLifetime" color...
                if (_changeColorOverLifetime)
                {
                    ParticleSystem.ColorOverLifetimeModule lifetimeModule = particleSystem.colorOverLifetime;

                    //...either just change the hue or the entire color.
                    if (_onlyChangeHue)
                        lifetimeModule.color = GetHue(lifetimeModule.color.color);
                    else
                        lifetimeModule.color = _color;
                }

                //If allowed to change the "ColorBySpeed" color...
                if (_changeColorBySpeed)
                {
                    ParticleSystem.ColorBySpeedModule speedModule = particleSystem.colorBySpeed;

                    //...either just change the hue or the entire color.
                    if (_onlyChangeHue)
                        speedModule.color = GetHue(speedModule.color.color);
                    else
                        speedModule.color = _color;
                }
            }
        }
    }
}