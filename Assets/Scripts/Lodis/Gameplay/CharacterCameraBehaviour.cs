using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lodis.Gameplay
{
    public class CharacterCameraBehaviour : MonoBehaviour
    {
        private Camera _attachedCamera;

        // Start is called before the first frame update
        void Awake()
        {
            _attachedCamera = GetComponent<Camera>();
            var camData = Camera.main.GetUniversalAdditionalCameraData();
            camData.cameraStack.Add(_attachedCamera);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}