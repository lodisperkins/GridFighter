using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class KeyboardFunction : MonoBehaviour
{
    public string inputText = ""; // Stores the typed input
    private int wordIndex = 0; // Tracks input length
    public Text InputTextUI = null; // UI Text field
    public Button backSpaceBtn;
    public Button confirmBtn;
    public Button capsBtn;
    public int textLimit = 20; // Character limit for input

    private float m_TimeStamp; // Used for cursor blinking timing
    private bool cursor = false; // Cursor toggle state
    private string cursorChar = ""; // Cursor display character
    [SerializeField] private UnityEvent _onConfirm; // Confirm event
    private bool isCaps = false; // Tracks Caps Lock state

    public UnityEvent OnConfirm { get => _onConfirm; private set => _onConfirm = value; }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateUIText();
        BlinkCursor();
    }

    /// <summary>
    /// Handles real-time keyboard input using Unity's Input System
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Keyboard.current != null)
        {
            foreach (KeyControl key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    string keyValue = key.displayName;

                    // Handle alphanumeric key presses
                    if (keyValue.Length == 1 && char.IsLetterOrDigit(keyValue[0]))
                    {
                        AlphabetFunction(isCaps ? keyValue.ToUpper() : keyValue.ToLower());
                    }
                    else if (key == Keyboard.current.backspaceKey)
                    {
                        BackSpaceFunction();
                    }
                    else if (key == Keyboard.current.enterKey)
                    {
                        ConfirmButtonFunction();
                    }
                    else if (key == Keyboard.current.spaceKey)
                    {
                        AlphabetFunction(" ");
                    }
                    else if (key == Keyboard.current.capsLockKey)
                    {
                        ToggleCaps();
                    }
                }
            }
        }
    }


    /// <summary>
    /// Updates the UI Text with current input and cursor
    /// </summary>
    private void UpdateUIText()
    {
        if (wordIndex > 0)
            InputTextUI.text = inputText + cursorChar;
        else
        {
            inputText = "";
            InputTextUI.fontStyle = FontStyle.Italic;
            InputTextUI.text = "type here ";
        }

        confirmBtn.interactable = wordIndex >= 1;
    }

    /// <summary>
    /// Simulates a blinking cursor effect
    /// </summary>
    private void BlinkCursor()
    {
        if (Time.time - m_TimeStamp >= 0.5f)
        {
            m_TimeStamp = Time.time;
            cursor = !cursor;
            cursorChar = cursor ? "|" : "";
        }
    }

    /// <summary>
    /// Adds typed character to input text
    /// </summary>
    public void AlphabetFunction(string alphabet)
    {
        if (wordIndex >= textLimit) return;

        inputText += alphabet;
        wordIndex++;
        InputTextUI.fontStyle = FontStyle.Normal;
        InputTextUI.text = inputText;
    }

    /// <summary>
    /// Deletes the last character from input text
    /// </summary>
    public void BackSpaceFunction()
    {
        if (inputText.Length > 0)
        {
            inputText = inputText.Substring(0, inputText.Length - 1);
            wordIndex--;
            InputTextUI.text = inputText;
        }
    }

    /// <summary>
    /// Toggles capitalization mode
    /// </summary>
    public void ToggleCaps()
    {
        isCaps = !isCaps;
    }

    /// <summary>
    /// Confirms input and invokes an event
    /// </summary>
    public void ConfirmButtonFunction()
    {
        InputTextUI.text = inputText;
        OnConfirm?.Invoke();
    }

    /// <summary>
    /// Clears the input text
    /// </summary>
    public void ClearText()
    {
        inputText = "";
        wordIndex = 0;
    }
}