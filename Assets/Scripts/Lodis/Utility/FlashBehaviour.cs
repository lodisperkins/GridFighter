using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lodis.Gameplay;

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
            Flash();
        }

        public static void Flash(Renderer renderer, Color color, float timeBetweenFlashes, int loopAmount, bool useEmission = false, float emissionStrength = 0)
        {
            if (useEmission)
            {
                renderer.materials[0].DOFloat(emissionStrength, "_EmissionStrength", timeBetweenFlashes).SetLoops(loopAmount);
                renderer.materials[0].DOVector(color, "_EmissionColor", timeBetweenFlashes).SetLoops(loopAmount);
                return;
            }

            renderer.materials[0].DOVector(color, "_Color", timeBetweenFlashes).SetLoops(loopAmount);
        }

        public static void Flash(ColorObject colorObject, Color color, float timeBetweenFlashes, int loopAmount, bool useEmission = false, float emissionStrength = 0)
        {
            if (useEmission)
            {
                colorObject.ObjectRenderer.materials[0].DOFloat(emissionStrength, "_EmissionStrength", timeBetweenFlashes).SetLoops(loopAmount);
                colorObject.ObjectRenderer.materials[0].DOVector(color, "_EmissionColor", timeBetweenFlashes).SetLoops(loopAmount);
                return;
            }

            colorObject.ObjectRenderer.materials[0].DOVector(color, "_Color", timeBetweenFlashes).SetLoops(loopAmount);
        }

        public void Flash(Renderer renderer, Color color)
        {
            if (_useEmission)
            {
                renderer.materials[0].DOFloat(_emissionStrength, "_EmissionStrength", _timeBetweenFlashes).SetLoops(_loopAmount);
                renderer.materials[0].DOVector(color, "_EmissionColor", _timeBetweenFlashes).SetLoops(_loopAmount);
                return;
            }

            renderer.materials[0].DOVector(color, "_Color", _timeBetweenFlashes).SetLoops(_loopAmount);
        }

        public void Flash(ColorObject colorObject, Color color)
        {
            if (_useEmission)
            {
                colorObject.ObjectRenderer.materials[0].DOFloat(_emissionStrength, "_EmissionStrength", _timeBetweenFlashes).SetLoops(_loopAmount);
                colorObject.ObjectRenderer.materials[0].DOVector(color, "_EmissionColor", _timeBetweenFlashes).SetLoops(_loopAmount);
                return;
            }

            colorObject.ObjectRenderer.materials[0].DOVector(color, "_Color", _timeBetweenFlashes).SetLoops(_loopAmount);
        }

        public void Flash(Color color, float timeBetweenFlashes, int loopAmount, bool useEmission = false, float emissionStrength = 0)
        {
            if (useEmission)
            {
                _mesh.materials[0].DOFloat(emissionStrength, "_EmissionStrength", timeBetweenFlashes).SetLoops(loopAmount);
                _mesh.materials[0].DOVector(color, "_EmissionColor", timeBetweenFlashes).SetLoops(loopAmount);
                return;
            }

            _mesh.materials[0].DOVector(color, "_Color", timeBetweenFlashes).SetLoops(loopAmount);
        }

        public void Flash()
        {
            if (_useEmission)
            {
                _mesh.materials[0].DOFloat(_emissionStrength, "_EmissionStrength", _timeBetweenFlashes).SetLoops(_loopAmount);
                _mesh.materials[0].DOVector(_color, "_EmissionColor", _timeBetweenFlashes).SetLoops(_loopAmount);
                return;
            }

            _mesh.materials[0].DOVector(_color, "_Color", _timeBetweenFlashes).SetLoops(_loopAmount);
        }

        public void StopFlash()
        {
            _mesh.materials[0].DOKill(true);
        }
    }
}
