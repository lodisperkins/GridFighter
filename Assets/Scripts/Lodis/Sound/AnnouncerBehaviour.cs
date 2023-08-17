using DG.Tweening;
using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using Lodis.UI;
using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.Sound
{
    public class AnnouncerBehaviour : MonoBehaviour
    {
        [System.Serializable]
        public class Announcement
        {
            public string AnnouncementName;
            public string Text;
            public Color TextColor;
            public AudioClip VoiceClip;
        }

        [SerializeField]
        private ComboCounterBehaviour _p1ComboCounter;
        [SerializeField]
        private Text _p1AnnouncementText;

        [SerializeField]
        private ComboCounterBehaviour _p2ComboCounter;
        [SerializeField]
        private Text _p2AnnouncementText;

        [SerializeField]
        private List<Announcement> _announcements;

        [SerializeField]
        private float _effectDuration;
        [SerializeField]
        private float _effectScale;
        private TimedAction _disableTextActionP1;
        private TimedAction _disableTextActionP2;
        [SerializeField]
        private float _messageDespawnDelay;

        private static AnnouncerBehaviour _instance;


        public static AnnouncerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(AnnouncerBehaviour)) as AnnouncerBehaviour;

                if (!_instance)
                {
                    GameObject announcer = new GameObject("Announcer");
                    _instance = announcer.AddComponent<AnnouncerBehaviour>();
                }

                return _instance;
            }
        }

        private void Start()
        {
            if (SceneManagerBehaviour.Instance.SceneIndex != 4)
                return;

            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(() =>
            {
                _p1AnnouncementText.enabled = false;
                _p2AnnouncementText.enabled = false;
            });
        }

        public void MakeAnnouncement(int playerID, string announcementName)
        {
            Announcement announcement = _announcements.Find(value => value.AnnouncementName == announcementName);
            StartSpawnEffect(playerID);

            if (playerID == 1)
            {
                _p1AnnouncementText.text = announcement.Text;
                _p1AnnouncementText.color = announcement.TextColor;
                SoundManagerBehaviour.Instance.PlayerAnnouncerSound(announcement.VoiceClip);

                RoutineBehaviour.Instance.StopAction(_disableTextActionP1);
                _disableTextActionP1 = RoutineBehaviour.Instance.StartNewTimedAction(args => DespawnMessage(playerID), TimedActionCountType.SCALEDTIME, _messageDespawnDelay);
            }
            else if (playerID == 2)
            {
                _p2AnnouncementText.text = announcement.Text;
                _p2AnnouncementText.color = announcement.TextColor;
                SoundManagerBehaviour.Instance.PlayerAnnouncerSound(announcement.VoiceClip);

                RoutineBehaviour.Instance.StopAction(_disableTextActionP2);
                _disableTextActionP2 = RoutineBehaviour.Instance.StartNewTimedAction(args => DespawnMessage(playerID), TimedActionCountType.SCALEDTIME, _messageDespawnDelay);
            }
        }

        private void DespawnMessage(int playerID)
        {
            if (playerID == 1)
                _p1AnnouncementText.enabled = false;
            else if (playerID == 2)
                _p2AnnouncementText.enabled = false;
        }

        private void StartSpawnEffect(int playerID)
        {
            if (playerID == 1)
            {
                _p1AnnouncementText.enabled = true;
                _p1AnnouncementText.rectTransform.DOComplete();
                _p1AnnouncementText.rectTransform.DOPunchScale(new Vector3(_effectScale, _effectScale, _effectScale), _effectDuration);
            }
            else if (playerID == 2)
            {
                _p2AnnouncementText.enabled = true;
                _p2AnnouncementText.rectTransform.DOComplete();
                _p2AnnouncementText.rectTransform.DOPunchScale(new Vector3(_effectScale, _effectScale, _effectScale), _effectDuration);
            }
        }
    }
}