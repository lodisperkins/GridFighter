﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class PanelBehaviour : MonoBehaviour
    {
        private Vector2 _position;
        private bool _occupied;
        private GridAlignment _alignment;
        [SerializeField]
        private Material _neutralMat;
        [SerializeField]
        private Material _leftSideMat;
        [SerializeField]
        private Material _rightSideMat;
        private MeshRenderer _mesh;

        private void Awake()
        {
            _mesh = GetComponent<MeshRenderer>();
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

        private void UpdateMaterial()
        {
            if (_mesh == null)
                _mesh = GetComponent<MeshRenderer>();

            switch (_alignment)
            {
                case GridAlignment.LEFT:
                    _mesh.material = _leftSideMat;
                    break;
                case GridAlignment.RIGHT:
                    _mesh.material = _rightSideMat;
                    break;
                case GridAlignment.ANY:
                    _mesh.material = _neutralMat;
                    break;
            }
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

    }
}


