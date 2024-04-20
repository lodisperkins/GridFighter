using System;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

public class LocalInput : MonoBehaviour {
    
  private void OnEnable() {
    QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
  }

  public void PollInput(CallbackPollInput callback) {
    Quantum.Input i = new Quantum.Input(); 
    callback.SetInput(i, DeterministicInputFlags.Repeatable);
  }
}
