using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;

    public float PlayerSpeed = 2f;

    private Vector3 moveDirection;

    private static Color color = Color.white;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        Material mat = GetComponent<MeshRenderer>().material;

        mat.color = Random.ColorHSV();
    }

    private void Update()
    {
        if (Keyboard.current[Key.W].wasPressedThisFrame)
            moveDirection = Vector3.forward;
        else if (Keyboard.current[Key.S].wasPressedThisFrame)
            moveDirection = -Vector3.forward;
        else if (Keyboard.current[Key.A].wasPressedThisFrame)
            moveDirection = Vector3.left;
        else if (Keyboard.current[Key.D].wasPressedThisFrame)
            moveDirection = Vector3.right;
    }

    public override void FixedUpdateNetwork()
    {
        // Only move own player and not every other player. Each player controls its own player object.
        if (HasStateAuthority == false)
        {
            return;
        }

        _controller.Move(moveDirection);

        if (moveDirection != Vector3.zero)
        {
            gameObject.transform.forward = moveDirection;
        }
    }
}
