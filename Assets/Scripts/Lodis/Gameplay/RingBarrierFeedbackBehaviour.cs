using System;
using Lodis.Utility;
using UnityEngine;
using static Lodis.Utility.RoutineBehaviour;

namespace Lodis.Gameplay
{
    public class RingBarrierFeedbackBehaviour : MonoBehaviour
    {
        private RingBarrierBehaviour _health;
        [SerializeField] private MeshRenderer _visual;
        [SerializeField] private Gradient _healthGradient;
        private float _baseTransparency;
        [SerializeField] [Range(0,1)] private float _maxTransparency;
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private float _fadeOutDuration;
        [SerializeField] private Texture2D[] _targetEmissionTextures;
        [SerializeField] private float _crackedEmissionStrength;
        [SerializeField] private GameObject[] _deathParticles;
        [SerializeField] private GameObject _topSupport;
        [SerializeField] private GameObject _topSupportInactive;
        [SerializeField] private Vector3 _supportVelocity;
        private float _currentDuration;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Color _materialColor;
        private bool _fadeEnabled;
        private int _fadeDirection;
        private DelayedAction _fadeAction;
        private Material _emissionMat;
        private Color _emissionColor;

        // Start is called before the first frame update
        void Awake()
        {
            _emissionMat = _visual.materials[1];
            _emissionColor = _emissionMat.GetColor("_EmissionColor");
            _health = GetComponent<RingBarrierBehaviour>();
            _health.AddOnTakeDamageAction(UpdateCracks);
            _health.AddOnDeathAction(DeactivateBarrier);
        }

        private void DeactivateBarrier()
        {
            _visual.gameObject.SetActive(false);

            for (int i = 0; i < _deathParticles.Length; i++)
            {
                _deathParticles[i].SetActive(true);
            }

            _topSupport.SetActive(false);
            _topSupportInactive.SetActive(true);
            RoutineBehaviour.Instance.StartNewTimedAction(args => _topSupportInactive.GetComponent<Rigidbody>().AddForce(_supportVelocity, ForceMode.Impulse), TimedActionCountType.UNSCALEDTIME, Time.fixedDeltaTime);
        }

        private void StartFadeIn()
        {
            if (_fadeAction?.GetEnabled() == true)
                Instance.StopAction(_fadeAction);

            _materialColor = _visual.material.color;
            _materialColor.a = _baseTransparency;
            _visual.material.color = _materialColor;
            
            _fadeEnabled = true;
            _currentDuration = _fadeInDuration;
            _fadeDirection = 1;
            _fadeAction = Instance.StartNewConditionAction(StartFadeOut, condition => _materialColor.a >= _maxTransparency);
        }

        private void StartFadeOut(params object[] objects)
        {
            if (_fadeAction?.GetEnabled() == true)
                Instance.StopAction(_fadeAction);

            _fadeDirection = -1;
            _currentDuration = _fadeOutDuration;
            _fadeAction = Instance.StartNewConditionAction(args =>
            {
                _fadeEnabled = false;
                _materialColor.a = _baseTransparency;
                _visual.material.color = _materialColor;
            }, condition => _materialColor.a <= _baseTransparency);
        }

        private void UpdateCracks()
        {
            float currentHealthPercentage = _health.Health / _health.MaxHealth.Value;
            Texture2D currentTexture = null;

            _emissionMat.SetColor("_EmissionColor", _emissionColor * _crackedEmissionStrength);

            if (currentHealthPercentage < .25f)
                currentTexture = _targetEmissionTextures[2];
            else if (currentHealthPercentage < .50f)
                currentTexture = _targetEmissionTextures[1];
            else if (currentHealthPercentage < .75f)
                currentTexture = _targetEmissionTextures[0];
            else
                _emissionMat.SetColor("_EmissionColor", _emissionColor);

            _emissionMat.SetTexture("_EmissionMap", currentTexture);
        }

        
        // Update is called once per frame
        void Update()
        {
            //if (!_fadeEnabled)
            //{
            //    _visual.material.SetColor(EmissionColor, _healthGradient.Evaluate(_health.Health / _health.MaxHealth.Value));
            //    _baseTransparency = _healthGradient.Evaluate(_health.MaxHealth.Value / _health.Health).a;
            //    return;
            //}

            //_materialColor = _visual.material.color;
            //_materialColor.a += (Time.deltaTime * (_maxTransparency - _baseTransparency) / _currentDuration) *
            //                    _fadeDirection;
            //_visual.material.color = _materialColor;
        }
    }
}

