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
        [SerializeField] [Range(0,1)] private float _maxTransparency;
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private float _fadeOutDuration;
        [SerializeField] private Texture2D[] _targetEmissionTextures;
        [SerializeField] private float _crackedEmissionStrength;
        [SerializeField] private GameObject[] _deathParticles;
        [SerializeField] private GameObject _topSupport;
        [SerializeField] private Rigidbody _topSupportInactive;
        [SerializeField] private Vector3 _supportVelocity;
        [SerializeField] private MeshRenderer _innerShieldRenderer;
        private Material _emissionMat;
        private Color _emissionColor;

        // Start is called before the first frame update
        void Awake()
        {
            _health = GetComponent<RingBarrierBehaviour>();
            _health.AddOnTakeDamageAction(UpdateCracks);
            _health.AddOnDeathAction(DeactivateBarrier);
        }

        void Start()
        {
            _emissionMat = _innerShieldRenderer.material;
        }

        public void ResetVisuals()
        {
            _visual.gameObject.SetActive(true);

            for (int i = 0; i < _deathParticles.Length; i++)
            {
                _deathParticles[i].SetActive(false);
            }

            _topSupport.SetActive(true);

            _topSupportInactive.gameObject.SetActive(false);
            _topSupportInactive.velocity = Vector3.zero;
            _topSupportInactive.angularVelocity = Vector3.zero;
            _topSupportInactive.transform.position = _topSupport.transform.position;
            _topSupportInactive.transform.rotation = _topSupport.transform.rotation;

            UpdateCracks();
        }

        private void DeactivateBarrier()
        {
            _visual.gameObject.SetActive(false);

            for (int i = 0; i < _deathParticles.Length; i++)
            {
                _deathParticles[i].SetActive(true);
            }

            _topSupport.SetActive(false);
            _topSupportInactive.gameObject.SetActive(true);
            RoutineBehaviour.Instance.StartNewTimedAction(args => _topSupportInactive.AddForce(_supportVelocity, ForceMode.Impulse), TimedActionCountType.UNSCALEDTIME, Time.fixedDeltaTime);
        }

        private void UpdateCracks()
        {
            float currentHealthPercentage = _health.Health / _health.MaxHealth.Value;
            Texture2D currentTexture = null;

            if (_emissionColor == default(Color))
                _emissionColor = _emissionMat.GetColor("_EmissionColor");

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
    }
}

