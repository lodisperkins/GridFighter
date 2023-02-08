using System;
using Lodis.Utility;
using UnityEngine;
using static Lodis.Utility.RoutineBehaviour;
using Lodis.Sound;
namespace Lodis.Gameplay
{
    public class RingBarrierFeedbackBehaviour : MonoBehaviour
    {
        private RingBarrierBehaviour _health;
        [Tooltip("The mesh of the game object that is the visual representation of the barrier.")]
        [SerializeField] private MeshRenderer _visual;
        [Tooltip("The textures that will be used to display the barriers battle damage. The textures should be ordered from smallest to greatest damage.")]
        [SerializeField] private Texture2D[] _targetEmissionTextures;
        [Tooltip("How intense the glow of the damage textures are.")]
        [SerializeField] private float _crackedEmissionStrength;
        [Tooltip("The particles that will spawn when the barrier is broken.")]
        [SerializeField] private GameObject[] _deathParticles;

        [Tooltip("The support at the top of the barrier.")]
        [SerializeField] private GameObject _topSupport;
        [Tooltip("The replacement support bar that spawns when the barrier explodes.")]
        [SerializeField] private Rigidbody _topSupportInactive;
        [Tooltip("The the velocity applied to the replacement support bar when the barrier explodes.")]
        [SerializeField] private Vector3 _supportVelocity;
        [Tooltip("The renderer attached to the inner portion of the barrier that displays the shield effect.")]
        [SerializeField] private MeshRenderer _innerShieldRenderer;

        [SerializeField] private AudioClip _damageSound;
        [SerializeField] private AudioClip _destroyedSound;
        private Material _emissionMat;
        private Color _emissionColor;

        // Start is called before the first frame update
        void Awake()
        {
            _health = GetComponent<RingBarrierBehaviour>();
            _health.AddOnTakeDamageAction(() =>
            {
                UpdateCracks();
                SoundManagerBehaviour.Instance.PlaySound(_damageSound);
            }
            );
            _health.AddOnDeathAction(() =>
            {
                DeactivateBarrier();
                SoundManagerBehaviour.Instance.PlaySound(_destroyedSound);
            }
            );
        }

        void Start()
        {
            _emissionMat = _innerShieldRenderer.material;
        }

        /// <summary>
        /// Makes the visual game object visible again, disables death particles, and removes the damage textures.
        /// </summary>
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

        /// <summary>
        /// Makes the barrier explode and become invisible.
        /// </summary>
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

        /// <summary>
        /// Changes the damage texture to a new one based on the current health.
        /// </summary>
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

