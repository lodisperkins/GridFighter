using Lodis.Gameplay;
using Lodis.Sound;
using Lodis.UI;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

namespace Lodis.FX
{
    public class FXManagerBehaviour : SimulationBehaviour
    {
        [SerializeField] private Light[] _environmentLights;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private float _onScreenDistance = 2.5f;
        [SerializeField] private AnimationCurve _superMoveCurve;
        [SerializeField] private BackgroundColorBehaviour _superBackground;
        [SerializeField] private GameObject _explosionEffectSmall;
        [SerializeField] private GameObject _explosionEffectMedium;
        [SerializeField] private GameObject _explosionEffectLarge;

        //---
        private bool _superMoveActive;
        private bool _environmentLightsEnabled;
        private bool _playerControlsEnabled;

        private CharacterCameraBehaviour _player1Camera;
        private CharacterCameraBehaviour _player2Camera;
        private Animator _player1Animator;
        private Animator _player2Animator;
        private List<int> _originalLayers;
        private GameObject[] _lastVisuals;

        private static FXManagerBehaviour _instance;
        private int _lastPlayerSuper;

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
                    manager.AddComponent<EntityDataBehaviour>();
                }

                return _instance;
            }
        }

        public bool SuperMoveEffectActive { get => _superMoveActive; private set => _superMoveActive = value; }
        public int LastPlayerSuper { get => _lastPlayerSuper; private set => _lastPlayerSuper = value; }

        // Start is called before the first frame update
        void Start()
        {
            _player1Camera = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<CharacterCameraBehaviour>();
            _player1Animator = BlackBoardBehaviour.Instance.Player1.GetComponentInChildren<Animator>();
            _player1Camera.AddOnLerpCompleteAction(() => StopAllSuperMoveVisuals(0));
            _player1Camera.CullingMask |= (1 << LayerMask.NameToLayer("LHSMesh"));

            _player2Camera = BlackBoardBehaviour.Instance.Player2.GetComponentInChildren<CharacterCameraBehaviour>();
            _player2Animator = BlackBoardBehaviour.Instance.Player2.GetComponentInChildren<Animator>();
            _player2Camera.AddOnLerpCompleteAction(() => StopAllSuperMoveVisuals(1));
            _player2Camera.transform.parent.localRotation = Quaternion.Euler(0, 180, 0);
            _player2Camera.FlipStartEndTransforms();
            _player2Camera.CullingMask |= (1 << LayerMask.NameToLayer("RHSMesh"));
        }

        public void SetEnvironmentLightsEnabled(bool enabled)
        {
            foreach (Light light in _environmentLights)
                light.enabled = enabled;

            _environmentLightsEnabled = enabled;
        }

        private void  SetPlayerControlsEnabled(bool enabled)
        {
            BlackBoardBehaviour.Instance.Player1Controller.Enabled = enabled;
            BlackBoardBehaviour.Instance.Player2Controller.Enabled = enabled;
            _playerControlsEnabled = enabled;
        }

        private void DisplayScreenShot()
        {
            RenderTexture renderTexture = new RenderTexture(_mainCamera.pixelWidth, _mainCamera.pixelHeight, 24);
            _mainCamera.targetTexture = renderTexture;

            _mainCamera.Render();
            RenderTexture.active = renderTexture;
        }

        private void SetVisualsVisible(bool visible)
        {
            if (visible)
            {
                int layer = _lastPlayerSuper == 0 ? LayerMask.NameToLayer("LHSMesh") : LayerMask.NameToLayer("RHSMesh");
                _originalLayers = new();

                for (int i = 0; i < _lastVisuals.Length; i++)
                {
                    _originalLayers.Add(_lastVisuals[i].layer);
                    _lastVisuals[i].layer = layer;

                    _lastVisuals[i].SetLayerRecursively(layer);
                }
            }
            else if (_originalLayers?.Count > 0)
            {
                for (int i = 0; i < _lastVisuals.Length; i++)
                {
                    _lastVisuals[i].layer = _originalLayers[i];

                    _lastVisuals[i].SetLayerRecursively(_originalLayers[i]);
                }

                _originalLayers.Clear();
            }
        }

        public void EnableSuperBackground(int player)
        {
            if (player == 0)
            {
                _superBackground.gameObject.SetActive(true);
                _superBackground.SetPrimaryColor(BlackBoardBehaviour.Instance.Player1Color);
                _superBackground.SetSecondaryColor(Color.white);
            }
            else if (player == 1)
            {
                _superBackground.gameObject.SetActive(true);
                _superBackground.SetPrimaryColor(Color.white);
                _superBackground.SetSecondaryColor(BlackBoardBehaviour.Instance.Player2Color);
            }
        }

        public void DisableSuperBackground()
        {
            _superBackground.gameObject.SetActive(false);
        }

        public void StartSuperMoveVisual(int player, Fixed32 duration, params GameObject[] extraVisuals)
        {
            if (player != 0 && player != 1)
                return;

            LastPlayerSuper = player;

            if (SuperMoveEffectActive)
                StopAllSuperMoveVisuals(LastPlayerSuper);

            CharacterCameraBehaviour currentCamera = null;
            Animator currentAnimator = null;
            Vector3 direction;

            if (player == 0)
            {
                currentCamera = _player1Camera;
                currentAnimator = _player1Animator;
                currentAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _player2Animator.enabled = false;
                direction = Vector3.back;
            }
            else if (player == 1)
            {
                currentCamera = _player2Camera;
                currentAnimator = _player2Animator;
                currentAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                _player1Animator.enabled = false;
                direction = Vector3.forward;
            }

            _lastVisuals = extraVisuals;

            SetVisualsVisible(true);
            SetEnvironmentLightsEnabled(false);

            MatchManagerBehaviour.Instance.ChangeSimulationTimeScale(0, 0, duration);
            currentCamera.LerpCamera(duration, _superMoveCurve);

            SuperMoveEffectActive = true;
        }

        public void StartSuperMoveVisual(int player, params GameObject[] extraVisuals)
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

                currentAnimator.updateMode = AnimatorUpdateMode.Normal;
                _player2Animator.enabled = false;

                direction = Vector3.right;
            }
            else
            {
                currentCamera = _player2Camera;
                currentAnimator = _player2Animator;
                currentAnimator.updateMode = AnimatorUpdateMode.Normal;
                _player1Animator.enabled = false;
                direction = Vector3.left;
            }

            SetVisualsVisible(true);
            SetEnvironmentLightsEnabled(false);
            MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, currentCamera.LerpDuration);
            currentCamera.LerpCamera(currentCamera.LerpDuration, _superMoveCurve);

            SuperMoveEffectActive = true;
        }

        public void StopAllSuperMoveVisuals(int player)
        {
            if (LastPlayerSuper != -1 && LastPlayerSuper != player)
                return;

            SetEnvironmentLightsEnabled(true);

            MatchManagerBehaviour.Instance.ResetTimeScale();

            SetVisualsVisible(false);

            _player1Camera.StopLerpCamera();
            _player1Camera.SetCameraEnabled(false);
            _player1Animator.updateMode = AnimatorUpdateMode.Normal;
            _player2Animator.enabled = true;

            _player2Camera.StopLerpCamera();
            _player2Animator.updateMode = AnimatorUpdateMode.Normal;
            _player1Animator.enabled = true;
            _player2Camera.SetCameraEnabled(false);

            SuperMoveEffectActive = false;
            LastPlayerSuper = -1;
        }

        public void SpawnExplosion(Vector3 position, int size = 1)
        {
            if (size == 0)
            {
                Instantiate(_explosionEffectSmall, position, Camera.main.transform.rotation);
            }
            else if (size == 1)
            {
                Instantiate(_explosionEffectMedium, position, Camera.main.transform.rotation);
            }
            else if (size >= 2)
            {
                Instantiate(_explosionEffectLarge, position, Camera.main.transform.rotation);
            }

            SoundManagerBehaviour.Instance.PlayFireExplosion();
            CameraBehaviour.ShakeBehaviour.ShakeRotation();
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(_superMoveActive);
            bw.Write(_environmentLightsEnabled);
            bw.Write(_playerControlsEnabled);
            bw.Write(LastPlayerSuper);
        }

        public override void Deserialize(BinaryReader br)
        {
            _superMoveActive = br.ReadBoolean();
            bool environmentLightsWereEnabled = br.ReadBoolean();
            bool playerControlsWereEnabled = br.ReadBoolean();
            LastPlayerSuper = br.ReadInt32();

            if (environmentLightsWereEnabled != _environmentLightsEnabled)
            {
                SetEnvironmentLightsEnabled(environmentLightsWereEnabled);
            }

            if (playerControlsWereEnabled != _playerControlsEnabled)
            {
                SetPlayerControlsEnabled(playerControlsWereEnabled);
            }
        }
    }
}