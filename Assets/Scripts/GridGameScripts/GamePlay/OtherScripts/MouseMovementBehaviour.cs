using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MouseMovementBehaviour : MonoBehaviour {
    public Camera cam;
	// Use this for initialization
	void Start () {
		
	}
    private void OnGUI()
    {
        //Get current event for gui(EX: mouse position updated)
        Event currentEvent = Event.current;
        //create new variable to store mouse position
        Vector2 mousePos = new Vector2();
        //set mouse position to be the current x value of the mouse on screen and subtract y from pixel height so its not inverted
        mousePos.Set(currentEvent.mousePosition.x, cam.pixelHeight - currentEvent.mousePosition.y);
        //convert the mousePos on screen to world space and set its x value to be our new x position
        float xpos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, transform.position.z  - cam.transform.position.z)).x;
        //set the transforms x to be our new x position
        transform.position = new Vector3(xpos, transform.position.y, transform.position.z);
    }
    // Update is called once per frame
    void Update () {
        
	}
}
