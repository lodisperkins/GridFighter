using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GridGame;

namespace Lodis.UI
{
    public class CharacterSelectButtonBehaviour : Button, ISelectHandler
    {
        [SerializeField]
        private GridGame.Event _onSelectCharacter;

        protected override void Awake()
        {
            _onSelectCharacter = Resources.Load<GridGame.Event>("Events/OnSelectCharacter");
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            _onSelectCharacter?.Raise(gameObject);
        }
    }
}