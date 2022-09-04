using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    [System.Serializable]
    public class ColorObject
    {
        public Renderer ObjectRenderer;
        public string[] ShaderProperties;
        public bool OnlyChangeHue;
    }

    public class ColorManagerBehaviour : MonoBehaviour
    {
        [SerializeField] private IntVariable _ownerID;
        [SerializeField] private ColorObject[] _objectsToColor;
        private Color _ownerColor;

        private void SetHue(ColorObject objectToColor)
        {
            Color propertyColor = new Color();
            Vector3 propertyHSV = new Vector3();
            Vector3 ownerHSV = new Vector3();

            foreach (string property in objectToColor.ShaderProperties)
            {
                propertyColor = objectToColor.ObjectRenderer.material.GetColor(property);
                Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
                Color.RGBToHSV(_ownerColor, out ownerHSV.x, out ownerHSV.y, out ownerHSV.z);

                propertyHSV.x = ownerHSV.x;

                Color newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

                objectToColor.ObjectRenderer.material.SetColor(property, newColor);
            }
        }

        private void SetColor(ColorObject objectToColor)
        {
            foreach (string property in objectToColor.ShaderProperties)
            {
                objectToColor.ObjectRenderer.material.SetColor(property, _ownerColor);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByID(_ownerID);    

            foreach (ColorObject colorObject in _objectsToColor)
            {
                if (colorObject.OnlyChangeHue)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);
            }
        }

    }
}
