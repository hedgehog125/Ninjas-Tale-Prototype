using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class playerAttack : MonoBehaviour
{
	[SerializeField] private GameObject katana;
	[SerializeField] private GameObject cameraObject;

	[SerializeField] private int maxThrowBufferTime;
	[SerializeField] private float minThrowDistance;

	private playerMovement moveScript;
	private katanaMovement katanaScript;
	private Camera cam;

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
		katanaScript = katana.GetComponent<katanaMovement>();
		cam = cameraObject.GetComponent<Camera>();
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
			else { // Can throw
				katanaScript.target = cam.ScreenToWorldPoint(targetInput);
				if (Mathf.Abs(Vector2.Distance(katanaScript.target, new Vector2(transform.position.x, transform.position.y))) >= minThrowDistance) {
					katanaScript.MultipleStart();
					throwInput = false;
				}

				throwBufferTick = 0;
				throwInput = false;
			}
		}
	}
}
