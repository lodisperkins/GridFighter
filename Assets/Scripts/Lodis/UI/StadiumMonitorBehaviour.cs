using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.UI
{

    enum MonitorScreen
    {
        VSPANEL,
        COMBOSCREEN,
        DANGERSCREEN,
        WINNERSCREEN
    }
    public class StadiumMonitorBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _vsPanel;
        [SerializeField]
        private GameObject _comboScreen;
        [SerializeField]
        private GameObject _dangerScreen;
        [SerializeField]
        private GameObject _winnerScreen;
        [SerializeField]
        private CameraBehaviour _camera;
        private MonitorScreen _currentScreenActive;
        private TimedAction _dangerTimer;

        internal MonitorScreen CurrentScreenActive { get => _currentScreenActive; private set => _currentScreenActive = value; }

        // Start is called before the first frame update
        void Start()
        {
            MatchManagerBehaviour.Instance.AddOnP1RingoutAction(() => SetWinnerScreenActive(GridScripts.GridAlignment.RIGHT));
            MatchManagerBehaviour.Instance.AddOnP2RingoutAction(() => SetWinnerScreenActive(GridScripts.GridAlignment.LEFT));
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(SetVSPanelActive);
        }

        public void SetVSPanelActive()
        {
            _comboScreen.SetActive(false);
            _dangerScreen.SetActive(false);
            _winnerScreen.SetActive(false);
            _vsPanel.SetActive(true);
            _camera.AlignmentFocus = GridScripts.GridAlignment.ANY;
            _camera.ZoomAmount = 0;
            _camera.ClampX = true;
            _currentScreenActive = MonitorScreen.VSPANEL;
        }

        public void SetWinnerScreenActive(GridScripts.GridAlignment winnerAlignement)
        {
            RoutineBehaviour.Instance.StopAction(_dangerTimer);
            _comboScreen.SetActive(false);
            _dangerScreen.SetActive(false);
            _winnerScreen.SetActive(true);
            _vsPanel.SetActive(false);

            _camera.AlignmentFocus = winnerAlignement;
            _camera.ZoomAmount = 5;
            _camera.ClampX = false;
            _currentScreenActive = MonitorScreen.WINNERSCREEN;
        }

        public void SetComboScreenActive()
        {
            if (CurrentScreenActive == MonitorScreen.DANGERSCREEN || CurrentScreenActive == MonitorScreen.WINNERSCREEN)
                return;

            RoutineBehaviour.Instance.StopAction(_dangerTimer);
            _comboScreen.SetActive(true);
            _dangerScreen.SetActive(false);
            _winnerScreen.SetActive(false);
            _vsPanel.SetActive(false);

            _camera.AlignmentFocus = GridScripts.GridAlignment.ANY;
            _camera.ZoomAmount = 2;
            _camera.ClampX = true;
            _currentScreenActive = MonitorScreen.COMBOSCREEN;
        }

        public void SetDangerScreenActive(int loserAlignement)
        {
            _comboScreen.SetActive(false);
            _dangerScreen.SetActive(true);
            _winnerScreen.SetActive(false);
            _vsPanel.SetActive(false);

            _camera.AlignmentFocus = (GridScripts.GridAlignment)loserAlignement;
            _camera.ZoomAmount = 8;
            _camera.ClampX = false;
            _currentScreenActive = MonitorScreen.DANGERSCREEN;
            _dangerTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => SetVSPanelActive(), TimedActionCountType.SCALEDTIME, 5);
        }
    }
}