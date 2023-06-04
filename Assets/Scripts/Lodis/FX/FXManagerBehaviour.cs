using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.FX
{
    public class FXManagerBehaviour : MonoBehaviour
    {
        private CharacterCameraBehaviour _player1Camera;
        private CharacterCameraBehaviour _player2Camera;
        private Animator _player1Animator;
        private Animator _player2Animator;
        [SerializeField]
        private Light[] _environmentLights;
        [SerializeField]
        private Camera _mainCamera;

        private static FXManagerBehaviour _instance;

        public static FXManagerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType<FXManagerBehaviour>();

                if (!_instance)
                {
                    GameObject manager = new GameObject("FXManager");
                    _instance = manager.AddComponent<FXManagerBehaviour>();
                }

                return _instance;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            _player1Camera = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<CharacterCameraBehaviour>();
            _player1Animator = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<Animator>();
            _player1Camera.AddOnLerpCompleteAction(StopAllSuperMoveVisuals);

            _player2Camera = BlackBoardBehaviour.Instance.Player2.GetComponentInChildren<CharacterCameraBehaviour>();
            _player2Animator = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<Animator>();
            _player2Camera.AddOnLerpCompleteAction(StopAllSuperMoveVisuals);
            _player2Camera.transform.parent.localRotation = Quaternion.Euler(0, 180, 0);
        }

        public void SetEnvironmentLightsEnabled(bool enabled)
        {
            foreach (Light light in _environmentLights)
                light.enabled = enabled;
        }

        private void  SetPlayerControlsEnabled(bool enabled)
        {
            BlackBoardBehaviour.Instance.Player1Controller.Enabled = enabled;
            BlackBoardBehaviour.Instance.Player2Controller.Enabled = enabled;
        }

        private void DisplayScreenShot()
        {
            RenderTexture renderTexture = new RenderTexture(_mainCamera.pixelWidth, _mainCamera.pixelHeight, 24);
            _mainCamera.targetTexture = renderTexture;

            _mainCamera.Render();
            RenderTexture.active = renderTexture;
        }

        public void StartSuperMoveVisual(int player, float duration)
        {
            if (player != 1 && player != 2)
                return;

            CharacterCameraBehaviour currentCamera = null;
            Animator currentAnimator = null;
            if (player == 1)
            {
                currentCamera = _player1Camera;
                currentAnimator = _player1Animator;
            }
            else
            {
                currentCamera = _player2Camera;
                currentAnimator = _player2Animator;
            }

            SetEnvironmentLightsEnabled(false);
            currentAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, duration);
            currentCamera.LerpCamera(duration);
        }

        public void StartSuperMoveVisual(int player)
        {
            if (player != 1 && player != 2)
                return;

            CharacterCameraBehaviour currentCamera = null;
            Animator currentAnimator = null;
            if (player == 1)
            {
                currentCamera = _player1Camera;
                currentAnimator = _player1Animator;
            }
            else
            {
                currentCamera = _player2Camera;
                currentAnimator = _player2Animator;
            }

            SetEnvironmentLightsEnabled(false);
            currentAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, currentCamera.LerpDuration);
            currentCamera.LerpCamera();
        }

        public void StopAllSuperMoveVisuals()
        {
            SetEnvironmentLightsEnabled(true);

            MatchManagerBehaviour.Instance.ResetTimeScale();

            _player1Camera.StopLerpCamera();
            _player1Animator.updateMode = AnimatorUpdateMode.Normal;

            _player2Camera.StopLerpCamera();
            _player2Animator.updateMode = AnimatorUpdateMode.Normal;
        }
    }
}