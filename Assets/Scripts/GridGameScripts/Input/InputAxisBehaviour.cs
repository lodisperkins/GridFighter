using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GridGame.InputCustom
{
    public class InputAxisBehaviour : MonoBehaviour
    {
        //the horizontal and vertical axis that controls this player
        [SerializeField]
        private string HorizontalAxis;
        [SerializeField]
        private string VerticalAxis;
        //the direction the player is holding
        [SerializeField]
        private VariableScripts.Vector2Variable Direction;
        //if true Input.GetAxisRaw is use instead of Input.GetAxis
        [SerializeField]
        private bool isRawInput;
        //used for input buffer
        private float timer;
        private float StopRollTime;
        //true if the player is ,oving
        private bool _isMoving;

        private void Start()
        {
            StopRollTime = .3f;
            timer = StopRollTime + Time.time;
        }
        //sends a message to the playeranimationbehaviour script tp disable or enable movement
        public void SendAnimationMessage()
        {
            _isMoving = CheckIfNotMoving();
            if (_isMoving)
            {
                SendMessage("EnableMoveAnimation");
            }
            else
            {
                SendMessage("DisableMoveAnimation");
            }
        }
        //Checks if a float is in the range of two other floats
        public bool InRange(float val,float num1, float num2)
        {
            return val >= num1 && val <= num2;
        }
        //checks if the player has been stationary for short period to enable the idle animation
        public bool CheckIfNotMoving()
        {
            if (InRange(Direction.Val.magnitude, 0, .2f))
            {
                if (Time.time >= timer)
                {
                    return false;
                }
            }
            else
            {
                timer = StopRollTime + Time.time;
            }
            return true;
        }
        
        // Update is called once per frame
        void Update()
        {
            if (isRawInput)
            {
                Direction.X = Input.GetAxisRaw(HorizontalAxis);
                Direction.Y = Input.GetAxisRaw(VerticalAxis);
            }
            else
            {
                Direction.X = Input.GetAxis(HorizontalAxis);
                Direction.Y = Input.GetAxis(VerticalAxis);
            }
            SendAnimationMessage();
        }
    }
}
