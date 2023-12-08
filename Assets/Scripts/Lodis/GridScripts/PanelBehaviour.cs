using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
        private Color _neutralColor;
        [SerializeField]
        private float _emissionStrength;
        [SerializeField]
        private float _emissionFadeDuration;
        private Color _positionLHSColor;
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
        private FlashBehaviour _flashBehaviour;

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

        public MarkerType CurrentMarker { get => _currentMarker; private set => _currentMarker = value; }

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
                bool flashAlignment = _alignment != value;
                    
                _alignment = value;
                UpdateMaterial();
                if (flashAlignment)
                    FlashAlignment();
            }
        }

        private void Awake()
        {
            _positionLHSColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(GridAlignment.LEFT);
            _positionRHSColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(GridAlignment.RIGHT);
            _flashBehaviour = GetComponent<FlashBehaviour>();
        }

        /// <summary>
        /// Changes the material of the panel based on the side of the
        /// grid that its on.
        /// </summary>
        private void UpdateMaterial()
        {
            if (!Application.isPlaying)
                return;

            if (_mesh == null)
                _mesh = GetComponent<MeshRenderer>();

            switch (Alignment)
            {
                case GridAlignment.NONE:
                    _defaultColor = Color.black;
                    break;
                case GridAlignment.LEFT:
                    _defaultColor = _positionLHSColor;
                    break;
                case GridAlignment.RIGHT:
                    _defaultColor = _positionRHSColor;
                    break;
                case GridAlignment.ANY:
                    _defaultColor = _neutralColor;
                    break;
            }


                _defaultColor /= 10;
                _mesh.material.SetColor("_Color", _defaultColor);
            //Vector3 propertyHSV;

            //Color.RGBToHSV(_defaultColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

            //if (propertyHSV.y > 0.1f)
            //    _mesh.material.ChangeHue(_defaultColor, "_Color");
            //else
            //{
            //}
        }

        public void Mark(Lodis.GridScripts.MarkerType markerType, GameObject markObject)
        {
            Vector2 markObjectPosition = new Vector2(markObject.transform.position.x, markObject.transform.position.z);
            if (markObject == _markObject && ((markObjectPosition - _lastMarkPosition).magnitude <= BlackBoardBehaviour.Instance.Grid.PanelSpacingX 
                && (markObjectPosition - _lastMarkPosition).magnitude <= BlackBoardBehaviour.Instance.Grid.PanelSpacingZ))
            {
                return;
            }

            _mesh.material.SetInt("_UseEmission", 1);
            switch (markerType)
            {
                case MarkerType.POSITION:

                    if (CurrentMarker != MarkerType.NONE && CurrentMarker != MarkerType.POSITION)
                        break;

                    if (markObject != _markObject || !_markerMovement)
                        _markerMovement = markObject.GetComponent<Movement.GridMovementBehaviour>();

                    if (!_markerMovement)
                        throw new System.Exception("Can't mark grid movement of object that doesn't have a GridMovementBehaviour attached. Object name is " + markObject.name);

                    _positionLHSColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(GridAlignment.LEFT);
                    _positionRHSColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(GridAlignment.RIGHT);

                    if (_markerMovement.Alignment == GridAlignment.LEFT)
                    {
                        _mesh.material.ChangeHue(_positionLHSColor, "_Color");
                        _mesh.material.SetColor("_EmissionColor", _positionLHSColor * _emissionStrength);
                        _mesh.material.DOFloat(_emissionStrength, "_EmissionStrength", _emissionFadeDuration);
                    }
                    else if (_markerMovement.Alignment == GridAlignment.RIGHT)
                    {
                        _mesh.material.ChangeHue(_positionRHSColor, "_Color");
                        _mesh.material.SetColor("_EmissionColor", _positionRHSColor * _emissionStrength);
                        _mesh.material.DOFloat(_emissionStrength, "_EmissionStrength", _emissionFadeDuration);
                    }
                    break;
                case MarkerType.WARNING:
                    _mesh.material.ChangeHue(_warningColor, "_Color");
                    _mesh.material.SetColor("_EmissionColor", _warningColor * _emissionStrength);
                    _mesh.material.DOFloat(_emissionStrength, "_EmissionStrength", _emissionFadeDuration);
                    break;
                case MarkerType.DANGER:
                    _mesh.material.ChangeHue(_dangerColor, "_Color");
                    _mesh.material.SetColor("_EmissionColor", _dangerColor * _emissionStrength);
                    _mesh.material.DOFloat(_emissionStrength, "_EmissionStrength", _emissionFadeDuration);
                    break;
                case MarkerType.UNBLOCKABLE:
                    _mesh.material.ChangeHue(_unblockableColor, "_Color");
                    _mesh.material.SetColor("_EmissionColor", _unblockableColor * _emissionStrength);
                    _mesh.material.DOFloat(_emissionStrength, "_EmissionStrength", _emissionFadeDuration);
                    break;
            }

            _markObject = markObject;
            CurrentMarker = markerType;
        }

        public void RemoveMark()
        {
            if (!_mesh)
                return;

            _mesh?.material.ChangeHue(_defaultColor, "_Color");

            //if (_mesh.material.GetFloat("_EmissionStrength") > 0)
            //    _mesh?.material.SetFloat("_EmissionStrength", _emissionStrength / 2);
                
            _mesh?.material.DOFloat(0, "_EmissionStrength", _emissionFadeDuration);
            CurrentMarker = MarkerType.NONE;
        }

        public void FlashAlignment()
        {
            Color color = _alignment == GridAlignment.LEFT ? _positionLHSColor : _positionRHSColor;
            _flashBehaviour.Flash(color, 5, 1, true, 5);
        }

        private void Update()
        {
            if (!_markObject || !_markObject.activeInHierarchy)
                RemoveMark();
        }
    }
}


