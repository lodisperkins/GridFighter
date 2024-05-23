using System;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

public class LocalInput : MonoBehaviour
{

    private PlayerControls controls;
    private EntityComponentGridPanel gridPanel;
    private EntityComponentGridPanel gridPanel2;
    [SerializeField] private EntityPrototype entity;
    [SerializeField] private EntityPrototype entity2;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Attack.started += context => { Debug.Log("Jump"); };

        gridPanel = entity.GetComponent<EntityComponentGridPanel>();
        gridPanel.Prototype.X = 5;
        Debug.Log(gridPanel.Prototype.X);

        gridPanel2 = entity.GetComponent<EntityComponentGridPanel>();
        gridPanel2.Prototype.X = 2;
        Debug.Log(gridPanel2.Prototype.X);
    }

    private void OnEnable()
    {
        controls.Enable();
        QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    public void PollInput(CallbackPollInput callback)
    {
        Quantum.Input inputHandler = new Quantum.Input();

        inputHandler.Jump = Convert.ToBoolean(controls.Player.Attack.ReadValue<float>());


        Vector2 moveDirection = controls.Player.Move.ReadValue<Vector2>();

        inputHandler.Direction = moveDirection.ToFPVector2();

        callback.SetInput(inputHandler, DeterministicInputFlags.Repeatable);
    }
}
