using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Lodis.Utility
{
    public static class Extensions
    {
        public static int GetCombinedMask(this LayerMask[] masks)
        {
            int total = 0;

            for (int i = 0; i < masks.Length; i++)
                total += masks[i];

            return total;
        }

        public static InputDevice[] GetDevices(this ReadOnlyArray<InputDevice> deviceArray, Condition condition)
        {
            List<InputDevice> devices = new List<InputDevice>();

            for (int i = 0; i < deviceArray.Count; i++)
            {
                if (condition.Invoke(deviceArray[i]))
                {
                    devices.Add(deviceArray[i]);
                }
            }
            return devices.ToArray();
        }
        
        public static bool Contains(this System.Array array, object item)
        {
            for (int i = 0; i < array.Length; i++)
                if (array.GetValue(i) == item) return true;

            return false;
        }

        public static bool IsFilled(this UnityEngine.UI.Slider slider)
        {
            return slider.value == slider.maxValue;
        }
    }
}
