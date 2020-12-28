using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GridGame.InputCustom
{
    public class InputButtonBehaviour : MonoBehaviour
    {
        [SerializeField]
        private bool _hasMultipleButtons;
        //list of inputs the player has
        [SerializeField]
        private List<VariableScripts.InputVariable> inputs;
        //reference to the last input added
        [SerializeField] private VariableScripts.InputVariable newInput;
        public float inputBuffer;
        public Vector2 inputRange;
        private bool canMove;
        //used for input buffer
        private float timer;

        // Use this for initialization
        void Start()
        {
            canMove = true;
        }
        //checks the list to see if any of the button are down
        public void CheckButton(VariableScripts.InputVariable input)
        {
            if (Input.GetAxis(input.Axis) >= inputRange.y && input.canPress)
            {
                input.canPress = false;
                if (input.CheckBufferTime())
                {
                    SendMessage(input.ButtonDownMessage, input.Arg);
                }
            }
            else if (Input.GetAxis(input.Axis) <= inputRange.x && input.canPress)
            {
                input.canPress = false;
                if (input.CheckBufferTime())
                {
                    SendMessage(input.ButtonNegativeMessage);
                }
            }
            else if (Input.GetAxis(input.Axis) < .5 && Input.GetAxis(input.Axis) > -.5 && input.ButtonUpMessage != "")
            {
                input.canPress = true;
                SendMessage(input.ButtonUpMessage);
            }
            else if (Input.GetAxis(input.Axis) < .5 && Input.GetAxis(input.Axis) > -.5)
            {
                input.canPress = true;
            }
        }
        public void CheckHeldButton(VariableScripts.InputVariable input)
        {
            if (Input.GetAxisRaw(input.Axis) == 1 && input.canPress)
            {

                if (input.CheckBufferTime() && input.CheckHoldTime())
                {
                    if (input.holdTime != 0)
                    {
                        input.canPress = false;
                    }
                    SendMessage(input.ButtonDownMessage, input.Arg);
                    input.ResetHoldTime();
                }
            }
            else if (Input.GetAxisRaw(input.Axis) == -1 && input.canPress && input.ButtonNegativeMessage != "")
            {
                if (input.CheckBufferTime() && input.CheckHoldTime())
                {
                    if (input.holdTime != 0)
                    {
                        input.canPress = false;
                    }
                    SendMessage(input.ButtonNegativeMessage);
                    input.ResetHoldTime();
                }
            }
            else if (Input.GetAxis(input.Axis) < .1 && Input.GetAxis(input.Axis) > -.1 && input.ButtonUpMessage != "")
            {
                input.ResetHoldTime();
                input.canPress = true;
                SendMessage(input.ButtonUpMessage);
            }
            else if (Input.GetAxis(input.Axis) < .1 && Input.GetAxis(input.Axis) > -.1)
            {
                input.ResetHoldTime();
                input.canPress = true;
            }
        }
        public void CheckButtons(VariableScripts.InputVariable input)
        {
            bool allPressed = true;
            foreach (var button in input.AxisNames)
            {

                if (Input.GetAxisRaw(button) != 1 && Input.GetAxisRaw(button) != -1)
                {
                    allPressed = false;
                    if (input.ButtonUpMessage != "")
                    {
                        SendMessage(input.ButtonUpMessage);
                    }
                    break;
                }
            }
            if (allPressed)
            {
                SendMessage(input.ButtonDownMessage);
            }
        }

        public void CheckInputs()
        {
            foreach (var input in inputs)
            {
                if (input.hasMultipleButtons)
                {
                    CheckButtons(input);
                }
                else if (input.mustHold)
                {
                    CheckHeldButton(input);
                }
                else
                {
                    CheckButton(input);
                }
            }
        }
        //adds an input to the list
        public void AddInput(string Axis, string message1, string message2, string message3, object Arg, bool hasMultiInput, List<string> buttons, bool mustHold, float holdTime)
        {
            newInput = VariableScripts.InputVariable.CreateInstance(Axis, message1, message2, message3, Arg, inputBuffer, hasMultiInput, buttons, mustHold, holdTime);
            inputs.Add(newInput);
        }
        //clears the entire input list
        public void Clear()
        {
            inputs.Clear();
        }

        public void CleanList()
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i] == null)
                    inputs.RemoveAt(i);
            }
        }

        // Update is called once per frame
        void Update()
        {

            CheckInputs();
            //Debug.Log(Input.GetAxis("Special1"));
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(GridGame.InputCustom.InputButtonBehaviour))]
public class InputButtonEditor : Editor
{
    string button;
    [SerializeField]
    List<string> buttons;
    private bool hasMultipleInputs;
    private bool mustHold;
    private float holdTime;
    string message1;
    string message2;
    string message3;
    [SerializeField]
    Object arg;
    
    public override void OnInspectorGUI()
    {
        GridGame.InputCustom.InputButtonBehaviour myscript = (GridGame.InputCustom.InputButtonBehaviour)target;
        DrawDefaultInspector();
        button = EditorGUILayout.TextField("Input Button Name",button);
        message1 = EditorGUILayout.TextField("Button Down Func",message1);
        message2 = EditorGUILayout.TextField("Button Up Func", message2);
        message3 = EditorGUILayout.TextField("Button Negative Func", message3);
        arg = EditorGUILayout.ObjectField("Argument",arg, typeof(object), true);
        hasMultipleInputs = EditorGUILayout.Toggle("Has Multiple Inputs", hasMultipleInputs);
        mustHold = EditorGUILayout.Toggle("Must Be Held", mustHold);
        if(mustHold)
        {
            holdTime = EditorGUILayout.FloatField("Hold Time", holdTime);
        }
        if (GUILayout.Button("Add Input"))
        {
            myscript.AddInput(button,message1,message2,message3,arg,hasMultipleInputs,buttons,mustHold,holdTime);
        }
        if(GUILayout.Button("Clear"))
        {
            myscript.Clear();
        }
        if (GUILayout.Button("CleanList"))
        {
            myscript.CleanList();
        }
    }
    
}
#endif
