using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.VariableScripts
{
    [CreateAssetMenu(menuName = "Variables/InputVariable")]
    public class InputVariable : ScriptableObject
    {
        [SerializeField]
        private object arg;
        [SerializeField]
        private string buttonDownMessage;
        [SerializeField]
        private string buttonUpMessage;
        [SerializeField]
        private string buttonNegativeMessage;
        [SerializeField]
        private string axis;
        public bool hasMultipleButtons;
        public bool mustHold;
        public bool canPress = true;
        [SerializeField] private List<string> _axisNames;
        [SerializeField]
        private float inputBuffer;
        private float inputBufferTimer;
        private float holdTimer;
        public float holdTime;
        public string Axis
        {
            get
            {
                return axis;
            }
        }

        public List<string> AxisNames
        {
            get { return _axisNames; }
        }

        public float InputBuffer
        {
            get
            {
                return inputBuffer;
            }
        }

        public string ButtonDownMessage
        {
            get
            {
                return buttonDownMessage;
            }
        }
        public string ButtonUpMessage
        {
            get
            {
                return buttonUpMessage;
            }
        }
        public string ButtonNegativeMessage
        {
            get
            {
                return buttonNegativeMessage;
            }
        }


        public bool CheckBufferTime()
        {
            if (Time.time < inputBufferTimer)
            {
                return false;
            }
            else
            {
                inputBufferTimer = Time.time + InputBuffer;
                return true;
            }
        }
        public void ResetHoldTime()
        {
            holdTimer = 0;
        }
        public bool CheckHoldTime()
        {
            //Debug.Log(holdTimer);
            if (holdTime <= 0)
            {
                return true;
            }
            if (holdTimer == 0)
            {
                holdTimer = Time.time + holdTime;
                return false;
            }
            else if (Time.time >= holdTimer)
            {
                holdTimer = 0;
                return true;
            }
            return false;
        }
        public object Arg
        {
            get
            {
                return arg;
            }

        }
        private void Init(string axisName, string funcMessage1, string funcMessage2, string funcMessage3,
            object funcArg, float timer, bool hasMultiInput, List<string> buttons, bool mustHoldVal, float holdTimeVal)
        {
            axis = axisName;
            buttonDownMessage = funcMessage1;
            buttonUpMessage = funcMessage2;
            buttonNegativeMessage = funcMessage3;
            arg = funcArg;
            inputBuffer = timer;
            hasMultipleButtons = hasMultiInput;
            _axisNames = buttons;
            mustHold = mustHoldVal;
            holdTime = holdTimeVal;
        }
        public static InputVariable CreateInstance(string axisName, string funcMessage1, string funcMessage2, string funcMessage3,
            object funcArg, float timer, bool hasMultiInput, List<string> buttons, bool mustHoldVal, float holdTime)
        {
            var data = CreateInstance<InputVariable>();
            data.Init(axisName, funcMessage1, funcMessage2, funcMessage3, funcArg, timer, hasMultiInput, buttons, mustHoldVal, holdTime);
            return data;
        }
    }
}


