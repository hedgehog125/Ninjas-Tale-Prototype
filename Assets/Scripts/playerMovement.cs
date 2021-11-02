using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	public float moveSpeed;

	private Vector2 move;
	private Rigidbody2D rb;
	void Awake() {
		rb = GetComponent<Rigidbody2D>();
	}

	void OnMove(InputValue movementValue) {
		move = movementValue.Get<Vector2>();
	}

	void FixedUpdate() {
		rb.AddForce(new Vector2(move.x * moveSpeed, 0));
    }
}
