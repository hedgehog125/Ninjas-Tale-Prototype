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
	private BoxCollider2D col;
	private katanaMovement katanaScript;
	private Camera cam;
	private LayerMask groundLayer;

	private bool throwInput;
	public Vector2 targetInput = new Vector2(0, 0);

	private int throwBufferTick;


	private void OnThrow(InputValue input) {
		throwInput = input.isPressed;
	}
	private void OnAim(InputValue input) {
		targetInput = input.Get<Vector2>();
	}
	private void OnAimMove(InputValue input) {
		targetInput += input.Get<Vector2>();
	}

	private void Awake() {
		moveScript = GetComponent<playerMovement>();
		col = GetComponent<BoxCollider2D>();

		katanaScript = katana.GetComponent<katanaMovement>();
		cam = cameraObject.GetComponent<Camera>();
		groundLayer = moveScript.groundLayer;

		BoxCollider2D katanaCol = katana.GetComponent<BoxCollider2D>();
		canThrowCol.size = new Vector2(katanaScript.playerOffset.x, katanaCol.size.y);
	}

	private void FixedUpdate() {
		if (throwInput || (throwBufferTick != 0 && throwBufferTick < maxThrowBufferTime)) {
			if (katana.activeSelf) { // Already thrown, attempt to buffer
				throwBufferTick++;
				if (throwBufferTick == maxThrowBufferTime) {
					throwBufferTick = 0;
					throwInput = false; // Drop the input, it has to be timed properly
				}
			}
			else { // Might be able to throw
				katanaScript.target = cam.ScreenToWorldPoint(targetInput);

				if (! canThrowCol.IsTouchingLayers(groundLayer)) {
					RaycastHit2D raycast = Physics2D.BoxCast(canThrowCol.bounds.center, canThrowCol.bounds.size, 0, moveScript.direction? Vector2.right : Vector2.left, minThrowDistance - 0.05f, groundLayer);
					if (raycast.collider == null) {
						katanaScript.MultipleStart();
						throwInput = false;
					}
				}

				throwBufferTick = 0;
				throwInput = false;
			}
		}
	}

	private void LateUpdate() {
		canThrowCol.offset = new Vector2(((col.size.x + canThrowCol.size.x) / 2) * (moveScript.direction? 1 : -1), katanaScript.playerOffset.y);
	}
}
