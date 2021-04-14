using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private WN_Blaster blaster;

        // Start is called before the first frame update
        void Start()
        {
            blaster = new WN_Blaster();
            blaster.Init(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            if (Keyboard.current[Key.Space].wasPressedThisFrame)
            {
                blaster.Activate();
            }
        }
    }
}


