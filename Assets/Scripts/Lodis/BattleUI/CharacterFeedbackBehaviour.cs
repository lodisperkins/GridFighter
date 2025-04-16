using System.Collections;
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
        [Header("Color Options")]
        [SerializeField] private Color _invincibleColor;
        [SerializeField] private Color _intangibleColor;
        [SerializeField] private ColorManagerBehaviour _colorManager;

        [Header("UI Options")]
        [SerializeField] private HealthBehaviour _health;
        [SerializeField] private GameObject _characterUI;

        [Header("Particle References")]
        [SerializeField] private ParticleSystem _stunParticles;
        [SerializeField] private ParticleSystem _stunSparks;
        [SerializeField] private ParticleSystem _deathSparks;
        [SerializeField] private ParticleSystem[] _additionalEffects;
        [SerializeField] private ParticleSystem[] _comboTrails;
        [SerializeField] private ParticleSystem _spawnEffect;

        [Header("Audio Options")]
        [SerializeField] private AudioClip _spawnSound;
        [SerializeField] private CharacterVoiceBehaviour _characterVoice;

        [Header("Character Extras")]
        [SerializeField] private AccessoryEffectBehaviour _accessory;

        //---
        private MovesetBehaviour _moveSet;
        private GridMovementBehaviour _movement;
        private CharacterStateMachineBehaviour _characterStateMachine;
        private int lastComboTrailIndex;
        private bool _comboTrailEnabled;
        private ShakeBehaviour _shakeBehaviour;

        public ColorManagerBehaviour ColorManager { get => _colorManager; private set => _colorManager = value; }

        void Start()
        {
            _health.AddOnInvincibilityActiveAction(() => FlashAllRenderers(_invincibleColor));
            //_health.AddOnIntangibilityActiveAction(() => FlashAllRenderers(_intangibleColor));
            _health.AddOnInvincibilityInactiveAction(ResetAllRenderers);
            //_health.AddOnIntangibilityInactiveAction(ResetAllRenderers);

            _health.AddOnStunAction(() => PlayStunParticles(true));
            _health.AddOnStunDisabledAction(() => PlayStunParticles(false));

            KnockbackBehaviour knockback = _health as KnockbackBehaviour;
            if (knockback)
            {
                knockback.AddOnKnockBackAction(SetComboTrail);
                knockback.LandingScript.AddOnLandingStartAction(DisableComboTrail);
            }

            _moveSet = GetComponentInParent<MovesetBehaviour>();
            _movement = GetComponentInParent<GridMovementBehaviour>();
            _shakeBehaviour = GetComponent<ShakeBehaviour>();
            _characterStateMachine = GetComponentInParent<CharacterStateMachineBehaviour>();

            if (SceneManagerBehaviour.Instance.SceneIndex == 4)
            {
                MatchManagerBehaviour.Instance.AddOnMatchCountdownStartAction(() =>
                {
                    PlaySpawnEffect();
                });
                PlaySpawnEffect();
            }

            MatchManagerBehaviour.Instance.AddOnRingoutAction(DisableComboTrail);
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(DisableComboTrail);
        }

        private void PlayStunParticles(bool active)
        {
            _stunParticles.gameObject.SetActive(active);
            _stunSparks.gameObject.SetActive(active);
        }

        private void SetComboTrail()
        {
            int level = BlackBoardBehaviour.Instance.GetComboLevelForOpponent(_movement.Alignment);

            _comboTrails[lastComboTrailIndex].gameObject.SetActive(false);
            _comboTrails[level].gameObject.SetActive(true);

            lastComboTrailIndex = level;
            _comboTrailEnabled = true;
        }

        private void DisableComboTrail()
        {
            _comboTrails[lastComboTrailIndex].gameObject.SetActive(false);
            _comboTrailEnabled = false;
        }

        public void PlaySpawnEffect()
        {
            Instantiate(_spawnEffect, (Vector3)_movement.CurrentPanel.FixedWorldPosition, Camera.main.transform.rotation);
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
                if (colorObject.ObjectRenderer)
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

        public void ShakeCharacter(float time, float strength, int frequency)
        {
            _shakeBehaviour.ShakePosition(time, strength, frequency);

            KnockbackBehaviour knockback = _health as KnockbackBehaviour;

            if (knockback)
            {
                knockback.AddOnKnockBackStartTempAction(_shakeBehaviour.StopShaking);
            }
        }

        public void ShakeCharacter()
        {
            _shakeBehaviour.ShakePosition();

            KnockbackBehaviour knockback = _health as KnockbackBehaviour;

            if (knockback)
            {
                knockback.AddOnKnockBackStartTempAction(_shakeBehaviour.StopShaking);
            }
        }

        private void Update()
        {
            _deathSparks.gameObject.SetActive(Mathf.Ceil(_health.Health) == _health.MaxHealth.FixedValue);

            if (_characterStateMachine.CurrentState != "Tumbling" && _comboTrailEnabled)
            {
                DisableComboTrail();
            }
        }
    }
}
