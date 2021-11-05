﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;

namespace Lodis
{
    public class CameraBehaviour : MonoBehaviour
    {
        private Vector3 _cameraPosition;
        [SerializeField]
        private Vector3 _cameraMoveSpeed;
        private float _currentTimeX;
        private Vector3 _lastCameraPosition;
        [SerializeField]
        private Vector3 _moveSensitivity;
        [SerializeField]
        private float _minX;
        [SerializeField]
        private float _midX;
        [SerializeField]
        private float _maxX;
        [SerializeField]
        private float _minZ;
        [SerializeField]
        private float _midZ;
        [SerializeField]
        private float _maxZ;
        [SerializeField]
        private Vector3 _averagePosition;
        [SerializeField]
        private AnimationCurve _curve;
        private float _currentTimeZ;

        // Start is called before the first frame update
        void Start()
        {
            _lastCameraPosition = transform.position;
            _cameraPosition = transform.position;
        }

        public Vector3 GetNewPosition()
        {
            Vector3 averagePosition = new Vector3();
            List<GameObject> entities = BlackBoardBehaviour.Instance.GetEntitiesInGame();
            int characterCount = 0;

            foreach (GameObject character in entities)
            {
                Vector3 characterPos = character.transform.position;

                if (characterPos.x < -5 || characterPos.x > BlackBoardBehaviour.Instance.Grid.Width + 5 || characterPos.y < -5)
                    continue;

                //Makes camera have move more towards players
                //if (character.CompareTag("Player"))
                //    characterPos *= 5;

                averagePosition += character.transform.position;
                characterCount++;
            }

            averagePosition /= characterCount;

            return averagePosition;
        }

        public float GetAverageDistance(Vector3 center)
        {
            float averageDistance = 0;
            List<GameObject> entities = BlackBoardBehaviour.Instance.GetEntitiesInGame();
            int characterCount = entities.Count;

            foreach (GameObject character in entities)
            {
                Vector3 characterPos = character.transform.position;

                if (characterPos.x < 0 || characterPos.x > BlackBoardBehaviour.Instance.Grid.Width)
                    continue;

                //Makes camera have move more towards players
                //if (character.CompareTag("Player"))
                //    characterPos *= 2;

                averageDistance += Vector3.Distance(character.transform.position, center);
            }

            averageDistance /= characterCount;

            return averageDistance;
        }

        private float GetAxisPositionByAvg(float min, float mid, float max, float averagePositionOnAxis, float moveSensitivity)
        {
            if (averagePositionOnAxis - mid >= moveSensitivity)
            {
                return max;
            }
            else if (averagePositionOnAxis - mid <= -moveSensitivity)
                return min;
            else
                return mid;
        }


        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(_averagePosition, 0.5f);
        }

        // Update is called once per frame
        void Update()
        {

            _averagePosition = GetNewPosition();
            float avgDistance = GetAverageDistance(_averagePosition);

            _cameraPosition.x = GetAxisPositionByAvg(_minX,_midX, _maxX, _averagePosition.x, _moveSensitivity.x);
            if (Mathf.Abs(_averagePosition.z) > 0)
                _cameraPosition.z = GetAxisPositionByAvg(_minZ, _midZ, _maxZ, _averagePosition.z, _moveSensitivity.z);


            if (_cameraPosition.x != _lastCameraPosition.x)
                _currentTimeX = 0;

            if (_cameraPosition.z != _lastCameraPosition.z)
                _currentTimeZ = 0;

            _currentTimeX = Mathf.MoveTowards(_currentTimeX, 1, _cameraMoveSpeed.x * Time.deltaTime);
            _currentTimeZ = Mathf.MoveTowards(_currentTimeZ, 1, _cameraMoveSpeed.z * Time.deltaTime);

            float newX = 0;
            newX = Mathf.Lerp(transform.position.x, _cameraPosition.x, _currentTimeX);
            float newZ = 0;
            newZ = Mathf.Lerp(transform.position.z, _cameraPosition.z, _currentTimeZ);
            transform.position = new Vector3(newX, transform.position.y, newZ);

            _lastCameraPosition = _cameraPosition;
        }
    }
}

