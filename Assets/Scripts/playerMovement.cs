using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private float moveSpeed;
	[SerializeField] private int coyoteTime;
	[SerializeField] private int maxJumpHoldTime;
	[SerializeField] private int maxJumpBufferTime;
	[SerializeField] private float jumpPower;
	[SerializeField] private float jumpHoldCurveSteepness; 
	[SerializeField] private float jumpSpeedBoost;
	[SerializeField] private float downFallBoost;
	[SerializeField] private float wallJumpPowerX;
	[SerializeField] private float wallJumpPowerY;


	private Vector2 moveInput;
	private bool jumpInput;

	private Rigidbody2D rb;
	private Collider2D col;

	private int coyoteTick;
	private int jumpHoldTick;
	private int jumpBufferTick;
	private bool hasJumped;
	private bool wasOnGround;

	private bool wallJumpDirection;
	private bool hasWallJumped;
	private float wallJumpHeight;

	private float normalGravity;


	// Modified by visual child
	public bool direction = true;

	private void OnMove(InputValue movementValue) {
		moveInput = movementValue.Get<Vector2>();
	}
	private void OnJump(InputValue input) {
		jumpInput = input.Get<float>() > 0;
	}


	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();

		normalGravity = rb.gravityScale;
	}
	private bool DetectGrounded() {
		Collider2D obCollider = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.down, 0.1f, groundLayer).collider;
		return obCollider != null;
    }
	private bool DetectWallSlide() {
		if (rb.velocity.y > 0.1f) return false;
		Collider2D obCollider = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, rb.velocity.x < 0? Vector2.left : Vector2.right, 0.1f, groundLayer).collider;
		return obCollider != null;
	}
	private bool DetectCanWallJump(bool willWallSlide) {
		if (! willWallSlide) return false;

		// Make sure the player isn't turning
		if (Mathf.Abs(rb.velocity.x) < 0.2f) return true;
		if (rb.velocity.x > 0) {
			if (! direction) return false;
        }
		else {
			if (direction) return false;
		}
		return true;
	}

	private bool GroundDetectTick() {
		bool isOnGround = DetectGrounded();
		if (isOnGround) {
			coyoteTick = 0;
		}
		if ((! isOnGround) && coyoteTick < coyoteTime) {
			isOnGround = true;
			coyoteTick++;
		}
		if (isOnGround != wasOnGround) {
			wasOnGround = isOnGround;
			if (isOnGround) {
				hasJumped = false;
			}
        }

		return isOnGround;
	}
    private void MoveTick(ref Vector2 newVel, bool isOnGround) {
		newVel.x += moveInput.x * moveSpeed;
		if ((! isOnGround) && moveInput.y < -0.3) {
			newVel.y -= downFallBoost;
		}
	}
	private void JumpTick(ref Vector2 newVel, bool isOnGround) {
		if (jumpInput || (jumpBufferTick != 0 && jumpBufferTick < maxJumpBufferTime)) {
			if ((isOnGround && (! hasJumped)) || jumpHoldTick < maxJumpHoldTime) {
				if (isOnGround && (! hasJumped)) {
					jumpHoldTick = 1;
					if (! jumpInput) { // Buffered
						jumpBufferTick = maxJumpBufferTime;
					}
				}
				else {
					jumpHoldTick++;
				}
				hasJumped = true;
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
	}
	private void WallTick(ref Vector2 newVel, bool isOnGround, bool isOnWall, bool canWallJump) {
		if (isOnWall) {
			rb.gravityScale = 0.25f;
		}
		else {
			rb.gravityScale = normalGravity;
        }


		if (isOnGround) {
			hasWallJumped = false;
		}
		else if (isOnWall) {
			canWallJump = canWallJump && (! hasWallJumped);
			if (! canWallJump) {
				if (direction != wallJumpDirection) { // Opposite wall
					canWallJump = true;
                }
				else if (transform.position.y <= wallJumpHeight) { // Fallen below last jump point
					canWallJump = true;
                }
            }

			if (jumpInput && canWallJump) {
				rb.gravityScale = normalGravity;
				newVel.x = direction? -wallJumpPowerX : wallJumpPowerX;
				newVel.y += wallJumpPowerY;
			}
		}
    }

	private void FixedUpdate() {
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

		Vector2 newVel = new Vector2(0, rb.velocity.y);
		bool isOnGround = GroundDetectTick();
		bool isOnWall = DetectWallSlide();
		bool canWallJump = DetectCanWallJump(isOnWall);

		MoveTick(ref newVel, isOnGround);
		JumpTick(ref newVel, isOnGround);
		WallTick(ref newVel, isOnGround, isOnWall, canWallJump);
		
		rb.velocity = newVel;
	}
}
