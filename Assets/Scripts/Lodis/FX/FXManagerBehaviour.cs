using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using Types;
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
        [SerializeField]
        private float _onScreenDistance = 2.5f;
        [SerializeField]
        private AnimationCurve _superMoveCurve;
        private bool _superMoveActive;

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

        public bool SuperMoveEffectActive { get => _superMoveActive; private set => _superMoveActive = value; }

        // Start is called before the first frame update
        void Start()
        {
            _player1Camera = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<CharacterCameraBehaviour>();
            _player1Animator = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<Animator>();
            _player1Camera.AddOnLerpCompleteAction(StopAllSuperMoveVisuals);
            _player1Camera.CullingMask |= (1 << LayerMask.NameToLayer("LHSMesh"));

            _player2Camera = BlackBoardBehaviour.Instance.Player2.GetComponentInChildren<CharacterCameraBehaviour>();
            _player2Animator = BlackBoardBehaviour.Instance.Player2.GetComponentInChildren<Animator>();
            _player2Camera.AddOnLerpCompleteAction(StopAllSuperMoveVisuals);
            _player2Camera.transform.parent.localRotation = Quaternion.Euler(0, 180, 0);
            _player2Camera.FlipStartEndTransforms();
            _player2Camera.CullingMask |= (1 << LayerMask.NameToLayer("RHSMesh"));
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

        public void StartSuperMoveVisual(int player, Fixed32 duration)
        {
            if (player != 1 && player != 2)
                return;

            CharacterCameraBehaviour currentCamera = null;
            Animator currentAnimator = null;
            Vector3 direction;

            if (player == 1)
            {
                currentCamera = _player1Camera;
                currentAnimator = _player1Animator;
                direction = Vector3.back;
            }
            else
            {
                currentCamera = _player2Camera;
                currentAnimator = _player2Animator;
                direction = Vector3.forward;
            }
            SetEnvironmentLightsEnabled(false);
            currentAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, duration);
            currentCamera.LerpCamera(duration, _superMoveCurve);

            SuperMoveEffectActive = true;
        }

        public void StartSuperMoveVisual(int player)
        {
            if (player != 1 && player != 2)
                return;

            CharacterCameraBehaviour currentCamera = null;
            Animator currentAnimator = null;
            Vector3 direction;

            if (player == 1)
            {
                currentCamera = _player1Camera;
                currentAnimator = _player1Animator;
                direction = Vector3.right;
            }
            else
            {
                currentCamera = _player2Camera;
                currentAnimator = _player2Animator;
                direction = Vector3.left;
            }

            SetEnvironmentLightsEnabled(false);
            currentAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, currentCamera.LerpDuration);
            currentCamera.LerpCamera(currentCamera.LerpDuration, _superMoveCurve);

            SuperMoveEffectActive = true;
        }

        public void StopAllSuperMoveVisuals()
        {
            SetEnvironmentLightsEnabled(true);

            MatchManagerBehaviour.Instance.ResetTimeScale();

            _player1Camera.StopLerpCamera();
            _player1Camera.SetCameraEnabled(false);
            _player1Animator.updateMode = AnimatorUpdateMode.Normal;

            _player2Camera.StopLerpCamera();
            _player2Animator.updateMode = AnimatorUpdateMode.Normal;
            _player2Camera.SetCameraEnabled(false);

            SuperMoveEffectActive = false;
        }
    }
}