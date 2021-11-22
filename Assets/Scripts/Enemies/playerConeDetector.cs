using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerConeDetector : MonoBehaviour {
	[HideInInspector] public bool inCone { get; private set; }

	private int countInCone;

    private void OnTriggerEnter2D(Collider2D collision) {
		countInCone++;
		inCone = true;
	}
	private void OnTriggerExit2D(Collider2D collision) {
		countInCone--;
		inCone = countInCone != 0;
	}
}
