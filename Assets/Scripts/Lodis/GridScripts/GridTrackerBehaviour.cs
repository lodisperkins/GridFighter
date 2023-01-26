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
        UNBLOCKABLE,
        NONE
    }

    public class GridTrackerBehaviour : MonoBehaviour
    {
        public MarkerType Marker;
        private PanelBehaviour _lastPanelMarked;
        [Tooltip("If true, will mark all panels this object collides with. Marks panels based on location by default.")]
        [SerializeField]
        private bool _markPanelsBasedOnCollision;
        private List<PanelBehaviour> _panelsInRange;

        public List<PanelBehaviour> PanelsInRange 
        {
            get
            {
                if (_panelsInRange == null)
                    return _panelsInRange = new List<PanelBehaviour>();

                return _panelsInRange;
            }
            set => _panelsInRange = value;
        }

        private void OnDisable()
        {
            _lastPanelMarked?.RemoveMark();
            _panelsInRange?.Clear();
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

        public bool MarkPanel(PanelBehaviour panel, MarkerType markerType)
        {
            if (!panel)
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


        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Panel") || !_markPanelsBasedOnCollision) return;

            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();
            
            if (!PanelsInRange.Contains(panel))
            {
                PanelsInRange.Add(panel);
                panel.Mark(Marker, gameObject);
                _lastPanelMarked = panel;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Panel") || !_markPanelsBasedOnCollision) return;
            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();

            if (PanelsInRange.Contains(panel))
            {
                PanelsInRange.Remove(panel);
                panel.RemoveMark();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!_markPanelsBasedOnCollision)
                MarkPanelAtLocation(transform.position, Marker);
        }
    }
}