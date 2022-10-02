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

        public void PlaySound(AudioClip clip)
        {
            if (!clip)
                return;

            _soundEffectSource.PlayOneShot(clip);
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
