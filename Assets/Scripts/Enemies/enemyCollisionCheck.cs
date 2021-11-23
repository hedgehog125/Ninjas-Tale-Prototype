using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyCollisionCheck : MonoBehaviour {
	[HideInInspector] public bool inCollider { get; private set; }

	private int countInCollider;

	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.gameObject.name == "PlayerWas") {
			countInCollider++;
			inCollider = true;
		}
	}
	private void OnTriggerExit2D(Collider2D collision) {
		if (collision.gameObject.name == "PlayerWas") {
			countInCollider--;
			inCollider = countInCollider != 0;
		}
	}
}
