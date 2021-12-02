using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerAttack : MonoBehaviour {
	[Header("Objects and Colliders")]
	[SerializeField] private GameObject katana;
	[SerializeField] private GameObject cameraObject;
	[SerializeField] public BoxCollider2D canThrowCol; // Read by katana movement

	[Header("Melee")]
	[SerializeField] public bool enableKatana; // Can be enabled by the katana item

	[Header("Throwing")]
	[SerializeField] private bool enableThrowing;
	[SerializeField] private int maxThrowBufferTime;
	[SerializeField] private float minThrowDistance;

	[Header("Sounds")]
	[SerializeField] private AudioSource katanaTakeOutSound;
	[SerializeField] private AudioSource katanaPutAwaySound;
	[SerializeField] private AudioSource katanaThrowSound;


	private playerMovement moveScript;
	private Rigidbody2D rb;
	private BoxCollider2D col;
	private katanaMovement katanaScript;
	private Camera cam;
	private LayerMask groundLayer;

	// Read by player movement
	[HideInInspector] public int throwTick { get; private set; }
	[HideInInspector] public bool throwDirection { get; private set; }
	[HideInInspector] public int meleeTick { get; private set; } // And by enemies and the katana

	// Read by katana
	[HideInInspector] public Vector2 targetInput { get; private set; } = new Vector2(0, 0);
	[HideInInspector] public Vector2 moveInput { get; private set; }
	[HideInInspector] public bool recallInput { get; private set; }
	[HideInInspector] public bool throwHoldInput { get; private set; } // Isn't set to false when katana is thrown

	private bool throwInput;
	private bool meleeInput;


	private int throwBufferTick;
	private bool thrownSinceGround;
	private bool attackedSinceGround;
	private bool getHeight;

	private bool punching;


	private void OnThrow(InputValue input) {
		throwInput = input.isPressed;
		throwHoldInput = input.isPressed;
	}
	private void OnRecall(InputValue input) {
		recallInput = input.isPressed;
	}
	private void OnAim(InputValue input) {
		targetInput = input.Get<Vector2>();
	}
	private void OnAimMove(InputValue input) {
		targetInput += input.Get<Vector2>();
	}
	private void OnMove(InputValue input) {
		moveInput = input.Get<Vector2>();
	}
	private void OnMelee(InputValue input) {
		meleeInput = input.isPressed;
	}

	private void Awake() {
		moveScript = GetComponent<playerMovement>();
		col = GetComponent<BoxCollider2D>();
		rb = GetComponent<Rigidbody2D>();

		katanaScript = katana.GetComponent<katanaMovement>();
		cam = cameraObject.GetComponent<Camera>();
		groundLayer = moveScript.groundLayer;

		BoxCollider2D katanaCol = katana.GetComponent<BoxCollider2D>();

		canThrowCol.size = new Vector2(col.size.x, katanaCol.size.y);
		canThrowCol.offset = new Vector2(0, katanaScript.heightOffset);
	}

	private void KatanaThrowTick(ref Vector2 vel) {
		if (throwTick == 0) {
			if (throwInput || (throwBufferTick != 0 && throwBufferTick < maxThrowBufferTime)) {
				if (katana.activeSelf) { // Already thrown, attempt to buffer
					throwBufferTick++;
					if (throwBufferTick == maxThrowBufferTime) {
						throwBufferTick = 0;
						throwInput = false; // Drop the input, it has to be timed properly
					}
				}
				else if (! moveScript.wasOnWall) { // Might be able to throw
					Vector2 target = cam.ScreenToWorldPoint(targetInput);

					bool directionWas = moveScript.direction;
					bool thrown = false;

					moveScript.direction = target.x > transform.position.x;
					if (! canThrowCol.IsTouchingLayers(groundLayer)) {
						RaycastHit2D raycast = Physics2D.BoxCast(canThrowCol.bounds.center, canThrowCol.bounds.size, 0, moveScript.direction ? Vector2.right : Vector2.left, (minThrowDistance + ((col.size.x + canThrowCol.size.x) / 2)) - 0.05f, groundLayer);
						if (raycast.collider == null) {
							if (target.x > transform.position.x) {
								if (target.x < canThrowCol.bounds.center.x + (canThrowCol.bounds.size.x / 2) + minThrowDistance) {
									target.x = canThrowCol.bounds.center.x + (canThrowCol.bounds.size.x / 2) + minThrowDistance;
								}
							}
							else {
								if (target.x > canThrowCol.bounds.center.x - (canThrowCol.bounds.size.x / 2) - minThrowDistance) {
									target.x = canThrowCol.bounds.center.x - (canThrowCol.bounds.size.x / 2) - minThrowDistance;
								}
							}
							katanaScript.target = target;


							if (moveScript.moveInputNeutralX || katanaScript.target.x > transform.position.x != directionWas) {
								vel.x *= moveScript.throwMomentumCancelMultiplier * (moveScript.moveInputNeutralX ? 1 : -1);
							}
							else {
								vel.x *= moveScript.throwMomentumReduceMultiplier;
							}
							if (! thrownSinceGround) { // To prevent flight
								if (vel.y < 0) {
									vel.y = 0;
								}
								else {
									vel.y *= moveScript.throwMomentumReduceMultiplier;
								}
							}

							throwTick = 1;
							throwDirection = moveScript.direction;
							thrown = true;
							getHeight = ! (thrownSinceGround || moveScript.wasOnGround);
							thrownSinceGround = true;
							if (! moveScript.wasOnGround) {
								moveScript.coyoteTick = moveScript.coyoteTime;
								moveScript.wallJumpCoyoteTick = moveScript.coyoteTime;
							}

							katanaScript.throwing = true;
							katanaScript.MultipleStart();
						}
					}

					if (! thrown) {
						moveScript.direction = directionWas;
					}

					throwBufferTick = 0;
					throwInput = false;
				}
			}
		}
		if (throwTick != 0) {
			if (getHeight) {
				if (! moveScript.wasOnGround) {
					vel.y += moveScript.throwHeightBoost;
				}
			}

			if (throwTick == moveScript.throwTime) {
				throwTick = 0;
			}
			else {
				throwTick++;
			}
		}
	}

	private void MeleeAttackTick(ref Vector2 vel) { // Smash Melee reference? :0
		if (meleeTick == 0) {
			if (meleeInput) {
				if (! (moveScript.wasOnWall || attackedSinceGround)) {
					if (moveScript.wasOnGround) {
						vel.x = (moveScript.direction? moveScript.meleeGroundBoost : -moveScript.meleeGroundBoost) * moveScript.meleeInitialGroundBoostMultiplier;
					}
					else {
						vel.x = (moveScript.direction? moveScript.meleeAirXBoost : -moveScript.meleeAirXBoost) * moveScript.meleeInitialAirBoostMultiplier;
						vel.y = moveScript.meleeAirYBoost * moveScript.meleeInitialAirBoostMultiplier;
					}
					attackedSinceGround = true;
					meleeTick = 1;
					meleeInput = false;

					punching = katana.activeSelf;
					if (punching) {
						
					}
					else {
						katanaScript.throwing = false;
						katanaScript.MultipleStart();
						katanaTakeOutSound.Play();
					}
				}
			}
		}
		else {
			if (meleeTick < moveScript.meleeBoostTime) {
				if (moveScript.wasOnGround) {
					vel.x = (moveScript.direction? moveScript.meleeGroundBoost : -moveScript.meleeGroundBoost) * moveScript.meleeInitialGroundBoostMultiplier;
				}
				else {
					vel.x += moveScript.direction? moveScript.meleeAirXBoost : -moveScript.meleeAirXBoost;
					vel.y += moveScript.meleeAirYBoost;
				}
			}
			else {
				vel.x *= moveScript.meleeAfterBoostMaintainance;
			}
			if (meleeTick == moveScript.meleeStopTime) {
				meleeTick = 0;
			}
			else {
				if (meleeTick == moveScript.meleeStopTime - 10) {
					katanaPutAwaySound.Play();
				}
				meleeTick++;
            }
        }
    }
	private void FixedUpdate() {
		Vector2 vel = rb.velocity;

		if (moveScript.wasOnGround || moveScript.wasOnWall) {
			thrownSinceGround = false;
			attackedSinceGround = false;
		}
		if (enableKatana) {
			if (enableThrowing) {
				KatanaThrowTick(ref vel);
			}
			MeleeAttackTick(ref vel);
		}

		rb.velocity = vel;
	}
}
