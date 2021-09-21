using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis
{
    public class AbilityDebugTextBehaviour : MonoBehaviour
    {
        public Gameplay.MovesetBehaviour MoveSet;
        public Text AbilitySlot1Text;
        public Text AbilitySlot2Text;

        // Update is called once per frame
        void Update()
        {
            AbilitySlot1Text.text = MoveSet.GetAbilityNamesInCurrentSlots()[0];
            AbilitySlot2Text.text = MoveSet.GetAbilityNamesInCurrentSlots()[1];
        }
    }
}
