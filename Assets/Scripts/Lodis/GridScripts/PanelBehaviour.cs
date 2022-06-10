using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class PanelBehaviour : MonoBehaviour
    {
        private Vector2 _position;
        [SerializeField]
        private bool _occupied;
        private GridAlignment _alignment;
        [Tooltip("The material to give this panel if it is not aligned with either side of the grid.")]
        [SerializeField]
        private Material _neutralMat;
        [Tooltip("The material to give this panel if it is aligned with the left side of the grid.")]
        [SerializeField]
        private Material _leftSideMat;
        [Tooltip("The material to give this panel if it is aligned with the right side of the grid.")]
        [SerializeField]
        private Material _rightSideMat;
        [SerializeField]
        private Color _positionLHSColor;
        [SerializeField]
        private Color _positionRHSColor;
        [SerializeField]
        private Color _warningColor;
        [SerializeField]
        private Color _dangerColor;
        [SerializeField]
        private Color _unblockableColor;
        private Color _defaultColor;
        [SerializeField]
        private MeshRenderer _mesh;
        private GameObject _markObject;
        private Movement.GridMovementBehaviour _markerMovement;
        private Vector2 _lastMarkPosition;
        private MarkerType _currentMarker;
        
        private void Awake()
        {
            //_mesh = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            _defaultColor = _mesh.material.color;
        }
        /// <summary>
        /// The side of the grid this panel this panel belongs to
        /// </summary>
        public GridAlignment Alignment
        {
            get
            {
                return _alignment;
            }
            set
            {
                _alignment = value;
                UpdateMaterial();
            }
        }

        /// <summary>
        /// Changes the material of the panel based on the side of the
        /// grid that its on.
        /// </summary>
        private void UpdateMaterial()
        {
            if (_mesh == null)
                _mesh = GetComponent<MeshRenderer>();

            switch (_alignment)
            {
                case GridAlignment.LEFT:
                    _mesh.material = _leftSideMat;
                    _defaultColor = _leftSideMat.color;
                    break;
                case GridAlignment.RIGHT:
                    _mesh.material = _rightSideMat;
                    _defaultColor = _rightSideMat.color;
                    break;
                case GridAlignment.ANY:
                    _mesh.material = _neutralMat;
                    _defaultColor = _neutralMat.color;
                    break;
            }
        }

        public void Mark(Lodis.GridScripts.MarkerType markerType, GameObject markObject)
        {
            Vector2 markObjectPosition = new Vector2(markObject.transform.position.x, markObject.transform.position.z);
            if (markObject == _markObject && ((markObjectPosition - _lastMarkPosition).magnitude <= BlackBoardBehaviour.Instance.Grid.PanelSpacingX 
                && (markObjectPosition - _lastMarkPosition).magnitude <= BlackBoardBehaviour.Instance.Grid.PanelSpacingZ))
            {
                return;
            }

            switch (markerType)
            {
                case MarkerType.POSITION:
                    if (_currentMarker != MarkerType.NONE) break;

                    if (markObject != _markObject)
                        _markerMovement = markObject.GetComponent<Movement.GridMovementBehaviour>();

                    if (!_markerMovement)
                        throw new System.Exception("Can't mark grid movement of object that doesn't have a GridMovementBehaviour attached. Object name is " + markObject.name);

                    if (_markerMovement.Alignment == GridAlignment.LEFT)
                        _mesh.material.color = _positionLHSColor;
                    else if (_markerMovement.Alignment == GridAlignment.RIGHT)
                        _mesh.material.color = _positionRHSColor;
                    break;
                case MarkerType.WARNING:
                    _mesh.material.color = _warningColor;
                    break;
                case MarkerType.DANGER:
                    _mesh.material.color = _dangerColor;
                    break;
                case MarkerType.UNBLOCKABLE:
                    _mesh.material.color = _unblockableColor;
                    break;
            }

            _markObject = markObject;
        }

        public void RemoveMark()
        {
            _mesh.material.color = _defaultColor;
            _currentMarker = MarkerType.NONE;
        }

        /// <summary>
        /// The position of this panel on the grid.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// Returns if there is anything preventing an object from moving on to this panel.
        /// </summary>
        public bool Occupied
        {
            get
            {
                return _occupied;
            }
            set
            {
                _occupied = value;
            }
        }

        private void Update()
        {
            if (!_markObject)
                RemoveMark();
        }
    }
}


