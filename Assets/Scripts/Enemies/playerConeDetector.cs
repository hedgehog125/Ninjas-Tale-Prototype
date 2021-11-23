using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerConeDetector : MonoBehaviour {
	[HideInInspector] public bool inCone { get; private set; }
	[HideInInspector] public bool inLargeCone; // Set by the large cone child

	private int countInCone;
	private bool isLarge;
	private playerConeDetector parentConeDetector;

    private void Awake() {
		isLarge = gameObject.name == "VisionConeLarge";
		if (isLarge) {
			parentConeDetector = transform.parent.gameObject.GetComponent<playerConeDetector>();
        }
	}

    private void OnTriggerEnter2D(Collider2D collision) {
		countInCone++;
		inCone = true;
		if (isLarge) {
			parentConeDetector.inLargeCone = true;
        }
	}
	private void OnTriggerExit2D(Collider2D collision) {
		countInCone--;
		inCone = countInCone != 0;
		if (isLarge) {
			parentConeDetector.inLargeCone = inCone;
        }
	}
}
