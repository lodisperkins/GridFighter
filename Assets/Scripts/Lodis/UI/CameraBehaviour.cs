using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;

namespace Lodis
{
    public class CameraBehaviour : MonoBehaviour
    {
        private Camera _camera;
        private float _maxDistance;
        private Vector3 _cameraPosition;
        [SerializeField]
        private Vector3 _cameraOffset;
        [SerializeField]
        private float _cameraMoveSpeed;
        private Vector3 _lastCameraPosition;
        [SerializeField]
        private Vector3 _moveSensitivity;

        // Start is called before the first frame update
        void Start()
        {
            _camera = GetComponent<Camera>();
            _maxDistance = BlackBoardBehaviour.Instance.Grid.Width;
        }

        public Vector3 GetNewPosition()
        {
            Vector3 averagePosition = new Vector3();
            List<GameObject> entities = BlackBoardBehaviour.Instance.GetEntitiesInGame();
            int characterCount = entities.Count;

            foreach (GameObject character in entities)
            {
                Vector3 characterPos = character.transform.position;

                if (characterPos.x < -5 || characterPos.x > BlackBoardBehaviour.Instance.Grid.Width + 5)
                    continue;

                if (character.CompareTag("Player"))
                    characterPos *= 5;

                averagePosition += character.transform.position;
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

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(_cameraPosition, 0.5f);
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 newMovePosition = GetNewPosition();
            float avgDistance = GetAverageDistance(newMovePosition);
            _cameraOffset.z = Mathf.Clamp(-avgDistance * 4, -5, -3);
            newMovePosition += _cameraOffset;

            if (Mathf.Abs(newMovePosition.x - _lastCameraPosition.x) > _moveSensitivity.x)
            {
                _cameraPosition.x = newMovePosition.x;
                _lastCameraPosition = _cameraPosition;
            }
            if (Mathf.Abs(newMovePosition.y - _lastCameraPosition.y) > _moveSensitivity.y)
            {
                _cameraPosition.y = newMovePosition.y;
                _lastCameraPosition = _cameraPosition;
            }
            if (Mathf.Abs(newMovePosition.z - _lastCameraPosition.z) > _moveSensitivity.z)
            {
                _cameraPosition.z = newMovePosition.z;
                _lastCameraPosition = _cameraPosition;
            }


            Vector3 moveDirection = (_cameraPosition - transform.position).normalized;
            //Debug.Log((Vector3.Distance(_cameraPosition, transform.position)));
            if (Vector3.Distance(_cameraPosition, transform.position) > 0.1f)
                transform.position += moveDirection * (_cameraMoveSpeed * (Vector3.Distance(_cameraPosition, transform.position))) * Time.deltaTime;
        }
    }
}

