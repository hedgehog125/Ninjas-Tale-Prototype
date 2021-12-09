using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class tutorialTextController : MonoBehaviour {
    [Header("Objects")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject okPrompt;
	[SerializeField] private TextMeshProUGUI smallText;
	[SerializeField] private GameObject smallContainer;

	[Header("Timings")]
    [SerializeField] private int okPromptTime;

	private playerMovement playerScript;


    private int okPromptTick;
    private bool okPressed;
	private bool okHasPressed;
	private bool small;
	private string currentID;
    [HideInInspector] public bool active { get; private set; }
	[HideInInspector] public bool blocking { get; private set; }

    private void Awake() {
		playerScript = GameObject.Find("Player").GetComponent<playerMovement>();
	}

    private void FixedUpdate() {
		okPressed = playerScript.jumpInput;
		if ((! small) && active) {
			if (okPromptTick == okPromptTime) {
				okPrompt.SetActive(true);
				if (okPressed) {
					okHasPressed = true;
				}
				else if (okHasPressed) {
					gameObject.SetActive(false);
					blocking = false;
					active = false;
				}
			}
			else {
				okPromptTick++;
			}
		}
    }

    public void Display(string message, bool isSmall, string id) {
		if (! active) {
			small = isSmall;
			if (small) {
				smallContainer.SetActive(true);
				smallText.text = message;
			}
			else {
				gameObject.SetActive(true);
				text.text = message;
				blocking = true;
			}
			okPrompt.SetActive(false);


			okPromptTick = 0;
			active = true;
			okPressed = false;
			okHasPressed = false;
			currentID = id;
		}
		
    }
	public void DeactivateIfSmall(string id) {
		if (small && active && id == currentID) {
			smallContainer.SetActive(false);
			active = false;
		}
	}
}
