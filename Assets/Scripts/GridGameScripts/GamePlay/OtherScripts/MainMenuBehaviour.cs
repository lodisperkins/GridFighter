using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridGame.GamePlay.AI;
using GridGame;
public class MainMenuBehaviour : MonoBehaviour {
    [SerializeField]
    private GameObject player;

	// Use this for initialization
	void Start () {
		
	}
    private void EnableAI()
    {
        player.GetComponent<AIControllerBehaviour>().enabled = true;
        player.GetComponent<AIMovementBehaviour>().enabled = true;
        player.GetComponent<AISpawnBehaviour>().enabled = true;
        player.GetComponent<BinaryTreeBehaviour>().enabled = true;
    }
    // Update is called once per frame
    void Update () {
		
	}
}
