using System.Collections;
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

        // Start is called before the first frame update
        void Start()
        {
            _lastCameraPosition = transform.position;
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

                if (character.CompareTag("Player"))
                    characterPos *= 2;

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
            _cameraPosition = _lastCameraPosition;
            _averagePosition = GetNewPosition();
            float avgDistance = GetAverageDistance(_averagePosition);

            _cameraPosition.x = GetAxisPositionByAvg(_minX,_midX, _maxX, _averagePosition.x, _moveSensitivity.x);
            if (Mathf.Abs(_averagePosition.z) > 0)
                _cameraPosition.z = GetAxisPositionByAvg(_minZ, _midZ, _maxZ, _averagePosition.z, _moveSensitivity.z);

            transform.position = Vector3.Lerp(transform.position, _cameraPosition, 1.5f * Time.deltaTime);
        }
    }
}

