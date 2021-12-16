using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arrowPlatformPlayerDetector : MonoBehaviour {
    [HideInInspector] public bool touching;

    private int count;
	private void OnCollisionEnter2D(Collision2D collision) {
		count++;
		touching = true;
		Debug.Log("A");
	}
	private void OnCollisionExit2D(Collision2D collision) {
		count--;
		if (count == 0) {
			touching = false;
		}
	}
}
