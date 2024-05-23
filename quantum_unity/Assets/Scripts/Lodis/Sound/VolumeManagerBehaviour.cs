using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Lodis.Sound
{
    public class VolumeManagerBehaviour : MonoBehaviour
    {
        enum SelectedSlider
        {
            SFX,
            VOICE,
            ANNOUNCER,
            NONE
        }

        [SerializeField]
        private AudioMixer _masterMixer;

        [SerializeField]
        private EventSliderBehaviour _musicSlider;
        [SerializeField]
        private Text _musicPercentage;
        [SerializeField]
        private EventSliderBehaviour _sfxSlider;
        [SerializeField]
        private Text _sfxPercentage;
        [SerializeField]
        private EventSliderBehaviour _voiceSlider;
        [SerializeField]
        private Text _voicePercentage;
        [SerializeField]
        private EventSliderBehaviour _announcerSlider;
        [SerializeField]
        private Text _announcerPercentage;

        [SerializeField]
        private VoicePackData _testVoices;
        [SerializeField]
        private AudioClip[] _announcerTestClips;

        private string _saveLoadPath;
        private PlayerControls _controls;
        private SelectedSlider _selectedSlider;

        private void Awake()
        {
            _controls = new PlayerControls();
            _controls.UI.Submit.performed += context => PlayTestSound();
        }

        public void InitializeSettings()
        {

            _musicSlider.Init();
            _sfxSlider.Init();
            _voiceSlider.Init();
            _announcerSlider.Init();


            _saveLoadPath = Application.persistentDataPath + "/musicSettings.txt";
            LoadValues();

            SetMusicVolume(_musicSlider.value);
            SetSFXVolume(_sfxSlider.value);
            SetVoiceVolume(_voiceSlider.value);
            SetAnnouncerVolume(_announcerSlider.value);
        }

        private void Start()
        {
            _musicSlider.onValueChanged.AddListener(SetMusicVolume);
            _musicSlider.AddOnSelectEvent(() => _selectedSlider = SelectedSlider.NONE);

            _sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            _sfxSlider.AddOnSelectEvent(() => _selectedSlider = SelectedSlider.SFX);

            _voiceSlider.onValueChanged.AddListener(SetVoiceVolume);
            _voiceSlider.AddOnSelectEvent(() => _selectedSlider = SelectedSlider.VOICE);

            _announcerSlider.onValueChanged.AddListener(SetAnnouncerVolume);
            _announcerSlider.AddOnSelectEvent(() => _selectedSlider = SelectedSlider.ANNOUNCER);
        }

        public void OnEnable()
        {
            _controls.Enable();
            _musicSlider.OnSelect();
        }

        public void OnDisable() 
        { 
            _controls.Disable();
        }

        private void PlayTestSound()
        {
            switch (_selectedSlider)
            {
                case SelectedSlider.SFX:
                    PlaySFXTest();
                    break;
                case SelectedSlider.VOICE:
                    PlayVoiceTest();
                    break;
                case SelectedSlider.ANNOUNCER:
                    PlayAnnouncerTest();
                    break;
            }
        }

        public void PlaySFXTest()
        {
            SoundManagerBehaviour.Instance.PlayHitSound(Random.Range(1, 3));
        }

        public void PlayVoiceTest()
        {
            SoundManagerBehaviour.Instance.PlayVoiceSound(_testVoices.GetRandomLightAttackClip());
        }

        public void PlayAnnouncerTest()
        {
            int index = Random.Range(0, _announcerTestClips.Length);

            SoundManagerBehaviour.Instance.PlayerAnnouncerSound(_announcerTestClips[index]);
        }

        public void SetSelectedSliderToNone()
        {
            _selectedSlider = SelectedSlider.NONE;
        }

        public void SetMusicVolume(float value)
        {
            _masterMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
            _musicPercentage.text = Mathf.RoundToInt(value * 100).ToString() + "%";
        }

        public void SetSFXVolume(float value)
        {
            _masterMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
            _sfxPercentage.text = Mathf.RoundToInt(value * 100).ToString() + "%";
        }

        public void SetVoiceVolume(float value)
        {
            _masterMixer.SetFloat("VoicesVolume", Mathf.Log10(value) * 20);
            _voicePercentage.text = Mathf.RoundToInt(value * 100).ToString() + "%";
        }

        public void SetAnnouncerVolume(float value)
        {
            _masterMixer.SetFloat("AnnouncerVolume", Mathf.Log10(value) * 20);
            _announcerPercentage.text = Mathf.RoundToInt(value * 100).ToString() + "%";
        }

        public void SaveValues()
        {

            StreamWriter writer = new StreamWriter(_saveLoadPath);

            writer.WriteLine(_musicSlider.value);
            writer.WriteLine(_sfxSlider.value);
            writer.WriteLine(_voiceSlider.value);
            writer.WriteLine(_announcerSlider.value);

            writer.Close();
        }

        public void LoadValues()
        {
            if (!File.Exists(_saveLoadPath))
                return;

            StreamReader reader = new StreamReader(_saveLoadPath);

            float musicVal = 0;
            float.TryParse(reader.ReadLine(), out musicVal);

            _musicSlider.UISlider.SetValueWithoutNotify(musicVal);

            float sfxVal = 0;
            float.TryParse(reader.ReadLine(), out sfxVal);

            _sfxSlider.UISlider.SetValueWithoutNotify(sfxVal);

            float voiceVal = 0;
            float.TryParse(reader.ReadLine(), out voiceVal);

            _voiceSlider.UISlider.SetValueWithoutNotify(voiceVal);

            float announcerVal = 0;
            float.TryParse(reader.ReadLine(), out announcerVal);

            _announcerSlider.UISlider.SetValueWithoutNotify(announcerVal);

            reader.Close();
        }

        public void ResetValues()
        {
            _musicSlider.value = 1;
            _sfxSlider.value = 1;
            _voiceSlider.value = 1;
            _announcerSlider.value = 1;
        }
    }
}