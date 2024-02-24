using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Lodis.Input
{
    public class InputRebindingBehaviour : MonoBehaviour
    {
        private InputAction _anyAction;
        private Dictionary<string, string> _bindings;
        [SerializeField]
        private string _currentKey;
        [SerializeField]
        private bool _isListening;

        private void Awake()
        {
            _anyAction = new InputAction(binding: "/*/<button>");
            _anyAction.performed += StoreBinding;
            _anyAction.Enable();

            _bindings = new Dictionary<string, string>();
            InitDictionary();
        }

        private void InitDictionary()
        {
            _bindings.Add("Move Up", "default");
            _bindings.Add("Move Down", "default");
            _bindings.Add("Move Left", "default");
            _bindings.Add("Move Right", "default");
            _bindings.Add("Weak Attack", "default");
            _bindings.Add("Strong Attack", "default");
            _bindings.Add("Special1", "default");
            _bindings.Add("Special2", "default");
            _bindings.Add("Burst", "default");
            _bindings.Add("Shuffle", "default");
        }

        public void SetCurrentKey(string key)
        {
            _currentKey = key;
        }

        public void SetIsListening(bool isListening)
        {
            _isListening = isListening;
        }

        private void StoreBinding(InputAction.CallbackContext context)
        {
            if (SceneManagerBehaviour.Instance.P1Devices.Length == 0)
                return;

            if (context.control.device != SceneManagerBehaviour.Instance.P1Devices[0] || !_isListening)
                return;

            InputBinding? binding = context.action.GetBindingForControl(context.control);

            if (binding == null)
                return;

            _bindings[_currentKey] = binding.Value.effectivePath;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}