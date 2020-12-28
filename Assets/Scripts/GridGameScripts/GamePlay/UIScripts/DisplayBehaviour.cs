using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
namespace GridGame
{
    public class DisplayBehaviour : MonoBehaviour
    {
        //the current block that the player has
        [SerializeField]
        private GridGame.VariableScripts.BlockVariable playerBlock;
        //whether or not the Ui shoulld update itself
        private bool canUpdate;
        //The image that the UI displays
       private RawImage _image;
        // Use this for initialization
        void Start()
        {
            _image = GetComponent<RawImage>();
            _image.color = Color.red;
        }
        //Changes the color of the ui to reflec6t he current block choice of the player
        public void ChangeColor()
        {
            _image.color = playerBlock.Color;
        }
        //set canuodate to true
        public void EnableUpdate()
        {
            canUpdate = true;
        }
        //sets can update to false
        public void DisableUpdate()
        {
            canUpdate = false;
        }
        private void Update()
        {
            if(canUpdate)
            {
                ChangeColor();
            }
        }
    }
}
