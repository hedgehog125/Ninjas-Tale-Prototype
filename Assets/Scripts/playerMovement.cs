using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	[SerializeField] private float moveSpeed;
	//[SerializeField] private float maxSpeed;

	private Vector2 move;
	private bool isJumping;

	private Rigidbody2D rb;
	private Collider2D col;
	private SpriteRenderer ren;

	private List<GameObject> onground = new List<GameObject>();

	void OnMove(InputValue movementValue) {
		move = movementValue.Get<Vector2>();
	}
	void OnJump(InputValue input) {
		isJumping = input.Get<float>() > 0;
	}


	void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
		ren = GetComponent<SpriteRenderer>();

	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Standable")) {
			float bottomY = transform.position.y - (col.bounds.size.y / 4);
			bool below = false;
			int count = collision.contactCount;
			for (int i = 0; i < count; i++) {
				Vector2 point = collision.GetContact(i).point;
				if (point.y <= bottomY) { // Point of contact has to be in the bottom quarter.
					below = true;
					break;
				}
			}
			if (below) {
				onground.Add(collision.gameObject);
			}
		}

	}
	void OnCollisionExit2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Standable")) {
			onground.Remove(collision.gameObject);
		}
	}

	void FixedUpdate() {
		// This gives a less physics-y feel compared to addForce and means movements don't last as long

		/*
		Vector2 newVel = rb.velocity;
		newVel.x += move.x * moveSpeed;
		if (Mathf.Abs(newVel.x) > maxSpeed) newVel.x = maxSpeed * Mathf.Sign(newVel.x);

		if (jump) {
			newVel.y += 2;
		}

		rb.velocity = newVel;
		*/
		//rb.AddForce(new Vector2(move.x * moveSpeed, 0));

		Vector2 newVel = new Vector2(move.x * moveSpeed, rb.velocity.y);
		if (isJumping && onground.Count != 0) {
			newVel.y += 5;
		}
		rb.velocity = newVel;

		// Flipping left/right
		if (Mathf.Abs(rb.velocity.x) > 0.1f) {
			ren.flipX = rb.velocity.x < 0;
		}
	}
}
