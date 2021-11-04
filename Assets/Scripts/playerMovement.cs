using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	[SerializeField] private float moveSpeed;
	[SerializeField] private int coyoteTime;
	[SerializeField] private int maxJumpHoldTime;
	[SerializeField] private int maxJumpBufferTime;
	[SerializeField] private float jumpPower;
	[SerializeField] private float jumpHoldCurveSteepness; 
	[SerializeField] private float jumpSpeedBoost;
	[SerializeField] private float downFallBoost;


	private Vector2 moveInput;
	private bool jumpInput;

	private Rigidbody2D rb;
	private Collider2D col;

	public List<GameObject> onground = new List<GameObject>();
	public List<GameObject> notBelow = new List<GameObject>();
	private int coyoteTick;
	public int jumpHoldTick;
	public int jumpBufferTick;

	void OnMove(InputValue movementValue) {
		moveInput = movementValue.Get<Vector2>();
	}
	void OnJump(InputValue input) {
		jumpInput = input.Get<float>() > 0;
	}


	void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
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
			else {
				notBelow.Add(collision.gameObject);
            }
		}

	}
	void OnCollisionStay2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Standable") && notBelow.Contains(collision.gameObject)) {
			notBelow.Remove(collision.gameObject);
			OnCollisionEnter2D(collision);
		}
	}
	void OnCollisionExit2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Standable")) {
			onground.Remove(collision.gameObject);
			notBelow.Remove(collision.gameObject);
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

		Vector2 newVel = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);

		bool isOnGround = onground.Count != 0;
		if (isOnGround) {
			coyoteTick = 0;
		}
		if ((! isOnGround) && coyoteTick < coyoteTime) {
			isOnGround = true;
			coyoteTick++;
		}

		if (jumpInput || (jumpBufferTick != 0 && jumpBufferTick < maxJumpBufferTime)) {
			if (isOnGround || jumpHoldTick < maxJumpHoldTime) {
				if (isOnGround) {
					jumpHoldTick = 1;
					if (! jumpInput) { // Buffered
						jumpBufferTick = maxJumpBufferTime;
					}
                }
				else {
					jumpHoldTick++;
				}
				newVel.y += (jumpPower / (
					Mathf.Sqrt(
						jumpHoldTick * jumpHoldCurveSteepness
					)
					- (jumpHoldCurveSteepness - 1)
				)) * ((Mathf.Abs(rb.velocity.x) * jumpSpeedBoost) + 1);
				coyoteTick = coyoteTime;
			}
			else {
				if (jumpBufferTick == maxJumpBufferTime) {
					jumpInput = false;
                }
				jumpBufferTick++;
            }
		}
		else {
			jumpBufferTick = 0;
			jumpHoldTick = maxJumpBufferTime;
		}

		if ((! isOnGround) && moveInput.y < -0.3) {
			newVel.y -= downFallBoost;
        }
		rb.velocity = newVel;
	}
}
