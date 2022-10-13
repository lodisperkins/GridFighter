using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Sound;

namespace Lodis.Gameplay
{

    public class ChargeEffectBehaviour : MonoBehaviour
    {
        [SerializeField]
        private AudioClip _chargeSound;
        [SerializeField]
        [Range(0f, 1f)]
        private float _volumeScale;
        
        public void StartChargeEffect()
        {
            gameObject.SetActive(true);
            SoundManagerBehaviour.Instance.PlaySound(_chargeSound, _volumeScale);
        }

        public void StopChargeEffect()
        {
            gameObject.SetActive(false);
            SoundManagerBehaviour.Instance.StopSound(_chargeSound);
        }
    }
}