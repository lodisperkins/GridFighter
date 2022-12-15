using Lodis.GridScripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ParticleColorManagerBehaviour : MonoBehaviour
    {
        [SerializeField] 
        private GridScripts.GridAlignment _alignment;
        [SerializeField]
        private bool _onlyChangeHue;
        [SerializeField]
        private bool _changeStartColor;
        [SerializeField]
        private bool _changeColorOverLifetime;
        [SerializeField]
        private bool _changeColorBySpeed;
        [SerializeField]
        private ParticleSystem[] _particleSystems;
        private Color _color;

        public GridAlignment Alignment { get => _alignment; set => _alignment = value; }

        // Start is called before the first frame update
        void Start()
        {
            _color = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(Alignment);
            SetColors();
        }

        public Color ChangeHue(Color oldColor)
        {
            Color newColor = _color;
            Color propertyColor = oldColor;
            Vector3 propertyHSV = new Vector3();
            Vector3 targetHSV = new Vector3();

            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(newColor, out targetHSV.x, out targetHSV.y, out targetHSV.z);

            propertyHSV.x = targetHSV.x;

            newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

            return newColor;
        }

        public void SetColors()
        {
            foreach (ParticleSystem particleSystem in _particleSystems)
            {

                if (_changeStartColor)
                {
                    ParticleSystem.MainModule main = particleSystem.main;

                    if (_onlyChangeHue)
                        main.startColor = ChangeHue(main.startColor.color);
                    else
                        main.startColor = _color;
                }

                if (_changeColorOverLifetime)
                {
                    ParticleSystem.ColorOverLifetimeModule lifetimeModule = particleSystem.colorOverLifetime;

                    if (_onlyChangeHue)
                        lifetimeModule.color = ChangeHue(lifetimeModule.color.color);
                    else
                        lifetimeModule.color = _color;
                }

                if (_changeColorBySpeed)
                {
                    ParticleSystem.ColorBySpeedModule speedModule = particleSystem.colorBySpeed;

                    if (_onlyChangeHue)
                        speedModule.color = ChangeHue(speedModule.color.color);
                    else
                        speedModule.color = _color;
                }
            }
        }
    }
}