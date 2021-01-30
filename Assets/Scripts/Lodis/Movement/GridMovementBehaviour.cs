﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.GridScripts;
using Lodis.Gameplay;

namespace Lodis.Movement
{
    public class GridMovementBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The position of the object on the grid.")]
        private Vector2 _position;
        private Vector3 _targetPosition;
        [SerializeField]
        [Tooltip("This is how close the object has to be to the panel it's moving towards to say its reached it.")]
        private float _targetTolerance;
        [SerializeField]
        [Tooltip("The current direction the object is moving in.")]
        private Vector2 _velocity;
        [SerializeField]
        [Tooltip("How fast the object can move towards a panel.")]
        private float _speed;
        private bool _isMoving;
        [Tooltip("If true, the object can cancel its movement in one direction, and start moving in another direction.")]
        public bool canCancelMovement;

        /// <summary>
        /// How much time it takes to move between panels
        /// </summary>
        public float Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }
        
        public Vector2 Position
        {
            get { return _position; }
        }

        public bool IsMoving
        {
            get
            {
                return _isMoving;
            }
        }

        private void Awake()
        {
            _targetPosition = transform.position;
        }

        /// <summary>
        /// Gradually moves the gameObject from its current position to the position given.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        private IEnumerator LerpPosition(Vector3 newPosition)
        {
            float lerpVal = 0;
            Vector3 startPosition = transform.position;

            while (transform.position != newPosition)
            {
                //Sets the current position to be the current position in the interpolation
                transform.position = Vector3.Lerp(startPosition, newPosition, lerpVal += Time.deltaTime * _speed);
                //Waits until the next fixed update before resuming to be in line with any physics calls
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="panelPosition">The position of the panel on the grid that the gameObject will traver to.</param>
        /// <param name="snapPosition">If true, teh gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(Vector2 panelPosition, bool snapPosition = false)
        {
            if (IsMoving && !canCancelMovement)
                return false;

            PanelBehaviour targetPanel;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Grid.GetPanel(panelPosition, out targetPanel, false))
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

            if (snapPosition)
            {
                transform.position = newPosition;
            }
            else
            {
                StartCoroutine(LerpPosition(newPosition));
            }

            _position = targetPanel.Position;
            return true;
        }

        /// <summary>
        /// Moves the gameObject from its current panel to the panel at the given position.
        /// </summary>
        /// <param name="x">The x position of the panel on the grid that the gameObject will traver to.</param>
        /// /// <param name="y">The y position of the panel on the grid that the gameObject will traver to.</param>
        /// <param name="snapPosition">If true, teh gameObject will immediately teleport to its destination without a smooth transition.</param>
        /// <returns>Returns false if the panel is occupied or not in the grids array of panels.</returns>
        public bool MoveToPanel(int x, int y, bool snapPosition = false)
        {
            if (IsMoving && !canCancelMovement)
                return false;

            PanelBehaviour targetPanel;

            //If it's not possible to move to the panel at the given position, return false.
            if (!BlackBoardBehaviour.Grid.GetPanel(x, y, out targetPanel, false))
                return false;

            //Sets the new position to be the position of the panel added to half the gameOgjects height.
            //Adding the height ensures the gameObject is not placed inside the panel.
            Vector3 newPosition = targetPanel.transform.position + new Vector3(0, transform.localScale.y / 2, 0);
            _targetPosition = newPosition;

            if (snapPosition)
            {
                transform.position = newPosition;
            }
            else
            {
                StartCoroutine(LerpPosition(newPosition));
            }

            _position = targetPanel.Position;
            return true;
        }

        private void Update()
        {
            MoveToPanel(_position + Velocity);
            _isMoving = Vector3.Distance(transform.position, _targetPosition) >= _targetTolerance;
        }
    }
}

