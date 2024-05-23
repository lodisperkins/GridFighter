using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CustomEventSystem;

namespace Lodis.UI
{
    public class CharacterSelectButtonBehaviour : Button, ISelectHandler
    {
        [SerializeField]
        private CustomEventSystem.Event _onSelectCharacter;

        protected override void Awake()
        {
            _onSelectCharacter = Resources.Load<CustomEventSystem.Event>("Events/OnSelectCharacter");
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            _onSelectCharacter?.Raise(gameObject);
        }
    }
}