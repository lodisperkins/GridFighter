using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Sound
{
    public class SoundManagerBehaviour : MonoBehaviour
    {
        private static SoundManagerBehaviour _instance;
        [SerializeField] private AudioSource _soundEffectSource;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioClip[] _hitSounds;
        private AudioClip _lastClip;

        /// <summary>
        /// Gets the static instance of the sound manager. Creates one if none exists
        /// </summary>
        public static SoundManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(SoundManagerBehaviour)) as SoundManagerBehaviour;

                if (!_instance)
                {
                    GameObject blackBoard = new GameObject("SoundManager");
                    _instance = blackBoard.AddComponent<SoundManagerBehaviour>();
                }

                return _instance;
            }
        }

        public void TogglePauseMusic()
        {
            if (_musicSource.isPlaying)
                _musicSource.Pause();
            else
                _musicSource.UnPause();
        }

        public void PlaySound(AudioClip clip)
        {
            if (!clip || (_soundEffectSource.isPlaying && _lastClip == clip))
                return;

            _lastClip = clip;
            _soundEffectSource.PlayOneShot(clip);
        }

        public void PlayHitSound(int strength)
        {
            strength--;
            if (strength < 0 || strength > _hitSounds.Length || (_soundEffectSource.isPlaying && _lastClip.name == _hitSounds[strength].name))
                return;


            _lastClip = _hitSounds[strength];
            _soundEffectSource.PlayOneShot(_hitSounds[strength]);
        }

        public void SetMusic(AudioClip music)
        {
            if (!music)
                return;

            _musicSource.clip = music;
            _musicSource.Play();
        }
    }
}
