using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.ScriptableObjects;
using Lodis.Input;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Lodis.UI
{
    public class MovesListBehaviour : MonoBehaviour
    {
        [SerializeField]
        private IntVariable _playerID;
        [SerializeField]
        private List<MoveDescriptionBehaviour> _normalMoveSlots;
        [SerializeField]
        private List<MoveDescriptionBehaviour> _specialMoveSlots;
        [SerializeField]
        private GameObject _firstSelected;
        private Deck _normalDeck;
        private Deck _specialDeck;
        private IControllable _player;
        private MultiplayerEventSystem _eventSystem;
        [SerializeField]
        private Text _description;
        [SerializeField]
        private VideoPlayer _videoPlayer;
        [SerializeField]
        private CursorLerpBehaviour _cursor;
        [SerializeField]
        private bool _manuallySetDeckRefs;

        private void Awake()
        {
            if (_manuallySetDeckRefs)
                return;

            _player = BlackBoardBehaviour.Instance.GetPlayerControllerFromID(_playerID);
            MovesetBehaviour moveset = _player.Character.GetComponent<MovesetBehaviour>();
            _normalDeck = moveset.NormalDeckRef;
            _specialDeck = moveset.SpecialDeckRef;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (!_manuallySetDeckRefs)
            {
                UpdateUI(_normalDeck, _specialDeck, _player.Character.GetComponentInParent<MultiplayerEventSystem>());
            }
        }

        public void UpdateUI(Deck normalDeckRef, Deck specialDeckRef, MultiplayerEventSystem eventSystem)
        {
            if (!eventSystem)
                return;

            _eventSystem = eventSystem;

            foreach (MoveDescriptionBehaviour normalMove in _normalMoveSlots)
            {
                AbilityData data = normalDeckRef.GetAbilityDataByType(normalMove.AbilityType);

                normalMove.Init(_description, _videoPlayer, data);
            }

            for (int i = 0; i < specialDeckRef.AbilityData.Count; i++)
            {
                _specialMoveSlots[i].Init(_description, _videoPlayer, specialDeckRef.AbilityData[i]);
            }

            _eventSystem.playerRoot = gameObject;
            _eventSystem.firstSelectedGameObject = _firstSelected;
            _eventSystem.SetSelectedGameObject(_firstSelected);
            _cursor.EventSystem = _eventSystem;
        }

        public void UpdateUI(Deck normalDeckRef, Deck specialDeckRef)
        {
            foreach (MoveDescriptionBehaviour normalMove in _normalMoveSlots)
            {
                AbilityData data = normalDeckRef.GetAbilityDataByType(normalMove.AbilityType);

                normalMove.Init(_description, _videoPlayer, data);
            }

            for (int i = 0; i < specialDeckRef.AbilityData.Count; i++)
            {
                _specialMoveSlots[i].Init(_description, _videoPlayer, specialDeckRef.AbilityData[i]);
            }
        }

        private void OnEnable()
        {
            _eventSystem?.SetSelectedGameObject(_firstSelected);
        }
    }
}