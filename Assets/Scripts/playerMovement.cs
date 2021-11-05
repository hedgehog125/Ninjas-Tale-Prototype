using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private float walkAcceleration;
	[SerializeField] private float maxWalkSpeed;
	[SerializeField] private float moveDeadzone;
	[SerializeField] private int coyoteTime;
	[SerializeField] private int maxJumpHoldTime;
	[SerializeField] private int maxJumpBufferTime;
	[SerializeField] private float jumpPower;
	[SerializeField] private float jumpHoldCurveSteepness; 
	[SerializeField] private float jumpSpeedBoost;
	[SerializeField] private float downFallBoost;
	[SerializeField] private float wallSlidingSpeed;
	[SerializeField] private float wallJumpPowerX;
	[SerializeField] private float wallJumpPowerY;


	private Vector2 moveInput;
	private bool moveInputNeutralX = true;
	private bool moveInputNeutralY = true;
	private bool jumpInput;

	private Rigidbody2D rb;
	private Collider2D col;

	private int coyoteTick;
	private int jumpHoldTick;
	private int jumpBufferTick;
	private bool hasJumped;
	private bool wasOnGround;

	private bool wasOnWall;
	private bool wallSlideDirection;
	private bool wallJumpDirection;
	private bool hasWallJumped;
	private float wallJumpHeight;

	private float normalGravity;


	// Modified by visual child
	public bool direction = true;

	private void OnMove(InputValue movementValue) {
		moveInput = movementValue.Get<Vector2>();

		moveInputNeutralX = Mathf.Abs(moveInput.x) < moveDeadzone;
		if (moveInputNeutralX) {
			moveInput.x = 0;
		}
		moveInputNeutralY = Mathf.Abs(moveInput.y) < moveDeadzone;
		if (moveInputNeutralY) {
			moveInput.y = 0;
		}
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
		Collider2D obCollider = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, Vector2.down, 0.02f, groundLayer).collider;
		return obCollider != null;
    }
	private bool DetectWallSlideTick() {
		if (rb.velocity.y > 0) return false;
		Collider2D obCollider = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, direction? Vector2.right : Vector2.left, 0.1f, groundLayer).collider;
		if (obCollider == null) return false;

		if (! wasOnWall) {
			if (moveInputNeutralX) return false;
			if (moveInput.x > 0 != direction) return false; // Not moving towards the wall
		}

		hasJumped = false;
		return true;
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
    private void MoveTick(ref Vector2 vel, bool isOnGround, bool isOnWall) {
		if ((! isOnGround) && moveInput.y < -moveDeadzone) {
			vel.y -= downFallBoost;
		}

		bool canMove = true;
		if (isOnWall) { // You can't move towards the wall
			if (moveInput.x > 0) {
				if (wallSlideDirection) canMove = false;
			}
			else if (! wallSlideDirection) canMove = false;
		}
		if (canMove) {
			vel.x += moveInput.x * walkAcceleration;
		}
	}
	private void JumpTick(ref Vector2 vel, bool isOnGround) {
		bool buffered = jumpBufferTick != 0 && jumpBufferTick < maxJumpBufferTime;
		if (jumpInput || buffered) {
			bool justStarted = isOnGround && (! hasJumped);
			if (justStarted || jumpHoldTick < maxJumpHoldTime) {
				if (justStarted) {
					jumpHoldTick = 1;
					jumpBufferTick = 0;
					if (! jumpInput) { // Unbuffer
						jumpBufferTick = maxJumpBufferTime;
					}
				}
				else {
					jumpHoldTick++;
				}

				hasJumped = true;
				vel.y += (jumpPower / (
					Mathf.Sqrt(
						jumpHoldTick * jumpHoldCurveSteepness
					)
					- (jumpHoldCurveSteepness - 1)
				)) * ((Mathf.Abs(rb.velocity.x) * jumpSpeedBoost) + 1);
				coyoteTick = coyoteTime;

			}
			if (! isOnGround) {
				if (jumpBufferTick == maxJumpBufferTime) {
					jumpInput = false;
				}
				jumpBufferTick++;
			}
		}

		if (! jumpInput) {
			jumpBufferTick = 0;
			jumpHoldTick = maxJumpHoldTime;
		}
	}
	private void WallTick(ref Vector2 vel, bool isOnGround, bool isOnWall, bool canWallJump) {
		if (isOnWall != wasOnWall) {
			if (isOnWall) {
				rb.gravityScale = wallSlidingSpeed;
				vel.y = 0;
				wallSlideDirection = vel.x > 0;
			}
			else {
				rb.gravityScale = normalGravity;
			}
			wasOnWall = isOnWall;
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
				vel.x = direction? -wallJumpPowerX : wallJumpPowerX;
				vel.y += wallJumpPowerY;
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

		Vector2 vel = new Vector2(rb.velocity.x, rb.velocity.y);
		bool isOnGround = GroundDetectTick();
		bool isOnWall = DetectWallSlideTick();
		bool canWallJump = DetectCanWallJump(isOnWall);

		MoveTick(ref vel, isOnGround, isOnWall);
		JumpTick(ref vel, isOnGround);
		WallTick(ref vel, isOnGround, isOnWall, canWallJump);

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxWalkSpeed) * Mathf.Sign(vel.x), vel.y);
	}
}
