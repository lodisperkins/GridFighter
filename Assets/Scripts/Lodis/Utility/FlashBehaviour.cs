using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Lodis.Utility
{
    public class FlashBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _timeBetweenFlashes;
        [SerializeField]
        private Color _color;
        [SerializeField]
        private bool _useEmission;
        [SerializeField]
        private float _emissionStrength;
        [SerializeField]
        private int _loopAmount = -1;

        private MeshRenderer _mesh;

        // Start is called before the first frame update
        void Start()
        {
            _mesh = GetComponent<MeshRenderer>();
            if (_useEmission)
            {
                _mesh.materials[0].DOFloat(_emissionStrength, "_EmissionStrength", _timeBetweenFlashes).SetLoops(_loopAmount);
                _mesh.materials[0].DOVector(_color, "_EmissionColor", _timeBetweenFlashes).SetLoops(_loopAmount);
                return;
            }

            _mesh.materials[0].DOVector(_color, "_Color", _timeBetweenFlashes).SetLoops(_loopAmount); ;
        }
    }
}
