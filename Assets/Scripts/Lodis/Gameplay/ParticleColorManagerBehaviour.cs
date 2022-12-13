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

        public void SetColors()
        {
            foreach (ParticleSystem particleSystem in _particleSystems)
            {

                if (_changeStartColor)
                {
                    ParticleSystem.MainModule main = particleSystem.main;
                    main.startColor = _color;
                }

                if (_changeColorOverLifetime)
                {
                    ParticleSystem.ColorOverLifetimeModule lifetimeModule = particleSystem.colorOverLifetime;
                    lifetimeModule.color = _color;  
                }

                if (_changeColorBySpeed)
                {
                    ParticleSystem.ColorBySpeedModule speedModule = particleSystem.colorBySpeed;
                    speedModule.color = _color; 
                }
            }
        }
    }
}