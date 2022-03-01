using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public enum MarkerType
    {
        POSITION,
        WARNING,
        DANGER
    }

    public class GridTrackerBehaviour : MonoBehaviour
    {
        public bool MarkPosition;
        public bool MarkWarning;
        public bool MarkDanger;
        private PanelBehaviour _lastPanelMarked;
        // Start is called before the first frame update
        void Start()
        {

        }

        public bool MarkPanelAtLocation(Vector3 position, MarkerType markerType)
        {
            PanelBehaviour panel = null;
            if (!BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(position, out panel))
                return false;

            if (_lastPanelMarked == null)
                _lastPanelMarked = panel;
            else if (panel != _lastPanelMarked)
            {
                _lastPanelMarked.RemoveMark();
                _lastPanelMarked = panel;
            }

            panel.Mark(markerType, gameObject);

            return true;
        }
        public bool MarkPanelAtGridPosition(int x, int y, MarkerType markerType)
        {
            PanelBehaviour panel = null;
            if (!BlackBoardBehaviour.Instance.Grid.GetPanel(x, y, out panel))
                return false;

            if (_lastPanelMarked == null)
                _lastPanelMarked = panel;
            else if (panel != _lastPanelMarked)
            {
                _lastPanelMarked.RemoveMark();
                _lastPanelMarked = panel;
            }

            panel.Mark(markerType, gameObject);

            return true;
        }

        // Update is called once per frame
        void Update()
        {
            if (MarkPosition)
                MarkPanelAtLocation(transform.position, MarkerType.POSITION);

            if (MarkWarning)
                MarkPanelAtLocation(transform.position, MarkerType.WARNING);

            if (MarkDanger)
                MarkPanelAtLocation(transform.position, MarkerType.DANGER);
        }
    }
}