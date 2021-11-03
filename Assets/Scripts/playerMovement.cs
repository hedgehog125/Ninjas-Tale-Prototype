using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	[SerializeField] private float moveSpeed;
	//[SerializeField] private float maxSpeed;

	private Vector2 move;
	private bool jump;
	private Rigidbody2D rb;


	void Awake() {
		rb = GetComponent<Rigidbody2D>();
	}

	void OnMove(InputValue movementValue) {
		move = movementValue.Get<Vector2>();
	}

	void OnJump(InputValue input) {
		jump = input.Get<float>() > 0;
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
		if (jump) {
			newVel.y += 1;
		}
		rb.velocity = newVel;
	}
}
