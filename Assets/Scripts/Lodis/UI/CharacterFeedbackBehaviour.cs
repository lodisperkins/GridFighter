﻿using System.Collections;
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
            foreach (ColorObject colorObject in _colorManager.ObjectsToColor)
            {
                Flash(colorObject, color);
            }
        }

        public void ResetAllRenderers()
        {
            foreach (ColorObject colorObject in _colorManager.ObjectsToColor)
            {
                colorObject.ObjectRenderer.material.DOKill();
                colorObject.ObjectRenderer.material.color = colorObject.DefaultColor;
            }
        }
    }
}
