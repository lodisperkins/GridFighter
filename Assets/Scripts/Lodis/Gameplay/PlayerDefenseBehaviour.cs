using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class PlayerDefenseBehaviour : MonoBehaviour
    {
        [SerializeField]
        private IDamagable _damageScript;

        // Start is called before the first frame update
        void Start()
        {
            _damageScript = GetComponent<IDamagable>();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
