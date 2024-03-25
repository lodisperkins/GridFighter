using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Sound
{
    public class SoundManagerBehaviour : MonoBehaviour
    {
        private static SoundManagerBehaviour _instance;
        [SerializeField] private AudioSource _soundEffectSource;
        [SerializeField] private AudioSource _voiceSource;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _announcer;
        [SerializeField] private AudioClip[] _hitSounds;
        [SerializeField] private AudioClip _clashSound;
        private AudioClip _lastClip;
        private bool _canPlaySameSFX;
        private TimedAction _enableSameSFXAction;
        [SerializeField] private float _sameSoundDelay = 0.0001f;

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

                    _instance._musicSource = new GameObject("MusicSource").AddComponent<AudioSource>();
                    _instance._musicSource.transform.SetParent(_instance.transform);

                    _instance._soundEffectSource = new GameObject("SoundEffectSource").AddComponent<AudioSource>();
                    _instance._soundEffectSource.transform.SetParent(_instance.transform);

                    _instance._voiceSource = new GameObject("VoiceSource").AddComponent<AudioSource>();
                    _instance._voiceSource.transform.SetParent(_instance.transform);

                    _instance._announcer = new GameObject("AnnouncerSource").AddComponent<AudioSource>();
                    _instance._announcer.transform.SetParent(_instance.transform);
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

        public void StopSound(AudioClip clip)
        {
            return;
            if (!_soundEffectSource.isPlaying || clip != _lastClip)
                return;

            _soundEffectSource.Stop();
        }

        public void PlaySound(AudioClip clip, float volumeScale)
        {
            if (!clip)
                return;
            if (_soundEffectSource.isPlaying && !_canPlaySameSFX && _lastClip == clip)
                return;

            _lastClip = clip;
            _soundEffectSource.clip = clip;
            _soundEffectSource.PlayOneShot(clip, volumeScale);
            _canPlaySameSFX = false;

            RoutineBehaviour.Instance.StopAction(_enableSameSFXAction);
            _enableSameSFXAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _canPlaySameSFX = true, TimedActionCountType.SCALEDTIME, _sameSoundDelay);
        }
        
        public void PlaySound(AudioClip clip)
        {
            if (!clip)
                return;
            if (_soundEffectSource.isPlaying && !_canPlaySameSFX && _lastClip == clip)
                return;

            _lastClip = clip;
            _soundEffectSource.PlayOneShot(clip);
            _canPlaySameSFX = false;

            RoutineBehaviour.Instance.StopAction(_enableSameSFXAction);
            _enableSameSFXAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _canPlaySameSFX = true, TimedActionCountType.SCALEDTIME, _sameSoundDelay);
        }

        public void PlayVoiceSound(AudioClip clip)
        {
            _voiceSource.PlayOneShot(clip);
        }

        public void PlayerAnnouncerSound(AudioClip voiceClip)
        {
            if (_announcer.isPlaying)
                _announcer.Stop();

            _announcer.PlayOneShot(voiceClip);
        }

        /// <summary>
        /// Plays one of the default hit sound effects.
        /// </summary>
        /// <param name="strength">The strength of the hit. Used to determine which sound to play.
        /// 1 = Light,
        /// 2 = Medium,
        /// 3 = Heavy</param>
        public void PlayHitSound(int strength)
        {
            strength--;
            if (strength < 0 || strength > _hitSounds.Length)
                return;


            _lastClip = _hitSounds[strength];
            _soundEffectSource.PlayOneShot(_hitSounds[strength]);
        }

        public void PlayClashSound()
        {
            PlaySound(_clashSound);
        }

        public void SetMusic(AudioClip music)
        {
            if (!music)
                return;

            _musicSource.clip = music;
            _musicSource.Play();
        }

        private void Update()
        {
            if (!_soundEffectSource.isPlaying)
            {
                _lastClip = null;
                RoutineBehaviour.Instance.StopAction(_enableSameSFXAction);
                _canPlaySameSFX = true;
            }
        }
    }
}
