using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using Quantum;
using TMPro;
using UnityEngine.UI;

public class InputHandler : MonoBehaviour {

    private void OnEnable() {
        QuantumCallback.Subscribe(this, (CallbackPollInput callbackInput) => GetInput(callbackInput));
    }

    public void GetInput(CallbackPollInput callbackInput) {
        Quantum.Input input = new Quantum.Input();

        input.Block = UnityEngine.Input.GetButton("Jump");

        Vector2 inputDirection = Vector2.zero;
        inputDirection.x = UnityEngine.Input.GetAxis("Horizontal");
        inputDirection.y = UnityEngine.Input.GetAxis("Vertical");

        input.InputVectorX = (short)(inputDirection.x * 10);
        input.InputVectorY = (short)(inputDirection.y * 10);

        callbackInput.SetInput(input, DeterministicInputFlags.Repeatable);
    }
}
