using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using UnityEngine.Events;
using DG.Tweening;

namespace Lodis.Gameplay
{
    public class CharacterFeedbackBehaviour : FlashBehaviour
    {
        [SerializeField] private Color _invincibleColor;
        [SerializeField] private Color _intangibleColor;
        [SerializeField] private ParticleSystem _stunParticles;
        [SerializeField] private HealthBehaviour _health;
        [SerializeField] private ColorManagerBehaviour _colorManager;
        [SerializeField] private ParticleSystem _deathSparks;
        [SerializeField] private ParticleSystem[] _additionalEffects;
        public ColorManagerBehaviour ColorManager { get => _colorManager; private set => _colorManager = value; }

        void Start()
        {
            _health.AddOnInvincibilityActiveAction(() => FlashAllRenderers(_invincibleColor));
            _health.AddOnIntangibilityActiveAction(() => FlashAllRenderers(_intangibleColor));

            _health.AddOnInvincibilityInactiveAction(ResetAllRenderers);
            _health.AddOnIntangibilityInactiveAction(ResetAllRenderers);

            _health.AddOnStunAction(() => _stunParticles.gameObject.SetActive(true));
            _health.AddOnStunDisabledAction(() => _stunParticles.gameObject.SetActive(false));
        }

        public void FlashAllRenderers(Color color)
        {
            foreach (ColorObject colorObject in ColorManager.ObjectsToColor)
            {
                Flash(colorObject, color);
            }
        }

        public void ResetAllRenderers()
        {
            foreach (ColorObject colorObject in ColorManager.ObjectsToColor)
            {
                colorObject.ObjectRenderer.material.DOKill();
            }
        }

        public void PlayEffect(int index)
        {
            GameObject instance = ObjectPoolBehaviour.Instance.GetObject(_additionalEffects[index].gameObject, transform.position, Camera.main.transform.rotation);
            ObjectPoolBehaviour.Instance.ReturnGameObject(instance, _additionalEffects[index].duration);
            
        }

        private void Update()
        {
            _deathSparks.gameObject.SetActive(Mathf.Ceil(_health.Health) == _health.MaxHealth.Value);
        }
    }
}
