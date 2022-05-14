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
        DANGER,
        UNBLOCKABLE
    }

    public class GridTrackerBehaviour : MonoBehaviour
    {
        public MarkerType Marker;
        private PanelBehaviour _lastPanelMarked;

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
            MarkPanelAtLocation(transform.position, Marker);
        }
    }
}