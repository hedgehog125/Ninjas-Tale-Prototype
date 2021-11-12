using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class katanaMovement : MonoBehaviour
{
	[SerializeField] private GameObject player;
	[SerializeField] private GameObject visibleKatana;

	[SerializeField] private float maxSpeed;
	[SerializeField] private float rotationSpeed;

	[SerializeField] private float passedTargetMaintainance;
	[SerializeField] private int maxAge;
	[SerializeField] private int maxStuckTime;

	[SerializeField] private float returnAcceleration;



	// Set by the player attack script
	public Vector2 target;

	// Read by it
	public float heightOffset;

	private Rigidbody2D rb;
	private BoxCollider2D col;
	private BoxCollider2D playerCol;
	private playerMovement playerScript;

	private Vector2 speed;
	private bool hasPassedTarget;
	private int age;
	private int stuckTick;
	private Vector2 lastPosition;
	private bool spinDirection;
	private float spinVelocity;

	private Vector2 GetOffset() {
		return new Vector2(((playerCol.size.x + col.size.x) / 2) * (playerScript.direction? 1 : -1), 1);
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		if (hasPassedTarget) {
			if (collision.gameObject.CompareTag("PlayerCollectSmall")) {
				gameObject.SetActive(false);
				return;
            }
        }
	}

    private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<BoxCollider2D>();

		playerCol = player.GetComponent<BoxCollider2D>();
		playerScript = player.GetComponent<playerMovement>();
	}

	public void MultipleStart() { // When thrown. Called by the attack script so it gets run for each throw instead of just once
		gameObject.SetActive(true);
		Vector2 position = (Vector2)player.transform.position + GetOffset();
		transform.position = position;
		lastPosition = position - new Vector2(100, 100); // Don't trigger the hit detection on the first frame

		Vector2 distance = target - position;
		Vector2 signs = new Vector2(Mathf.Sign(distance.x), Mathf.Sign(distance.y));
		float ratio = Mathf.Abs(distance.x) / Mathf.Abs(distance.y);

		if (ratio > 1) {
			speed.x = maxSpeed;
			speed.y = maxSpeed / ratio;
		}
		else {
			speed.x = maxSpeed * ratio;
			speed.y = maxSpeed;
		}
		speed *= signs;

		hasPassedTarget = false;
		age = 0;
		stuckTick = 0;
		spinDirection = Random.Range(0, 2) == 0;
	}

	private void FixedUpdate() {
		Vector2 position2 = transform.position;
		if (Mathf.Abs(Vector2.Distance(position2, lastPosition)) < 0.1f) {
			stuckTick++;
			if (! hasPassedTarget) {
				hasPassedTarget = true;
				speed *= 0;
            }
        }
		lastPosition = position2;
		if (hasPassedTarget) {
			target = (Vector2)player.transform.position + GetOffset();
			Vector2 direction = (target - position2).normalized;
			rb.velocity += direction * returnAcceleration;
			speed = rb.velocity;
		}
		else {
			bool slowDown = false;
			if (speed.x > 0) {
				if (transform.position.x > target.x) {
					slowDown = true;
				}
			}
			else {
				if (transform.position.x < target.x) {
					slowDown = true;
				}
			}

			if (slowDown) {
				speed *= passedTargetMaintainance;
				if (Mathf.Abs(speed.magnitude) < 0.2f) {
					speed *= 0;
					hasPassedTarget = true;
                }
			}
			rb.velocity = speed;
		}

		spinVelocity += spinDirection? rotationSpeed : -rotationSpeed;
		visibleKatana.transform.Rotate(0, 0, spinVelocity);

		age++;
		if (age > maxAge || stuckTick > maxStuckTime) {
			gameObject.SetActive(false);
        }
	}
}
