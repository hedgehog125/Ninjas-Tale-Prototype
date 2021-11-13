using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class katanaMovement : MonoBehaviour
{
	[SerializeField] private GameObject player;
	[SerializeField] private GameObject visibleKatana;

	[SerializeField] private float maxSpeed;
	[SerializeField] private float rotationSpeed;

	[SerializeField] private float passedTargetMaintainance;
	[SerializeField] private int maxAge;
	[SerializeField] private int maxStuckTime;
	[SerializeField] private int maxHoldTime;

	[SerializeField] private float returnAcceleration;
	[SerializeField] private float recallAcceleration;
	[SerializeField] private float recallBoost;
	[SerializeField] private float minReturnSpeed;
	[SerializeField] private float maxReturnSpeed;

	// TODO: implement max return speed and vary spin speed based on velocity <============ Needs debugging as well



	// Set by the player attack script
	public Vector2 target;

	// Read by it
	public float heightOffset;

	private Rigidbody2D rb;
	private BoxCollider2D col;
	private BoxCollider2D playerCol;
	private playerMovement playerScript;
	private playerAttack attackScript;
	

	private Vector2 speed;
	private bool hasPassedTarget;
	private bool returning;
	private bool hasReachedMin;

	private int age;
	private int stuckTick;
	private int holdTick;


	private Vector2 lastPosition;
	private bool recalling;
	private bool spinDirection;


	private Vector2 GetOffset() {
		return new Vector2(((playerCol.size.x + col.size.x) / 2) * (playerScript.direction? 1 : -1), heightOffset);
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
		attackScript = player.GetComponent<playerAttack>();
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
		visibleKatana.transform.rotation = Quaternion.identity; // Reset the rotation


		hasPassedTarget = false;
		recalling = false;
		returning = false;
		age = 0;
		stuckTick = 0;
		holdTick = 0;
		spinDirection = Random.Range(0, 2) == 0;
	}

	private void FixedUpdate() {
		Vector2 position2 = transform.position;

		if (Mathf.Abs(Vector2.Distance(position2, lastPosition)) < 0.1f && ((! hasPassedTarget) || returning)) {
			stuckTick++;
			if (! hasPassedTarget) {
				hasPassedTarget = true;
				rb.velocity *= 0;
			}
		}

		if (attackScript.recallInput) {
			if (! recalling) {
				if (hasPassedTarget) {
					rb.velocity += rb.velocity.normalized * recallBoost;
				}
				else {
					Vector2 direction = rb.velocity.normalized;
					rb.velocity *= 0;
					rb.velocity -= direction * recallBoost;
					hasPassedTarget = true;
				}
				recalling = true;
			}
		}
		if (hasPassedTarget) {
			if (attackScript.throwHoldInput) {
				if (holdTick == maxHoldTime) {
					returning = true;
				}
				else {
					holdTick++;
				}
			}
			else {
				returning = true;
			}
		}

		lastPosition = position2;
		if (hasPassedTarget) {
			if (returning) {
				target = (Vector2)player.transform.position + GetOffset();

				Vector2 distance = target - position2;
				Vector2 direction = distance.normalized;
				speed = rb.velocity;
				speed += direction * (recalling? recallAcceleration : returnAcceleration);

				float ratio = Mathf.Abs(speed.x) / Mathf.Abs(speed.y);
				if (Mathf.Abs(speed.x) < minReturnSpeed || Mathf.Abs(speed.y) < minReturnSpeed) {
					if (hasReachedMin) {
						Debug.Log("A");
						Vector2 signs = new Vector2(Mathf.Sign(speed.x), Mathf.Sign(speed.y));
						speed = new Vector2(Mathf.Abs(speed.x), Mathf.Abs(speed.y));

						if (ratio > 1) {
							speed.x = minReturnSpeed;
							speed.y = minReturnSpeed / ratio;
						}
						else {
							speed.x = minReturnSpeed * ratio;
							speed.y = minReturnSpeed;
						}
						speed *= signs;
					}
				}
				else {
					//hasReachedMin = true;
					float max = Mathf.Max(Mathf.Abs(distance.magnitude) * 10, maxReturnSpeed) * 1000;
					if (Mathf.Abs(speed.x) > max || Mathf.Abs(speed.y) > max) {
						Debug.Log("B");
						Vector2 signs = new Vector2(Mathf.Sign(speed.x), Mathf.Sign(speed.y));
						speed = new Vector2(Mathf.Abs(speed.x), Mathf.Abs(speed.y));

						if (ratio > 1) {
							speed.x = max;
							speed.y = max / ratio;
						}
						else {
							speed.x = max * ratio;
							speed.y = max;
						}
						speed *= signs;
					}
				}

				rb.velocity = speed;

				age++;
			}
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

		if (age > maxAge || stuckTick > maxStuckTime) {
			gameObject.SetActive(false);
		}

		visibleKatana.transform.Rotate(0, 0, Mathf.Abs(speed.magnitude) * (spinDirection? rotationSpeed : -rotationSpeed));
	}
}
