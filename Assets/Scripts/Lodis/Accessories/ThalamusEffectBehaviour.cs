using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Gameplay;
using Lodis.Utility;

namespace Lodis.Accessories
{
    public class ThalamusEffectBehaviour : AccessoryEffectBehaviour
    {
        private RotationBehaviour _rotationScript;
        private HoverBehaviour _hoverScipt;
        private HealthBehaviour _health;
        private Quaternion _rotation;
        private Vector3 _position;
        [SerializeField]
        private AnimationCurve _moveCurve;

        private void Start()
        {
            _rotationScript = GetComponent<RotationBehaviour>();
            _hoverScipt = GetComponent<HoverBehaviour>();
            _health = Owner.GetComponentInChildren<HealthBehaviour>();

            _health.AddOnTakeDamageAction(StopEffect);

            _rotation = transform.rotation;
            _position = transform.localPosition;
            _rotationScript.enabled = false;
        }

        private void OnDisable()
        {
            StopEffect();
        }

        public override void PlayEffect()
        {
            base.PlayEffect();
            
            //_rotationScript.enabled = true;

            if (_hoverScipt)
                _hoverScipt.enabled = false;

            transform.DOMove(transform.position + (Vector3.up) + -Owner.transform.forward, 1.2f).SetEase(_moveCurve);
        }

        public override void StopEffect()
        {
            base.StopEffect();

            transform.localPosition = _position;
            transform.rotation = _rotation;
            transform.DOKill();

            //_rotationScript.enabled = false;
            if (_hoverScipt)
                _hoverScipt.enabled = true;
        }
    }
}