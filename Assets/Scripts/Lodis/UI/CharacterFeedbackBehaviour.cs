using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using UnityEngine.Events;
using DG.Tweening;
using CustomEventSystem;
using Lodis.Movement;
using Lodis.Accessories;

namespace Lodis.Gameplay
{
    public class CharacterFeedbackBehaviour : FlashBehaviour
    {
        [SerializeField] private Color _invincibleColor;
        [SerializeField] private Color _intangibleColor;
        [SerializeField] private ParticleSystem _stunParticles;
        [SerializeField] private HealthBehaviour _health;
        private MovesetBehaviour _moveSet;
        private GridMovementBehaviour _movement;
        [SerializeField] private ColorManagerBehaviour _colorManager;
        [SerializeField] private ParticleSystem _deathSparks;
        [SerializeField] private ParticleSystem[] _additionalEffects;
        [SerializeField] private AccessoryEffectBehaviour _accessory;

        public ColorManagerBehaviour ColorManager { get => _colorManager; private set => _colorManager = value; }

        void Start()
        {
            _health.AddOnInvincibilityActiveAction(() => FlashAllRenderers(_invincibleColor));
            _health.AddOnIntangibilityActiveAction(() => FlashAllRenderers(_intangibleColor));

            _health.AddOnInvincibilityInactiveAction(ResetAllRenderers);
            _health.AddOnIntangibilityInactiveAction(ResetAllRenderers);

            _health.AddOnStunAction(() => _stunParticles.gameObject.SetActive(true));
            _health.AddOnStunDisabledAction(() => _stunParticles.gameObject.SetActive(false));
            _moveSet = GetComponentInParent<MovesetBehaviour>();
            _movement = GetComponentInParent<GridMovementBehaviour>();
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

        public void PlayEffectOnLimb(EventArguments args)
        {
            bool shouldMirror = args.BoolArgs[0];
            int index = args.IntArgs[0];
            if (shouldMirror)
            {
                index = _movement.Alignment == GridScripts.GridAlignment.LEFT ? args.IntArgs[0] : args.IntArgs[0] + 2;

                if (index > 3)
                    index -= 4;
            }

            Transform spawnTransform = _moveSet.GetSpawnTransform((LimbType)index);

            for (int i = 0; i < args.UnityObjectArgs.Length; i++)
            {
                GameObject instance = ObjectPoolBehaviour.Instance.GetObject(args.UnityObjectArgs[i] as GameObject, spawnTransform);
                ObjectPoolBehaviour.Instance.ReturnGameObject(instance, args.FloatArgs[i]);
            }
        }

        public void PlayEffectFromEvent(EventArguments args)
        {
            for (int i = 0; i < args.UnityObjectArgs.Length; i++)
            {
                GameObject instance = ObjectPoolBehaviour.Instance.GetObject(args.UnityObjectArgs[i] as GameObject, transform.position, Camera.main.transform.rotation);
                ObjectPoolBehaviour.Instance.ReturnGameObject(instance, args.FloatArgs[i]);
            }

        }

        public void PlayEffect(int index)
        {
            GameObject instance = ObjectPoolBehaviour.Instance.GetObject(_additionalEffects[index].gameObject, transform.position, Camera.main.transform.rotation);
            ObjectPoolBehaviour.Instance.ReturnGameObject(instance, _additionalEffects[index].duration);
            
        }

        public void PlayAccessoryEffect()
        {
            if (_accessory)
                _accessory.PlayEffect();
        }

        public void StopAccessoryEffect()
        {
            if (_accessory)
                _accessory.StopEffect();
        }

        private void Update()
        {
            _deathSparks.gameObject.SetActive(Mathf.Ceil(_health.Health) == _health.MaxHealth.Value);
        }
    }
}
