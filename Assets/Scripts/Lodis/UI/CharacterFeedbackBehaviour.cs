using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class CharacterFeedbackBehaviour : MonoBehaviour
    {
        private Renderer[] _renderers;

        // Start is called before the first frame update
        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
