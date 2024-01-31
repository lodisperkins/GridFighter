using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Lodis.ScriptableObjects;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Variables/Input Device Array")]
    public class InputDeviceData : ScriptableObject
    {
        [SerializeField]
        private InputDevice[] _val;
        public InputDevice[] Value
        {
            get
            {
                return _val;
            }
            set
            {
                _val = value;
            }
        }

        public int Length
        {
            get
            {
                if (_val == null)
                    return 0;

                return _val.Length;
            }
        }

        public void Init(InputDevice[] value)
        {
            _val = value;
        }

        public static InputDeviceData CreateInstance(InputDevice[] value)
        {
            var data = CreateInstance<InputDeviceData>();
            data.Init(value);
            return data;
        }

        public static bool operator ==(InputDeviceData lhs, InputDevice[] rhs)
        {
            return lhs.Value == rhs;
        }

        public static bool operator !=(InputDeviceData lhs, InputDevice[] rhs)
        {
            return lhs.Value != rhs;
        }

        public static implicit operator InputDevice[](InputDeviceData lhs)
        {
            return lhs.Value;
        }

        public InputDevice this[int index]
        {
            get => _val[index];
            set => _val[index] = value;
        }
    }
}