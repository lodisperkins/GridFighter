using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class TextFlashBehaviour : MonoBehaviour
    {
        private Color _baseColor;
        [SerializeField]
        private Color _flashColor;
        [SerializeField]
        private float _flashActiveTime;
        [SerializeField]
        private float _flashInactiveTime;
        [SerializeField]
        private bool _flashOnStart;
        [SerializeField]
        private bool _flashOnEnable;
        [SerializeField]
        private bool _flashActive;
        private Text _text;

        public bool FlashActive { get => _flashActive; private set => _flashActive = value; }
        public Color BaseColor { get => _baseColor; set => _baseColor = value; }

        // Start is called before the first frame update
        void Awake()
        {
            _text = GetComponent<Text>();
            BaseColor = _text.color;
        }

        private void Start()
        {
            if (_flashOnStart)
                StartFlash();
        }

        private void OnEnable()
        {
            if (_flashOnEnable)
                StartFlash();
        }

        public void StartFlash()
        {
            _flashActive = true;
            StartCoroutine(FlashRoutine());
        }

        public void StopFlash()
        {
            _flashActive = false;
            StopAllCoroutines();
        }

        private IEnumerator FlashRoutine()
        {
            while (_flashActive)
            {
                _text.color = _flashColor;
                yield return new WaitForSeconds(_flashActiveTime);
                _text.color = BaseColor;
                yield return new WaitForSeconds(_flashInactiveTime);
            }
        }
    }
}