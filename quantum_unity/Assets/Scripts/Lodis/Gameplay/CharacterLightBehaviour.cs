using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class CharacterLightBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GridMovementBehaviour _gridMovementScript;
        [SerializeField]
        private Light _lhsLight;
        [SerializeField]
        private Light _rhsLight;
        [SerializeField]
        private ColorManagerBehaviour _colorManager;
        [SerializeField]
        private GameObject _mesh;

        // Start is called before the first frame update
        void Start()
        {
            if (_gridMovementScript.Alignment == GridScripts.GridAlignment.LEFT)
            {
                _lhsLight.gameObject.SetActive(true);
                _rhsLight.gameObject.SetActive(false);

                _lhsLight.cullingMask = LayerMask.GetMask("LHSMesh");
                ChangeLayer(_mesh, LayerMask.NameToLayer("LHSMesh"));
                _colorManager.SpecularLight = _lhsLight;
            }
            else if (_gridMovementScript.Alignment == GridScripts.GridAlignment.RIGHT)
            {
                _lhsLight.gameObject.SetActive(false);
                _rhsLight.gameObject.SetActive(true);

                _colorManager.SpecularLight = _rhsLight;
                _rhsLight.cullingMask = LayerMask.GetMask("RHSMesh");
                ChangeLayer(_mesh, LayerMask.NameToLayer("RHSMesh"));
            }
        }

        private void ChangeLayer(GameObject go, int layer)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                child.gameObject.layer = layer;

                if (child.childCount > 0)
                    ChangeLayer(child.gameObject, layer);
            }
        }
    }
}