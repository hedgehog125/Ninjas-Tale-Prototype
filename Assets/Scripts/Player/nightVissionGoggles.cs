using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class nightVissionGoggles : MonoBehaviour {
    [SerializeField] private GameObject goggles;
    private bool gogglesInput;

    private nightVisionAnimation gogglesScript;

    private void Awake() {
        gogglesScript = goggles.GetComponent<nightVisionAnimation>();  
    }

    private void OnNightVision(InputValue input) {
        gogglesInput = input.isPressed;
    }

    private void FixedUpdate() {
        if (! gogglesScript.animating) {
            if (gogglesInput) {
                gogglesScript.Toggle();

                gogglesInput = false;
            }
        }
    }
}
