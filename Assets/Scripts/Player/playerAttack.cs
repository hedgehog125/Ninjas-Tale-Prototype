using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerAttack : MonoBehaviour
{
	[SerializeField] private GameObject katana;
	[SerializeField] private GameObject cameraObject;
	[SerializeField] private BoxCollider2D canThrowCol;

	[SerializeField] private int maxThrowBufferTime;
	[SerializeField] private float minThrowDistance;

	private playerMovement moveScript;
	private Rigidbody2D rb;
	private BoxCollider2D col;
	private katanaMovement katanaScript;
	private Camera cam;
	private LayerMask groundLayer;

	// Read by player movement
	public int throwTick;
	public bool throwDirection;

	private bool throwInput;
	public Vector2 targetInput = new Vector2(0, 0);
	public Vector2 moveInput;
	public bool recallInput; // Read by katana

	private int throwBufferTick;


	private void OnThrow(InputValue input) {
		throwInput = input.isPressed;
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

	private void FixedUpdate() {
		Vector2 vel = rb.velocity;

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
					katanaScript.target = cam.ScreenToWorldPoint(targetInput);

					bool directionWas = moveScript.direction;
					bool thrown = false;

					moveScript.direction = katanaScript.target.x > transform.position.x;
					if (! canThrowCol.IsTouchingLayers(groundLayer)) {
						RaycastHit2D raycast = Physics2D.BoxCast(canThrowCol.bounds.center, canThrowCol.bounds.size, 0, moveScript.direction? Vector2.right : Vector2.left, (minThrowDistance + ((col.size.x + canThrowCol.size.x) / 2)) - 0.05f, groundLayer);
						if (raycast.collider == null) {
							if (moveScript.moveInputNeutralX || katanaScript.target.x > transform.position.x != directionWas) {
								vel.x *= moveScript.throwMomentumCancelMultiplier * (moveScript.moveInputNeutralX? 1 : -1);
							}
							else {
								vel.x *= moveScript.throwMomentumReduceMultiplier;
							}
							if (vel.y < 0) {
								vel.y = 0;
							}
							else {
								vel.y *= moveScript.throwMomentumReduceMultiplier;
							}

							throwTick = 1;
							throwDirection = moveScript.direction;
							thrown = true;

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
			if (! moveScript.wasOnGround) {
				vel.y += moveScript.throwHeightBoost;
			}

			if (throwTick == moveScript.throwTime) {
				throwTick = 0;
			}
			else {
				throwTick++;
			}
		}

		rb.velocity = vel;
	}
}
