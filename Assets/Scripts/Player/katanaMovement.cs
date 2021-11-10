using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class katanaMovement : MonoBehaviour
{
	[SerializeField] private GameObject player;

	[SerializeField] private float maxSpeed;
	[SerializeField] private Vector2 playerOffset;

	// Set by the player attack script
	public Vector2 target;

	private Rigidbody2D rb;
	private playerMovement playerScript;

	private Vector2 speed;


	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		playerScript = player.GetComponent<playerMovement>();
	}

	private void Start() { // When thrown
		Vector2 position = new Vector2(player.transform.position.x, player.transform.position.y) + (playerOffset * new Vector2Int(playerScript.direction? 1 : -1, 0));
		transform.position = position;

		Vector2 distance = target - position;
		Vector2 signs = new Vector2(Mathf.Sign(distance.x), Mathf.Sign(distance.y));
		float ratio = Mathf.Abs(distance.x) / Mathf.Abs(distance.y);

		if (ratio > 1) {
			speed.x = maxSpeed;
			speed.y = maxSpeed / ratio;
		}
		else {
			speed.x = maxSpeed * ratio;
			speed.y = maxSpeed;
		}
		speed *= signs;
	}

	private void FixedUpdate() {
		rb.velocity = speed;
	}
}
