using UnityEngine;

namespace GridGame.Movement
{
	public class PlayerLookBehaviour : MonoBehaviour
	{

		[SerializeField] private Transform _enemyPosition;
    	// Use this for initialization
        private enum Facing
        {
	        Right = 0,
	        Left = 180,
	        Up = -90,
	        Down = 90
        }

        private void RotatePlayerOnYAxis(float degrees)
        {
	        transform.rotation = Quaternion.Euler(transform.rotation.x,degrees,transform.rotation.z);
        }
        public void LookTowardsEnemyPosition()
        {
	        if ((int)_enemyPosition.position.z > (int)transform.position.z)
	        {
		       RotatePlayerOnYAxis((float)Facing.Right);
	        }
	        else if ((int) _enemyPosition.position.z < (int) transform.position.z)
	        {
		        RotatePlayerOnYAxis((float) Facing.Left);
	        }
        }
    	// Update is called once per frame
    	void Update () {
    		LookTowardsEnemyPosition();
    	}
    }

}

