using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.Gameplay
{
    public enum Attack
    {
        WEAKNEUTRAL,
        WEAKSIDE,
        WEAKFORWARD,
        WEAKBACKWARD,
        STRONGNEUTRAL,
        STRONGSIDE,
        STRONGFORWARD,
        STRONGBACKWARD
    }

    public class MovesetBehaviour : MonoBehaviour
    {
        private SF_ChargeForwardShot _blaster;

        // Start is called before the first frame update
        void Start()
        {
            _blaster = new SF_ChargeForwardShot();
            _blaster.Init(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (Keyboard.current[Key.Space].wasPressedThisFrame)
            {
                _blaster.UseAbility(new Vector2(0,1));
            }
            else if (Keyboard.current[Key.B].wasPressedThisFrame)
            {
                _blaster.UseAbility(new Vector2(0, -1));
            }
        }
    }
}


