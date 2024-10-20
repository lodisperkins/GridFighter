﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using UnityEngine.Events;
using DG.Tweening;
using CustomEventSystem;
using Lodis.Movement;
using Lodis.Accessories;
using Lodis.Sound;

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
        [SerializeField] private ParticleSystem _spawnEffect;
        [SerializeField] private AudioClip _spawnSound;
        [SerializeField] private CharacterVoiceBehaviour _characterVoice;
        [SerializeField] private GameObject _characterUI;

        public ColorManagerBehaviour ColorManager { get => _colorManager; private set => _colorManager = value; }

        void Start()
        {
            _health.AddOnInvincibilityActiveAction(() => FlashAllRenderers(_invincibleColor));
            //_health.AddOnIntangibilityActiveAction(() => FlashAllRenderers(_intangibleColor));

            _health.AddOnInvincibilityInactiveAction(ResetAllRenderers);
            //_health.AddOnIntangibilityInactiveAction(ResetAllRenderers);

            _health.AddOnStunAction(() => _stunParticles.gameObject.SetActive(true));
            _health.AddOnStunDisabledAction(() => _stunParticles.gameObject.SetActive(false));
            _moveSet = GetComponentInParent<MovesetBehaviour>();
            _movement = GetComponentInParent<GridMovementBehaviour>();

            if (SceneManagerBehaviour.Instance.SceneIndex == 4)
            {
                MatchManagerBehaviour.Instance.AddOnMatchCountdownStartAction(() =>
                {
                    PlaySpawnEffect();
                });
                PlaySpawnEffect();
            }
        }

        public void PlaySpawnEffect()
        {
            Instantiate(_spawnEffect, transform.position, Camera.main.transform.rotation);
            SoundManagerBehaviour.Instance.PlaySound(_spawnSound);
            RoutineBehaviour.Instance.StartNewTimedAction(args => _characterVoice.PlaySpawnSound(), TimedActionCountType.SCALEDTIME, 0.1f);
        }

        public void FlashAllRenderers(Color color)
        {
            foreach (ColorObject colorObject in ColorManager.ObjectsToColor)
            {
                Flash(colorObject, color);
            }
        }

        public void SetCharacterUIEnabled(bool value)
        {
            _characterUI.SetActive(value);
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

        public void PlaySoundFromEvent(EventArguments args)
        {
            for (int i = 0; i < args.UnityObjectArgs.Length; i++)
            {
                SoundManagerBehaviour.Instance.PlaySound(args.UnityObjectArgs[i] as AudioClip);
            }
        }

        public void PlayVoiceSound(int clipType)
        {
            switch (clipType)
            {
                case 0:
                    _characterVoice.PlayLightAttackSound();
                    break;
                case 1:
                    _characterVoice.PlayHeavyAttackSound();
                    break;
                case 3:
                    _characterVoice.PlayHurtSound();
                    break;
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
