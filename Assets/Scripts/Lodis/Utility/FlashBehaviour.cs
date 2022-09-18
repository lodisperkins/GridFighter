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

        public float EmissionStrength { get => _emissionStrength; set => _emissionStrength = value; }
        public float TimeBetweenFlashes { get => _timeBetweenFlashes; set => _timeBetweenFlashes = value; }

        // Start is called before the first frame update
        void Start()
        {
            _mesh = GetComponent<MeshRenderer>();
            Flash();
        }

        public static void Flash(Renderer renderer, Color color, float timeBetweenFlashes, int loopAmount, bool useEmission = false, float emissionStrength = 0)
        {
            Color defaultColor;

            if (useEmission)
            {
                float defaultStrength = renderer.material.GetFloat("_EmissionStrength");
                renderer.materials[0].DOFloat(emissionStrength, "_EmissionStrength", timeBetweenFlashes).SetLoops(loopAmount).onKill +=
                    () => renderer.material.SetFloat("_EmissionStrength", defaultStrength);

                defaultColor = renderer.material.GetColor("_EmissionColor");
                renderer.materials[0].DOVector(color, "_EmissionColor", timeBetweenFlashes).SetLoops(loopAmount).onKill +=
                    () => renderer.material.SetColor("_EmissionColor", defaultColor);
                return;
            }

            defaultColor = renderer.material.GetColor("_Color");
            renderer.materials[0].DOVector(color, "_Color", timeBetweenFlashes).SetLoops(loopAmount).onKill +=
                    () => renderer.material.SetColor("_Color", defaultColor); ;
        }

        public static void Flash(ColorObject colorObject, Color color, float timeBetweenFlashes, int loopAmount, bool useEmission = false, float emissionStrength = 0)
        {
            Color defaultColor;

            if (useEmission)
            {
                float defaultStrength = colorObject.ObjectRenderer.material.GetFloat("_EmissionStrength");
                colorObject.ObjectRenderer.materials[0].DOFloat(emissionStrength, "_EmissionStrength", timeBetweenFlashes).SetLoops(loopAmount).onKill +=
                    () => colorObject.ObjectRenderer.material.SetFloat("_EmissionStrength", defaultStrength);

                defaultColor = colorObject.ObjectRenderer.material.GetColor("_EmissionColor");
                colorObject.ObjectRenderer.materials[0].DOVector(color, "_EmissionColor", timeBetweenFlashes).SetLoops(loopAmount).onKill +=
                    () => colorObject.ObjectRenderer.material.SetColor("_EmissionColor", defaultColor);
                return;
            }

            defaultColor = colorObject.ObjectRenderer.material.GetColor("_Color");
            colorObject.ObjectRenderer.materials[0].DOVector(color, "_Color", timeBetweenFlashes).SetLoops(loopAmount).onKill +=
                    () => colorObject.ObjectRenderer.material.SetColor("_Color", defaultColor);
        }

        public void Flash(Renderer renderer, Color color)
        {
            Color defaultColor;

            if (_useEmission)
            {
                float defaultStrength = renderer.material.GetFloat("_EmissionStrength");
                renderer.materials[0].DOFloat(EmissionStrength, "_EmissionStrength", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => renderer.material.SetFloat("_EmissionStrength", defaultStrength);

                defaultColor = renderer.material.GetColor("_EmissionColor");
                renderer.materials[0].DOVector(color, "_EmissionColor", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => renderer.material.SetColor("_EmissionColor", defaultColor);

                return;
            }

            defaultColor = renderer.material.GetColor("_Color");
            renderer.materials[0].DOVector(color, "_Color", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => renderer.material.SetColor("_Color", defaultColor);
        }

        public void Flash(ColorObject colorObject, Color color)
        {
            Color defaultColor;

            if (_useEmission)
            {
                float defaultStrength = colorObject.ObjectRenderer.material.GetFloat("_EmissionStrength");
                colorObject.ObjectRenderer.materials[0].DOFloat(EmissionStrength, "_EmissionStrength", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => colorObject.ObjectRenderer.material.SetFloat("_EmissionStrength", defaultStrength);

                defaultColor = colorObject.ObjectRenderer.material.GetColor("_EmissionColor");
                colorObject.ObjectRenderer.materials[0].DOVector(color, "_EmissionColor", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => colorObject.ObjectRenderer.material.SetColor("_EmissionColor", defaultColor);

                return;
            }

            defaultColor = colorObject.ObjectRenderer.material.GetColor("_Color");
            colorObject.ObjectRenderer.materials[0].DOVector(color, "_Color", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => colorObject.ObjectRenderer.material.SetColor("_Color", defaultColor);
        }

        public void Flash(Color color, float timeBetweenFlashes, int loopAmount, bool useEmission = false, float emissionStrength = 0)
        {
            Color defaultColor;

            if (_useEmission)
            {
                float defaultStrength = _mesh.material.GetFloat("_EmissionStrength");
                _mesh.materials[0].DOFloat(EmissionStrength, "_EmissionStrength", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => _mesh.material.SetFloat("_EmissionStrength", defaultStrength);

                defaultColor = _mesh.material.GetColor("_EmissionColor");
                _mesh.materials[0].DOVector(color, "_EmissionColor", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => _mesh.material.SetColor("_EmissionColor", defaultColor);

                return;
            }

            defaultColor = _mesh.material.GetColor("_Color");
            _mesh.materials[0].DOVector(color, "_Color", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => _mesh.material.SetColor("_Color", defaultColor);
        }

        public void Flash()
        {
            Color defaultColor;

            if (_useEmission)
            {
                float defaultStrength = _mesh.material.GetFloat("_EmissionStrength");
                _mesh.materials[0].DOFloat(EmissionStrength, "_EmissionStrength", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => _mesh.material.SetFloat("_EmissionStrength", defaultStrength);

                defaultColor = _mesh.material.GetColor("_EmissionColor");
                _mesh.materials[0].DOVector(_color, "_EmissionColor", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => _mesh.material.SetColor("_EmissionColor", defaultColor);

                return;
            }

            defaultColor = _mesh.material.GetColor("_Color");
            _mesh.materials[0].DOVector(_color, "_Color", TimeBetweenFlashes).SetLoops(_loopAmount).onKill +=
                    () => _mesh.material.SetColor("_Color", defaultColor);
        }

        public void StopFlash()
        {
            _mesh.materials[0].DOKill(true);
        }
    }
}
