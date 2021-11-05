using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour {
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private float walkAcceleration;
	[SerializeField] private float maxWalkSpeed;
	[SerializeField] private float moveDeadzone;
	[SerializeField] private float neutralSpeedMaintenance;
	[SerializeField] private float turnSpeedMaintenance;


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
	[SerializeField] private int wallJumpPreventBackwardsTime;


	// Needs to be read by the visual child
	public Vector2 moveInput;
	public bool moveInputNeutralX = true;
	public bool moveInputNeutralY = true;
	private bool jumpInput;

	private Rigidbody2D rb;
	private Collider2D col;

	private int coyoteTick;
	private int jumpHoldTick;
	private int jumpBufferTick;
	private bool hasJumped;

	public bool wasOnWall;
	public bool wasOnGround;

	private bool wallSlideDirection;
	private bool wallJumpDirection;
	private bool hasWallJumped;
	private float wallJumpHeight;
	private int wallJumpPreventBackwardsTick;

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
		RaycastHit2D raycast = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0, direction? Vector2.right : Vector2.left, 0.1f, groundLayer);
		if (raycast.collider == null) return false;
		float distance = Mathf.Max(raycast.distance - 0.05f, 0);


		bool canSlide = true;
		if (! wasOnWall) {
			if (moveInputNeutralX) canSlide = false;
			else if (moveInput.x > 0 != direction) canSlide = false; // Not moving towards the wall
		}
		if (canSlide && hasWallJumped) { // Extra requirements if the player has wall jumped since touching the ground
			if (direction == wallJumpDirection && transform.position.y > wallJumpHeight) { // The player last jumped off this wall from a lower height, can't wall jump
				canSlide = false;
			}
		}

		if (! canSlide) {
			// To stop friction
			distance = raycast.distance;
			return false;
		}
		transform.position = new Vector2(transform.position.x + (direction ? -distance : distance), transform.position.y);
		if (! canSlide) return false;

		hasJumped = false;
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
				hasWallJumped = false;
			}
        }

		return isOnGround;
	}
    private void MoveTick(ref Vector2 vel, bool isOnGround, ref bool isOnWall) {
		if ((! isOnGround) && (! moveInputNeutralY)) {
			if (isOnWall) {
				wasOnWall = false;
				isOnWall = false; // Only for this frame
				rb.gravityScale = normalGravity;
				transform.position = new Vector2(transform.position.x + (direction ? -0.025f : 0.025f), transform.position.y); // Move away from the wall slightly to stop friction

				// So you have to press down again to fall faster
				moveInputNeutralY = true;
				moveInput.y = 0;
			}
			else {
				vel.y -= downFallBoost;
			}
		}

		bool canMove = true;
		if (isOnWall || wallJumpPreventBackwardsTick != 0) { // You can't move towards the wall
			if (moveInput.x > 0) {
				if (wallSlideDirection) canMove = false;
			}
			else if (! wallSlideDirection) canMove = false;
			if (wallJumpPreventBackwardsTick != 0) {
				wallJumpPreventBackwardsTick--;
			}
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
	private void WallTick(ref Vector2 vel, bool isOnGround, bool isOnWall) {
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
			wallJumpPreventBackwardsTick = 0;
		}
		else if (isOnWall) {
			if (jumpInput) {
				rb.gravityScale = normalGravity;
				vel.x = direction? -wallJumpPowerX : wallJumpPowerX;
				vel.y += wallJumpPowerY;

				hasWallJumped = true;
				wallJumpDirection = direction;
				wallJumpHeight = transform.position.y;
				wallJumpPreventBackwardsTick = wallJumpPreventBackwardsTime;
			}
		}
    }

	private void FixedUpdate() {
		Vector2 vel = new Vector2(rb.velocity.x, rb.velocity.y);
		if (moveInputNeutralX) {
			vel.x *= neutralSpeedMaintenance;
		}
		else if (direction != moveInput.x > 0) {
			vel.x *= turnSpeedMaintenance;
		}

		bool isOnGround = GroundDetectTick();
		bool isOnWall = DetectWallSlideTick();

		MoveTick(ref vel, isOnGround, ref isOnWall);
		JumpTick(ref vel, isOnGround);
		WallTick(ref vel, isOnGround, isOnWall);

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxWalkSpeed) * Mathf.Sign(vel.x), vel.y);
	}
}
