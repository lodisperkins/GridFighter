using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ColorManagerBehaviour : MonoBehaviour
    {
        [SerializeField] private IntVariable _ownerID;
        [SerializeField] private string[] _shaderProperties;
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private bool _onlyChangeHue;
        private Color _ownerColor;

        private void SetHue(string property)
        {
            Color propertyColor = _renderer.material.GetColor(property);
            Vector3 propertyHSV = new Vector3();
            Vector3 ownerHSV = new Vector3();

            Color.RGBToHSV(propertyColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);
            Color.RGBToHSV(_ownerColor, out ownerHSV.x, out ownerHSV.y, out ownerHSV.z);

            propertyHSV.x = ownerHSV.x;

            Color newColor = Color.HSVToRGB(propertyHSV.x, propertyHSV.y, propertyHSV.z);

            _renderer.material.SetColor(property, newColor);
        }

        // Start is called before the first frame update
        void Start()
        {
            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByID(_ownerID);    

            foreach (string property in _shaderProperties)
            {
                if (_onlyChangeHue)
                    SetHue(property);
                else
                    _renderer.material.SetColor(property, _ownerColor);
            }
        }

    }
}
