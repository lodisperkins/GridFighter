using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Input;
using UnityEngine.UIElements;
using Lodis.Utility;
using Lodis.UI;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(fileName = "InputProfileData", menuName = "InputProfileData")]
    public class InputProfileData : ScriptableObject
    {
        [SerializeField]
        private RebindData[] _val;
        [SerializeField]
        private InputDeviceData _deviceData;

        public RebindData[] Value
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

        public string DeviceName { get => DeviceData[0].name; }
        public InputDeviceData DeviceData { get => _deviceData; set => _deviceData = value; }

        public void Init(RebindData[] value)
        {
            _val = value;
        }

        public static InputProfileData CreateInstance(RebindData[] value)
        {
            var data = CreateInstance<InputProfileData>();
            data.Init(value);
            return data;
        }

        public static bool operator ==(InputProfileData lhs, RebindData[] rhs)
        {
            return lhs.Value == rhs;
        }

        public static bool operator !=(InputProfileData lhs, RebindData[] rhs)
        {
            return lhs.Value != rhs;
        }

        public static implicit operator RebindData[](InputProfileData lhs)
        {
            return lhs.Value;
        }

        public RebindData this[int index]
        {
            get => _val[index];
            set => _val[index] = value;
        }

        public void SetBinding(BindingType bindingType, string path, string displayName)
        {
            RebindData currentData = GetBinding(bindingType);

            if (currentData == null)
                return;

            currentData.Path = path;
            currentData.DisplayName = displayName;
        }

        public RebindData GetBinding(BindingType bindingType)
        {
            return _val.FindValue<RebindData>(data => data.Binding == bindingType);
        }

        public void ClearBindings()
        {
            for (int i = 0; i < _val.Length; i++)
            {
                _val[i].DisplayName = "";
            }
        }
    }
}