using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arrowPlatform : MonoBehaviour {
	[Header("Objects")]
	[SerializeField] private arrowPlatformPlayerDetector detectorScript;

	[SerializeField] private Directions direction;
	[SerializeField] private float maxSpeed;
	[SerializeField] private float acceleration;

	private Rigidbody2D rb;
	private GameObject arrowChild;

	private enum Directions {
		Up,
		Down,
		Left,
		Right
	};

	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		arrowChild = transform.GetChild(0).gameObject;

		if (direction == Directions.Up) {
			arrowChild.transform.Rotate(new Vector3(0, 0, -90));
		}
		else if (direction == Directions.Down) {
			arrowChild.transform.Rotate(new Vector3(0, 0, 90));
		}
		else if (direction == Directions.Left) {
			arrowChild.transform.Rotate(new Vector3(0, 0, 180));
		}
	}

	private void FixedUpdate() {
		Vector2 vel = rb.velocity;
		float amount = acceleration * Time.deltaTime;

		if (detectorScript.touching) {
			if (direction == Directions.Up) {
				vel.y += amount;
			}
			else if (direction == Directions.Down) {
				vel.y -= amount;
			}
			else if (direction == Directions.Left) {
				vel.x -= amount;
			}
			else {
				vel.x += amount;
			}
		}

		vel.x = Mathf.Clamp(vel.x, -maxSpeed, maxSpeed);
		vel.y = Mathf.Clamp(vel.y, -maxSpeed, maxSpeed);

		rb.velocity = vel;
	}
}
