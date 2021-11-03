using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	public float moveSpeed;

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
		rb.AddForce(new Vector2(move.x * moveSpeed, jump? 5 : 0));
    }
}
