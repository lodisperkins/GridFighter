using GridGame.Movement;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GridGame.GamePlay.OtherScripts
{
	public class ScreenShakeBehaviour : MonoBehaviour {
		public float shakeVal;

		public bool isShaking;
        public bool updatePosition;
        public bool isMoving;
		private Vector3 _startPosition;
        public bool shouldStop;
		private GridPhysicsBehaviour physicsBehaviour;
		private Vector3 yOffset;
		public Vector3 StartPosition
		{
			get { return _startPosition; }
			set { _startPosition = value; }
		}

		[SerializeField]
		private float shakeRange;

		[SerializeField] private int shakeLength;
		private void Start()
		{
			_startPosition = transform.position;
			yOffset = new Vector3(0, transform.position.y, 0);
			physicsBehaviour = GetComponent<GridPhysicsBehaviour>();
		}

		IEnumerator Shake()
		{
           if(updatePosition)
            {
                _startPosition = transform.position;
            }
            if (shakeLength == 0)
			{
				shakeLength = 5;
			}
			isShaking = false;
			bool positive = true;
			float val = 0;
			for(int i = 0; i< shakeLength; i++)
			{
                if(shouldStop)
                {
                    shouldStop = false;
                    yield break;
                }
				if (positive)
				{
					val = Random.Range(0, shakeRange);
					positive = false;
				}
				else
				{
					val = Random.Range( -shakeRange,0);
					positive = true;
				}
				var newPosition = new Vector3( 0,0, val);
				transform.position += newPosition;
				yield return new WaitForSeconds(shakeVal);
			}

			transform.localPosition = _startPosition;
		}

		public void StartShaking()
		{
            if(isMoving)
            {
                return;
            }
			isShaking = true;
		}
		public void StartShaking(int length)
		{
            if (isMoving)
            {
                return;
            }
            shakeLength = length;
			isShaking = true;
		}
	
		// Update is called once per frame
		void Update () {
			if (isShaking)
			{
				StartCoroutine(Shake());
			}
			if(physicsBehaviour)
            {
				if(physicsBehaviour.currentPanel)
                {
					_startPosition = physicsBehaviour.currentPanel.transform.position + yOffset;
				}
				
            }
		}
	}
}
