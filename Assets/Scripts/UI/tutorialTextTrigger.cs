using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tutorialTextTrigger : MonoBehaviour {
	[SerializeField] private string message;
	[SerializeField] private int triggerTime;
	[SerializeField] private bool small;


	private int triggerTick;
	private bool playerInTrigger;
	private tutorialTextController controller;

	private void Awake() {
		controller = GameObject.Find("UI").transform.GetChild(2).gameObject.GetComponent<tutorialTextController>();	
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			playerInTrigger = true;
		}
	}

	private void OnTriggerExit2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			playerInTrigger = false;
		}
	}

	private void FixedUpdate() {
		if (playerInTrigger) {
			if (triggerTick == triggerTime) {
				controller.Display(message, small, gameObject.name);

				triggerTick = 0;
			}
			else {
				triggerTick++;
			}
		}
		else {
			triggerTick = 0;
			controller.DeactivateIfSmall(gameObject.name);
		}
	}
}
