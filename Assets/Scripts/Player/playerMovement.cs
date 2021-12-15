using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class playerMovement : MonoBehaviour {
	[Header("Objects and Layers")]
	[SerializeField] public LayerMask groundLayer; // Read by player attack script
	[SerializeField] private GameObject ledgeGrabTester;
	[SerializeField] private tutorialTextController tutorialScript;

	[Header("Movement")]
	[SerializeField] private float walkAcceleration;
	[SerializeField] private float moveAirAcceleration;
	[SerializeField] private float maxWalkSpeed;
	[SerializeField] private float moveDeadzone;
	[SerializeField] private float neutralSpeedMaintenance;
	[SerializeField] private float neutralAirSpeedMaintenance;
	[SerializeField] private float turnSpeedMaintenance;
	[SerializeField] private float turnAirSpeedMultiplier;


	[Header("Jumping")]
	[SerializeField] public int coyoteTime; // Read by player attack script
	[SerializeField] private int maxJumpHoldTime;
	[SerializeField] private int maxJumpBufferTime;
	[SerializeField] private float jumpPower;
	[SerializeField] private float jumpHoldCurveSteepness;
	[SerializeField] private float jumpSpeedBoost;

	[Header("Wall Sliding and Jumping")]
	[SerializeField] private float downFallBoost;
	[SerializeField] private float wallSlidingSpeed;
	[SerializeField] private float minSlideTriggerSpeed; // On the X axis, only when neutral
	[SerializeField] private float wallJumpPowerX;
	[SerializeField] private float wallJumpPowerY;
	[SerializeField] private int wallJumpPreventBackwardsTime;

	[Header("Ledge Grabbing")]
	[SerializeField] private float ledgeGrabDistance;
	[SerializeField] private float ledgeGrabAcceleration;
	[SerializeField] private float ledgeGrabMaxSpeed;

	// These are katana related and are only used in the attack script but they're movement related so the values are set here (also why they're public)
	[Header("Katana Movement")]
	[SerializeField] public float throwHeightBoost;
	[SerializeField] public int throwTime;
	[SerializeField] public float throwMomentumCancelMultiplier; // Backwards and neutral
	[SerializeField] public float throwMomentumReduceMultiplier; // Forwards

	// Same here
	[Header("Melee Movement")]
	[SerializeField] public float meleeAirXBoost;
	[SerializeField] public float meleeAirYBoost;
	[SerializeField] public float meleeGroundBoost;
	[SerializeField] public float meleeInitialAirBoostMultiplier;
	[SerializeField] public float meleeInitialGroundBoostMultiplier;
	[SerializeField] public int meleeBoostTime;
	[SerializeField] public float meleeAfterBoostMaintainance;
	[SerializeField] public int meleeStopTime;


	[Header("Sounds")]
	[SerializeField] private AudioSource landGrassSound;
	[SerializeField] private AudioSource fallSound;
	[SerializeField] private AudioSource jumpSound;



	private Rigidbody2D rb;
	private Collider2D col;
	private BoxCollider2D ledgeCol;
	private playerAttack attackScript;
	private playerDamage damageScript;


	// Needs to be read by the visual child and the player attack script
	[HideInInspector] public float yAcceleration { get; private set; }
	[HideInInspector] public bool moveInputNeutralX { get; private set; } = true;
	[HideInInspector] public bool moveInputNeutralY { get; private set; } = true;
	[HideInInspector] public bool wasOnWall { get; private set; }
	[HideInInspector] public bool wasOnGround { get; private set; }
	[HideInInspector] public int jumpHoldTick { get; private set; }

	// Read by attack script
	[HideInInspector] public bool canControl { get; private set; }

	// Modified by visual child
	[HideInInspector] public bool direction = true;

	private Vector2 moveInput;
	[HideInInspector] public bool jumpInput { get; private set; } // Read by tutorial boxes
	private bool jumpBufferInput; // Set to false after jumping

	[HideInInspector] public int coyoteTick; // Modified by attack script
	private int jumpBufferTick;
	private bool hasJumped;

	private bool wallSlideDirection;
	private bool wallSlideBuffered;

	private bool wallJumpDirection;
	private bool hasWallJumped;
	private float wallJumpHeight;
	[HideInInspector] public int wallJumpCoyoteTick; // Modified by attack script
	private bool wallJumpPendingDirection;
	private int wallJumpPreventBackwardsTick;
	private bool touchedWallSinceGround;

	private bool ledgeGrabbing;
	private float ledgeGrabX;
	private float ledgeGrabY;
	private bool ledgeGrabStage;
	private int ledgeGrabTick;

	private float normalGravity;
	private Vector2 velWas;

	private void OnMove(InputValue input) {
		moveInput = input.Get<Vector2>();

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
		jumpInput = input.isPressed;
		jumpBufferInput = jumpInput;
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		GameObject ob = collision.gameObject;
		loadingZoneController itsLoadngController = ob.GetComponent<loadingZoneController>();
		if (itsLoadngController != null) {
			SceneManager.LoadScene(itsLoadngController.target);
			return;
		}

		warpController itsWarpController = ob.GetComponent<warpController>();
		if (itsWarpController != null) {
			transform.position = itsWarpController.target;
		}

		checkpointController itsCheckpointController = ob.GetComponent<checkpointController>();
		if (itsCheckpointController != null) {
			damageScript.respawnLocation = ob.transform.position;
		}
	}


	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<Collider2D>();
		ledgeCol = ledgeGrabTester.GetComponent<BoxCollider2D>();
		ledgeCol.size = new Vector2(col.bounds.size.x, 0.1f);

		attackScript = GetComponent<playerAttack>();
		damageScript = GetComponent<playerDamage>();

		normalGravity = rb.gravityScale;
	}
	private bool DetectGrounded() {
		Vector2 center = col.bounds.center;
		center.y -= 0.02f;
		Vector2 size = col.bounds.size;
		size.x -= 0.05f;
		size.y -= 0.02f;

		Collider2D obCollider = Physics2D.BoxCast(center, size, 0, Vector2.down, 0.02f, groundLayer).collider;
		return obCollider != null;
    }
	private bool[] DetectWallSlideTick(bool isOnGround) {
		Vector2 center = col.bounds.center;
		Vector2 size = col.bounds.size;

		bool[] outputs = new bool[2];
		RaycastHit2D raycastCenter = Physics2D.BoxCast(center, size, 0, direction? Vector2.right : Vector2.left, 0.1f, groundLayer);
		if (raycastCenter.collider == null) return outputs;


		bool movingTowards = true;
		if (! (wasOnWall || wallSlideBuffered)) {
			if (moveInputNeutralX && Mathf.Abs(rb.velocity.x) < minSlideTriggerSpeed) movingTowards = false;
			else if ((! moveInputNeutralX) && moveInput.x > 0 != direction) movingTowards = false; // Not moving towards the wall
		}

		if ((! touchedWallSinceGround) && movingTowards) {
			landGrassSound.volume = Mathf.Abs(velWas.x) / 30f;
			landGrassSound.Play();
			touchedWallSinceGround = true;
		}

		// These early returns are slightly later here so the sound can still play if there's contact
		if (isOnGround) {
			wallSlideBuffered = false;
			wallJumpCoyoteTick = coyoteTime;
			return outputs;
		}
		if (attackScript.meleeTick != 0) return outputs;

		if (! ledgeCol.IsTouchingLayers(groundLayer)) {
			RaycastHit2D raycastTop = Physics2D.BoxCast(ledgeCol.bounds.center, ledgeCol.bounds.size, 0, Vector2.up, 3, groundLayer);
 			float space = raycastTop.collider == null? 3 : raycastTop.distance;

			raycastTop = Physics2D.BoxCast(ledgeCol.bounds.center, ledgeCol.bounds.size, 0, Vector2.down, 3, groundLayer);
			space += raycastTop.distance;

			if (space >= size.y) {
				outputs[1] = true; // Can climb to here
				ledgeGrabX = ledgeCol.bounds.center.x;
				ledgeGrabY = (ledgeCol.bounds.center.y - raycastTop.distance) + 0.02f + (col.bounds.size.y / 2);
			}
		}

		bool canSlide = movingTowards;
		if (canSlide && hasWallJumped) { // Extra requirements if the player has wall jumped since touching the ground
			if (direction == wallJumpDirection && transform.position.y > wallJumpHeight) { // The player last jumped off this wall from a lower height, can't wall jump
				canSlide = false;
			}
		}

		if (! canSlide) return outputs;

		bool buffered = jumpBufferTick != 0 && jumpBufferTick < maxJumpBufferTime;
		if (rb.velocity.y > 0 && (! ((jumpInput && (jumpHoldTick == 0 || jumpHoldTick == maxJumpHoldTime)) || buffered))) {
			wallSlideBuffered = true;
			return outputs;
        }

		wallSlideBuffered = false;
		hasJumped = false;
		outputs[0] = true;
		wallJumpCoyoteTick = 0;
		wallJumpPendingDirection = direction;

		return outputs;
	}

	private bool GroundDetectTick() {
		bool isOnGround = DetectGrounded();
		if (isOnGround) {
			coyoteTick = 0;
			touchedWallSinceGround = false;
			if (! wasOnGround) { // Landed
				landGrassSound.volume = -velWas.y / 75f;
				landGrassSound.Play();
				fallSound.Stop();
			}
		}
		else {
			fallSound.volume = Mathf.Min(-velWas.y / 100f, 0.2f);
			if ((! fallSound.isPlaying) && rb.velocity.y < -1) {
				fallSound.time = Random.Range(0, fallSound.clip.length / 2f);
				fallSound.Play();
			}
		}
		if ((! isOnGround) && coyoteTick < coyoteTime) {
			isOnGround = true;
			coyoteTick++;
		}
		if (isOnGround) {
			hasJumped = false;
			hasWallJumped = false;
		}
		wasOnGround = isOnGround;

		return isOnGround;
	}
    private void MoveTick(ref Vector2 vel, bool isOnGround, ref bool isOnWall, bool reduceToAirTurnSpeed) {
		if ((! isOnGround) && (! moveInputNeutralY)) {
			if (isOnWall) {
				wasOnWall = false;
				isOnWall = false; // Only for this frame
				rb.gravityScale = normalGravity;
				transform.position = new Vector2(transform.position.x + (direction? -0.025f : 0.025f), transform.position.y); // Move away from the wall slightly to stop friction

				// So you have to press down again to fall faster
				moveInputNeutralY = true;
				moveInput.y = 0;
			}
			else if (moveInput.y < 0) {
				vel.y -= downFallBoost;
			}
		}

		bool canMove = true;
		if (isOnWall) { // Can't move towards the wall
			if (moveInput.x > 0) {
				if (wallSlideDirection) canMove = false;
			}
			else if (! wallSlideDirection) canMove = false;
		}
		if (attackScript.throwTick != 0) { // Can only move in the direction the katana was thrown
			if (moveInput.x > 0) {
				if (! attackScript.throwDirection) canMove = false;
			}
			else if (attackScript.throwDirection) canMove = false;
		}

		float xInput = moveInput.x;
		if (wallJumpPreventBackwardsTick != 0) { // You have to move away from the wall
			xInput = wallJumpDirection? -1 : 1;
			wallJumpPreventBackwardsTick--;
		}

		if (attackScript.meleeTick != 0) {
			canMove = false;
        }


		if (canMove) {
			if (isOnGround) {
				vel.x += xInput * walkAcceleration;
			}
			else {
				vel.x += xInput * (moveAirAcceleration * (reduceToAirTurnSpeed? turnAirSpeedMultiplier : 1));
			}
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
					coyoteTick = coyoteTime;
					jumpBufferInput = false;

					jumpSound.Play();
				}
				else {
					jumpHoldTick++;
					if (jumpHoldTick == maxJumpHoldTime) {
						jumpInput = false;
					}
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
			if (jumpBufferInput && (! isOnGround)) {
				if (jumpBufferTick == maxJumpBufferTime) {
					jumpBufferInput = false;
					jumpBufferTick = 0;
				}
				else {
					jumpBufferTick++;
                }
			}
		}
		else if ((! isOnGround) && jumpHoldTick != 0) {
			jumpHoldTick = maxJumpHoldTime; // The boost doesn't apply to any extra hold frames
		}
	}
	private void WallTick(ref Vector2 vel, bool isOnGround, bool isOnWall, bool canLedgeGrab) {
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
		else if (isOnWall || (wallJumpCoyoteTick != 0 && wallJumpCoyoteTick < coyoteTime)) {
			bool buffered = jumpBufferTick != 0 && jumpBufferTick < maxJumpBufferTime;
			if ((jumpInput && (jumpHoldTick == 0 || jumpHoldTick == maxJumpHoldTime)) || buffered) {
				hasWallJumped = true;
				wallJumpDirection = wallJumpPendingDirection;
				wallJumpHeight = transform.position.y;

				jumpBufferTick = 0;
				wallJumpCoyoteTick = coyoteTime;

				jumpInput = false;
				jumpBufferInput = false;

				if (canLedgeGrab) {
					ledgeGrabbing = true;
					ledgeGrabStage = false;
					ledgeGrabTick = 0;
					rb.gravityScale = 0;

					vel.x = 0;
					vel.y = 0;
				}
				else {
					vel.x = wallJumpPendingDirection? -wallJumpPowerX : wallJumpPowerX;
					if (vel.y < 0) vel.y = 0;
					vel.y += wallJumpPowerY;

					wallJumpPreventBackwardsTick = wallJumpPreventBackwardsTime;
					rb.gravityScale = normalGravity;
				}
			}
		}
    }

	public void LedgeGrabStateTick(ref Vector2 vel) {
		if (transform.position.y > ledgeGrabY) {
			ledgeGrabStage = true;
			rb.gravityScale = normalGravity;
			transform.position = new Vector3(transform.position.x, ledgeGrabY);
		}
		if (ledgeGrabStage) {
			ledgeGrabTick++;
			if (ledgeGrabY - transform.position.y >= 2 || ledgeGrabTick == 150) { // Fail-safe
				ledgeGrabbing = false;
				Debug.Log("A");
			}
			vel.x = wallJumpDirection? Mathf.Min(vel.x + (ledgeGrabAcceleration / 2), ledgeGrabMaxSpeed / 2) : Mathf.Max(vel.x - (ledgeGrabAcceleration / 2), -(ledgeGrabMaxSpeed / 2));
			if (wallJumpDirection) {
				if (transform.position.x > ledgeGrabX) {
					ledgeGrabbing = false;
				}
			}
			else {
				if (transform.position.x < ledgeGrabX) {
					ledgeGrabbing = false;
				}
			}
		}
		else {
			vel.y = Mathf.Min(vel.y + ledgeGrabAcceleration, ledgeGrabMaxSpeed);
		}
	}
	public void NormalStateTick(ref Vector2 vel, ref bool isOnGround, ref bool isOnWall, ref bool canLedgeGrab) {
		bool turning = false;
		if (moveInputNeutralX && attackScript.meleeTick == 0) {
			if (isOnGround) {
				vel.x *= neutralSpeedMaintenance;
			}
			else {
				vel.x *= neutralAirSpeedMaintenance;
			}
		}
		else if (direction != moveInput.x > 0 && Mathf.Abs(vel.x) > 1) {
			turning = true;
			if (isOnGround) {
				vel.x *= turnSpeedMaintenance;
			}
		}

		MoveTick(ref vel, isOnGround, ref isOnWall, turning);
		if (! isOnWall) {
			JumpTick(ref vel, isOnGround);
		}
		WallTick(ref vel, isOnGround, isOnWall, canLedgeGrab);

		if ((! isOnWall) && wallJumpCoyoteTick < coyoteTime) {
			wallJumpCoyoteTick++;
		}
	}

    private void FixedUpdate() {
		Vector2 vel = new Vector2(rb.velocity.x, rb.velocity.y);
		yAcceleration = (vel - velWas).y;
		canControl = (! tutorialScript.blocking) && damageScript.stunTick == 0;

		bool isOnGround = GroundDetectTick();
		bool[] outputs = DetectWallSlideTick(isOnGround);
		bool isOnWall = outputs[0];
		bool canLedgeGrab = outputs[1];

		if (canControl) {
			if (ledgeGrabbing) {
				LedgeGrabStateTick(ref vel);
			}
			else {
				NormalStateTick(ref vel, ref isOnGround, ref isOnWall, ref canLedgeGrab);
			}
		}
		
		if (attackScript.meleeTick == 0) {
			rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxWalkSpeed) * Mathf.Sign(vel.x), vel.y);
        }
		else {
			rb.velocity = vel;
        }
		velWas = vel;
	}

	public void LateUpdate() {
		ledgeCol.offset = new Vector2(Mathf.Round(col.bounds.center.x) + (direction? 0.25f : -0.25f), col.bounds.center.y + ledgeGrabDistance) - new Vector2(col.bounds.center.x, col.bounds.center.y);
	}
}
