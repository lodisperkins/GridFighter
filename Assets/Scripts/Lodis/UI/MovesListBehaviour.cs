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

        private void Awake()
        {
            _player = BlackBoardBehaviour.Instance.GetPlayerControllerFromID(_playerID);
            MovesetBehaviour moveset = _player.Character.GetComponent<MovesetBehaviour>();
            _normalDeck = moveset.NormalDeckRef;
            _specialDeck = moveset.SpecialDeckRef;
        }

        // Start is called before the first frame update
        void Start()
        {
           foreach (MoveDescriptionBehaviour normalMove in _normalMoveSlots)
           {
                AbilityData data = _normalDeck.GetAbilityDataByType(normalMove.AbilityType);

                normalMove.Init(_description, _videoPlayer, data);
           }

           for (int i = 0; i < _specialDeck.AbilityData.Count; i++)
           {
                _specialMoveSlots[i].Init(_description, _videoPlayer, _specialDeck.AbilityData[i]);
           }

            _eventSystem = _player.Character.GetComponentInParent<MultiplayerEventSystem>();

            if (!_eventSystem)
                return;

            _eventSystem.playerRoot = gameObject;
            _eventSystem.firstSelectedGameObject = _firstSelected;
            _eventSystem.SetSelectedGameObject(_firstSelected);
            _cursor.EventSystem = _eventSystem;
        }

        private void OnEnable()
        {
            _eventSystem?.SetSelectedGameObject(_firstSelected);
        }
    }
}