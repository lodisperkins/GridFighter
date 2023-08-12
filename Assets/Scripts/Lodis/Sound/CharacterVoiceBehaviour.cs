using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Sound
{
    public class CharacterVoiceBehaviour : MonoBehaviour
    {
        [SerializeField]
        private VoicePackData _voicePack;
        [SerializeField]
        private AudioSource _source;
        [SerializeField]
        private KnockbackBehaviour _knockback;

        private void Start()
        {
            _knockback = GetComponentInParent<KnockbackBehaviour>();
            _knockback?.AddOnTakeDamageAction(PlayHurtSound);
        }

        public void PlayHurtSound()
        {
            _source.Stop();
            AudioClip clip = _voicePack.GetRandomHurtClip();

            if (clip)
                _source.PlayOneShot(clip);
        }

        public void PlayLightAttackSound()
        {
            _source.Stop();
            AudioClip clip = _voicePack.GetRandomLightAttackClip();

            if (clip)
                _source.PlayOneShot(clip);
        }

        public void PlayHeavyAttackSound()
        {
            _source.Stop();
            AudioClip clip = _voicePack.GetRandomHeavyAttackClip();

            if (clip)
                _source.PlayOneShot(clip);
        }

        public void PlayDeathSound()
        {
            _source.Stop();
            _source.PlayOneShot(_voicePack.Death);
        }

        public void PlayVoiceSound(AudioClip clip)
        {
            _source.Stop();
            _source.PlayOneShot(clip);
        }

        public void PlayBurstSound()
        {
            _source.Stop();
            _source.PlayOneShot(_voicePack.Burst);
        }

        public void PlaySpawnSound()
        {
            _source.Stop();
            _source.PlayOneShot(_voicePack.Spawn);
        }
    }
}