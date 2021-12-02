using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class tutorialTextController : MonoBehaviour {
    [Header("Objects")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject okPrompt;

    [Header("Timings")]
    [SerializeField] private int okPromptTime;

    private int okPromptTick;
    private bool okPressed;
    [HideInInspector] public bool active { get; private set; }

    private void OnSubmit(InputValue value) {
        okPressed = value.isPressed;
    }

    private void Awake() {
        Display("My name jeff");
    }

    private void FixedUpdate() {
        if (okPromptTick == okPromptTime) {
            okPrompt.SetActive(true);
            if (okPressed) {
                gameObject.SetActive(false);
                active = false;
            }
        }
        else {
            okPromptTick++;
        }
    }

    public void Display(string explaination) {
        gameObject.SetActive(true);
        text.text = explaination;
        okPrompt.SetActive(false);

        okPromptTick = 0;
        active = true;
    }
}
