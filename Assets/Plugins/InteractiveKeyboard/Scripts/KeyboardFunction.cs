/*
======================================================================
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// Copyright (c)2017 SAASHA INTERACTIVE. All rights reserved.
// For any help please mail us on saashainteractive AT gamil DOT com
======================================================================
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KeyboardFunction : MonoBehaviour {

    public string inputText = null;
    int wordIndex = 0;    
    public Text InputTextUI = null;
    public Button backSpaceBtn;
    public Button confirmBtn;
    public Button capsBtn;
    public int textLimit = 20;
    
    private float m_TimeStamp;
    private bool cursor = false;
    private string cursorChar = "";
    [SerializeField]
    private UnityEvent _onConfirm;

    public UnityEvent OnConfirm { get => _onConfirm; private set => _onConfirm = value; }

    private void Update()
    {
        //If word index lower than 0 then Caps & Backspace button should be DISABLED.
        if (wordIndex > 0)
            InputTextUI.text = inputText+ cursorChar;

        //*****************************************
        //In this font style gets change if wordindex is equal to 0
        if (wordIndex == 0)
        {            
            inputText = null;
            InputTextUI.fontStyle = FontStyle.Italic;
            InputTextUI.text = "type here ";
        }
        //If character doesnt more than 3 then confirm button should be disabled. You can change this as per your requirements.
        if (wordIndex < 1)
        {
            //Debug.Log("Please type atlest 3 characters");
            confirmBtn.interactable = false;
        }
        else
            confirmBtn.interactable = true;

        //Here cursor blicking happening by changing m_TimeStamp value you can change cursor stamp speed.
        if (Time.time - m_TimeStamp >= 0.1f)
        {
            m_TimeStamp = Time.time;
            if (cursor == false)
            {
                cursor = true;                
                cursorChar += "|";
                if (wordIndex > textLimit && wordIndex < textLimit)
                {
                    cursor = true;
                    inputText = inputText.Substring(0, inputText.Length - 1);
                    wordIndex--;                    
                }
            }
            else
            {
                cursor = false;
                cursorChar = "";               
            }
        }
        
        if (wordIndex != 0 && inputText.StartsWith(" "))
        {
            //This is for to check is text filed blank or input is only feed with spacebar
            print("empty console");
        }

    }

    //Here you can edit button input by typing custom symbol
    public void AlphabetFunction(string alphabet)
    {
        wordIndex++;
        if (inputText != null)
        {
            alphabet = alphabet.ToLower();
            InputTextUI.fontStyle = FontStyle.Normal;
        }
        inputText = inputText + alphabet;
        InputTextUI.text = inputText;     
        
    }

    //delete last input text
    public void BackSpaceFunction()
    {
        if (inputText == null || inputText.Length <= 0)
            return;

        inputText = inputText.Substring(0, inputText.Length-1);
        InputTextUI.text = inputText;
        wordIndex--;        
    }

    //To make input text into smallcase alphabates
    public void ShiftButtonFunction()
    {
        inputText = inputText.ToLower();
        InputTextUI.text = inputText;
    }

    //Here you can parse any function or method or load scene by clickcing on confirm button
    public void ConfirmButtonFunction()
    {
        InputTextUI.text = inputText;
        OnConfirm?.Invoke();
    }
}