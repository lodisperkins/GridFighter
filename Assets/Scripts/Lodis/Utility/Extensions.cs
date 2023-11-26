using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

namespace Lodis.Utility
{
    public static class Extensions
    {
        public delegate bool ArrCondition<T>(T item);

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
            {
                var value = array.GetValue(i);
                if (value != item)
                    continue;

                return true;
            }

            return false;
        }


        public static bool Contains<T>(this System.Array array, ArrCondition<T> condition)
        {
            for (int i = 0; i < array.Length; i++)
            {
                T value = (T)array.GetValue(i);
                if (condition(value))
                    return true;
            }

            return false;
        }

        public static bool IsFilled(this UnityEngine.UI.Slider slider)
        {
            return slider.value == slider.maxValue;
        }

        public static bool GetIsPrefab(this GameObject gameObject)
        {
            return gameObject.scene.rootCount == 0;
        }

        public static void ChangeHue(this Material material, Color newColor, string property)
        {
            Color propertyColor = new Color();
            Vector3 propertyHSV = new Vector3();
            Vector3 targetHSV = new Vector3();
            
            propertyColor = material.GetColor(property);
            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(newColor, out targetHSV.x, out targetHSV.y, out targetHSV.z);

            propertyHSV.x = targetHSV.x;

            newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

            material.SetColor(property, newColor);
        }

        public static void ChangeHue(this Image image, Color newColor)
        {
            Color propertyColor = new Color();
            Vector3 propertyHSV = new Vector3();
            Vector3 targetHSV = new Vector3();
            
            propertyColor = image.color;
            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(newColor, out targetHSV.x, out targetHSV.y, out targetHSV.z);

            propertyHSV.x = targetHSV.x;

            newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

            image.color = newColor;
        }

        public static void ChangeHue(this Text text, Color newColor)
        {
            Color propertyColor = new Color();
            Vector3 propertyHSV = new Vector3();
            Vector3 targetHSV = new Vector3();
            
            propertyColor = text.color;
            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(newColor, out targetHSV.x, out targetHSV.y, out targetHSV.z);

            propertyHSV.x = targetHSV.x;

            newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

            text.color = newColor;
        }

        public static void ChangeHue(this Image image)
        {
            Color newColor = new Color();
            Color propertyColor = new Color();
            Vector3 propertyHSV = new Vector3();
            Vector3 targetHSV = new Vector3();
            
            propertyColor = image.color;
            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(newColor, out targetHSV.x, out targetHSV.y, out targetHSV.z);

            propertyHSV.x = targetHSV.x;

            newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

            image.color = newColor;
        }
    }
}
