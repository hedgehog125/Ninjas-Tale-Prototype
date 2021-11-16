using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class nightVisionGoggles : MonoBehaviour {
    [Header("Game Objects")]
    [SerializeField] private GameObject goggles;

    // Also read by the controller script
    [Header("When Active")]
    [SerializeField] public float lightGlobalIntensity;
    [SerializeField] public float lightMultiplier;

    [Header("Colours When Active")]
    [SerializeField] public Color lightGlobalColor;
    [SerializeField] public Color lightSkyColor;

    private bool gogglesInput;

    private nightVisionAnimation gogglesScript;

    [HideInInspector] public bool active;

    private void Awake() {
        gogglesScript = goggles.GetComponent<nightVisionAnimation>();

        if (lightGlobalColor.a != 1) {
            Debug.LogWarning("Global light colour is at least partly transparent.");
        }
        if (lightSkyColor.a != 1) {
            Debug.LogWarning("Sky light colour is at least partly transparent.");
        }
    }

    private void OnNightVision(InputValue input) {
        gogglesInput = input.isPressed;
    }

    private void FixedUpdate() {
        if (! gogglesScript.animating) {
            if (gogglesInput) {
                active = ! active;
                gogglesScript.Toggle();

                gogglesInput = false;
            }
        }
    }
}
