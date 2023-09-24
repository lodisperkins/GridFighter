using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;

namespace Lodis
{
    public class CameraBehaviour : MonoBehaviour
    {
        private static ShakeBehaviour _shakeBehaviour;
        private Vector3 _cameraPosition;
        [SerializeField]
        private Vector3 _cameraMoveSpeed;
        private float _currentTimeX;
        private Vector3 _lastCameraPosition;
        [SerializeField]
        private Vector3 _moveSensitivity;
        [SerializeField]
        private float _minX;
        private float _midX;
        [SerializeField]
        private float _maxX;
        [SerializeField]
        private float _maxY;
        [SerializeField]
        private float _zoomAmount;
        [SerializeField]
        private Vector3 _averagePosition;
        [SerializeField]
        private AnimationCurve _curve;
        private float _currentTimeZ;
        [SerializeField]
        private bool _showAveragePosition;
        private Vector3 _startPosition;
        private Vector3 _startPositionYZ;
        private bool _clampX = true;
        private bool _clampY = true;
        private float _currentTimeY;
        private GridAlignment _alignmentFocus = GridAlignment.ANY;
        private static CameraBehaviour _instance;

        public static ShakeBehaviour ShakeBehaviour { get => _shakeBehaviour; }

        /// <summary>
        /// Gets the static instance of the black board. Creates one if none exists
        /// </summary>
        public static CameraBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraBehaviour>();

                if (!_instance)
                {
                    throw new System.Exception("No camera instance in scene.");
                }

                return _instance;
            }
        }

        private void Awake()
        {
            _shakeBehaviour = GetComponent<ShakeBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            Vector2 dimensions = BlackBoardBehaviour.Instance.Grid.Dimensions;
            int panelXpos = (int)(dimensions.x - 1);
            PanelBehaviour panel = null;
            BlackBoardBehaviour.Instance.Grid.GetPanel(panelXpos, 0, out panel);
            _midX = panel.transform.position.x / 2 + BlackBoardBehaviour.Instance.Grid.transform.position.x;
            _startPosition = new Vector3(_midX, transform.position.y, transform.position.z);
            transform.position = _startPosition;
            _lastCameraPosition = transform.position;
            _cameraPosition = transform.position;

            _startPositionYZ = new Vector3(0, transform.position.y, transform.position.z);
        }

        public Vector3 GetNewPosition()
        {
            Vector3 averagePosition = new Vector3();
            List<GameObject> entities = new List<GameObject> { BlackBoardBehaviour.Instance.Player1, BlackBoardBehaviour.Instance.Player2 };
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

        public float ZoomAmount
        {
            get { return _zoomAmount; }
            set { _zoomAmount = value; }
        }

        public GridAlignment AlignmentFocus { get => _alignmentFocus; set => _alignmentFocus = value; }
        public bool ClampX { get => _clampX; set => _clampX = value; }
        public Vector3 CameraMoveSpeed { get => _cameraMoveSpeed; set => _cameraMoveSpeed = value; }
        public bool ClampY { get => _clampY; set => _clampY = value; }

        public float GetAverageDistance(Vector3 center)
        {
            float averageDistance = 0;
            List<GridMovementBehaviour> entities = BlackBoardBehaviour.Instance.GetEntitiesInGame();
            int characterCount = entities.Count;

            foreach (GridMovementBehaviour character in entities)
            {
                Vector3 characterPos = character.transform.position;

                if (characterPos.x < -2 || characterPos.x > BlackBoardBehaviour.Instance.Grid.Width + 2)
                    continue;

                if (character.Alignment != _alignmentFocus && _alignmentFocus != GridAlignment.ANY)
                    continue;

                //Makes camera have move more towards players
                //if (character.CompareTag("Player"))
                //    characterPos *= 2;

                averageDistance += Vector3.Distance(character.transform.position, center);
            }

            averageDistance /= characterCount;

            return averageDistance;
        }

        private float GetXAxisPositionByAvg(float min, float mid, float max, float averagePositionOnAxis, float moveSensitivity)
        {
            if (!ClampX)
                return averagePositionOnAxis;

            if (averagePositionOnAxis - mid >= moveSensitivity)
            {
                return _startPosition.x + max;
            }
            else if (averagePositionOnAxis - mid <= -moveSensitivity)
                return _startPosition.x - min;
            else
                return mid;
        }

        private float GetZAxisPositionByAvg(float min, float mid, float max, float averagePositionOnAxis, float moveSensitivity)
        {
            if (averagePositionOnAxis - mid >= moveSensitivity)
            {
                return _startPosition.z + max;
            }
            else if (averagePositionOnAxis - mid <= -moveSensitivity)
                return _startPosition.z - min;
            else
                return mid;
        }

        private float GetYAxisPositionByAvg(float min, float mid, float max, float averagePositionOnAxis, float moveSensitivity)
        {
            if (!ClampY)
                return averagePositionOnAxis + _startPosition.y;

            if (averagePositionOnAxis >= moveSensitivity)
            {
                return _startPosition.y + max;
            }
            else
                return _startPosition.y;
        }


        private void OnDrawGizmos()
        {
            if (_showAveragePosition)
                Gizmos.DrawSphere(_averagePosition, 0.5f);
        }

        // Update is called once per frame
        void Update()
        {

            _averagePosition = GetNewPosition();
            float avgDistance = GetAverageDistance(_averagePosition);

            _cameraPosition.x = GetXAxisPositionByAvg(_minX,_midX, _maxX, _averagePosition.x, _moveSensitivity.x);

            _cameraPosition.y = GetYAxisPositionByAvg(0, _startPosition.y, _maxY, _averagePosition.y, _moveSensitivity.y);


            if (_cameraPosition.x != _lastCameraPosition.x)
                _currentTimeX = 0;

            if (_cameraPosition.y != _lastCameraPosition.y)
                _currentTimeY = 0;

            _currentTimeX = Mathf.MoveTowards(_currentTimeX, 1, CameraMoveSpeed.x * Time.deltaTime);
            _currentTimeY = Mathf.MoveTowards(_currentTimeY, 1, CameraMoveSpeed.y * Time.deltaTime);
            //_currentTimeZ = Mathf.MoveTowards(_currentTimeZ, 1, _cameraMoveSpeed.z * Time.deltaTime);

            float newX = 0;
            newX = Mathf.Lerp(transform.position.x, _cameraPosition.x, _currentTimeX);
            float newY = 0;
            float newZ = _startPositionYZ.z;

            if (_zoomAmount == 0)
                newY = Mathf.Lerp(transform.position.y, _cameraPosition.y, _currentTimeY);
            else
            {
                newY = _startPositionYZ.y + transform.forward.y * _zoomAmount;
                newZ = _startPositionYZ.z + transform.forward.z * _zoomAmount;
            }

            transform.position = new Vector3(newX, newY, newZ);

            _lastCameraPosition = _cameraPosition;
        }
    }
}

