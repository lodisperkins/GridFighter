using Lodis.Gameplay;
using Lodis.Movement;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
        [SerializeField] private bool _markPanelsBasedOnCollision;
        [Tooltip("If true, will mark the panel at the location of the given movement script. Marks panels based on location by default.")]
        [SerializeField] private bool _markPanelAtGridLocation;
        [Tooltip("If true, will mark the panel at the location of the given collider script. Marks panels based on location by default.")]
        [SerializeField] private bool _markCollider;
        [Tooltip("If true, will mark all panels in cardinal directions at some given range.")]
        [SerializeField] private bool _markAtRange;
        [Tooltip("If true, will mark all panels in all directions at some given range.")]
        [SerializeField] private bool _markAtRadius;
        [ShowIf("_markPanelAtGridLocation")]
        [SerializeField] private GridMovementBehaviour _movementToTrack;
        [ShowIf("_markCollider")]
        [SerializeField] private ColliderBehaviour _colliderToTrack;
        [ShowIf("_markAtRange")]
        [SerializeField] private int _xRange;
        [ShowIf("_markAtRange")]
        [SerializeField] private int _yRange;
        [ShowIf("_markAtRadius")]
        [SerializeField] private int _radius;

        private List<PanelBehaviour> _panelsInRange = new List<PanelBehaviour>();
        private List<PanelBehaviour> _panelsInCollisionRange;

        public List<PanelBehaviour> PanelsInCollisionRange 
        {
            get
            {
                if (_panelsInCollisionRange == null)
                    return _panelsInCollisionRange = new List<PanelBehaviour>();

                return _panelsInCollisionRange;
            }
            set => _panelsInCollisionRange = value;
        }

        public bool MarkPanelsBasedOnCollision { get => _markPanelsBasedOnCollision; set => _markPanelsBasedOnCollision = value; }
        public bool MarkPanelAtGridLocation { get => _markPanelAtGridLocation; set => _markPanelAtGridLocation = value; }
        public bool MarkCollider { get => _markCollider; set => _markCollider = value; }
        public ColliderBehaviour ColliderToTrack { get => _colliderToTrack; set => _colliderToTrack = value; }

        private void OnDisable()
        {
            _lastPanelMarked?.RemoveMark();
            _panelsInCollisionRange?.Clear();
            ClearPanelsInRange();
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

            if (_markAtRange)
            {
                MarkOtherPanelAtRange(panel.Position.X, panel.Position.Y);
            }
            else if (_markAtRadius)
            {
                MarkOtherPanelAtRadius(panel.Position.X, panel.Position.Y);
            }

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


            if (_markAtRange)
            {
                MarkOtherPanelAtRange(panel.Position.X, panel.Position.Y);
            }
            else if (_markAtRadius)
            {
                MarkOtherPanelAtRadius(panel.Position.X, panel.Position.Y);
            }
            return true;
        }

        public bool MarkPanel(PanelBehaviour panel, MarkerType markerType)
        {
            if (!panel)
                return false;

            if (_lastPanelMarked == null)
            {
                _lastPanelMarked = panel;
            }
            else if (panel != _lastPanelMarked)
            {
                _lastPanelMarked.RemoveMark();
                _lastPanelMarked = panel;
            }

            panel.Mark(markerType, gameObject);

            return true;
        }

        public bool MarkPanel(PanelBehaviour panel)
        {
            if (!panel)
                return false;

            if (_lastPanelMarked == null)
            {
                _lastPanelMarked = panel;
            }
            else if (panel != _lastPanelMarked)
            {
                _lastPanelMarked.RemoveMark();
                _lastPanelMarked = panel;
            }

            panel.Mark(Marker, gameObject);

            return true;
        }

        private void MarkOtherPanelAtRange(int x, int y)
        {
            ClearPanelsInRange();

            PanelBehaviour panel;

            for (int i = 1; i <= _xRange; i++)
            {
                if (GridBehaviour.Grid.GetPanel(x + i, y, out panel))
                {
                    _panelsInRange.Add(panel);
                    MarkPanel(panel);
                }

                if (GridBehaviour.Grid.GetPanel(x - i, y, out panel))
                {
                    _panelsInRange.Add(panel);
                    MarkPanel(panel);
                }
            }

            for (int i = 1; i <= _yRange; i++)
            {
                if (GridBehaviour.Grid.GetPanel(x, y + i, out panel))
                {
                    _panelsInRange.Add(panel);
                    MarkPanel(panel);
                }

                if (GridBehaviour.Grid.GetPanel(x, y - i, out panel))
                {
                    _panelsInRange.Add(panel);
                    MarkPanel(panel);
                }
            }
        }

        private void MarkOtherPanelAtRadius(int x, int y)
        {
            ClearPanelsInRange();

            List<PanelBehaviour> panels = GridBehaviour.Grid.GetPanelNeighbors(new FixedPoints.FVector2(x, y), _radius);

            foreach (PanelBehaviour panel in panels)
            {
                _panelsInRange.Add(panel);
                MarkPanel(panel);
            }
        }

        public void ClearPanelsInRange()
        {
            foreach (PanelBehaviour panel in _panelsInRange)
            {
                panel.RemoveMark();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Panel") || !MarkPanelsBasedOnCollision) return;

            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();
            
            if (!PanelsInCollisionRange.Contains(panel))
            {
                PanelsInCollisionRange.Add(panel);
            }

            panel.Mark(Marker, gameObject);
            _lastPanelMarked = panel;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Panel") || !MarkPanelsBasedOnCollision) return;
            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();

            if (PanelsInCollisionRange.Contains(panel))
            {
                PanelsInCollisionRange.Remove(panel);
                panel.RemoveMark();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (MarkPanelsBasedOnCollision)
                return;

            if (MarkPanelAtGridLocation)
            {
                if (_movementToTrack)
                    MarkPanelAtGridPosition(_movementToTrack.Position.X, _movementToTrack.Position.Y, Marker);
            }
            else if (MarkCollider)
            {
                if (ColliderToTrack)
                    MarkPanelAtGridPosition(ColliderToTrack.EntityCollider.PanelX, ColliderToTrack.EntityCollider.PanelY, Marker);
            }
            else
                MarkPanelAtLocation(transform.position, Marker);
        }
    }
}