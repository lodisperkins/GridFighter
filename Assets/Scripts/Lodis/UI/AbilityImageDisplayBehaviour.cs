using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis
{
    public class AbilityImageDisplayBehaviour : MonoBehaviour
    {
        public Gameplay.MovesetBehaviour MoveSet;
        public Image AbilitySlot1Image;
        public Image AbilitySlot2Image;

        private void Start()
        {
            if (MoveSet)
                UpdateImages();
        }

        public void UpdateImages()
        {
            if (!MoveSet || MoveSet.SpecialAbilitySlots.Length <= 0)
                return;

            Ability ability1 = MoveSet.GetAbilityInCurrentSlot(0);
            Ability ability2 = MoveSet.GetAbilityInCurrentSlot(1);

            if (ability1 != null)
                AbilitySlot1Image.sprite = ability1.abilityData.DisplayIcon;
            if (ability2 != null)
                AbilitySlot2Image.sprite = ability2.abilityData.DisplayIcon;
        }
    }
}
