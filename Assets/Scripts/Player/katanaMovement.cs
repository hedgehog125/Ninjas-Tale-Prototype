using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class katanaMovement : MonoBehaviour
{
	[Header("Objects")]
	[SerializeField] private GameObject player;
	[SerializeField] private GameObject childLight;

	[Header("Speeds")]
	[SerializeField] private float maxSpeed;
	[SerializeField] private float rotationSpeed;
	[SerializeField] private float meleeMoveSpeed;

	[Header("Recall and Return Speeds")]
	[SerializeField] private float returnAcceleration;
	[SerializeField] private float recallAcceleration;
	[SerializeField] private float recallBoost;
	[SerializeField] private float maxReturnSpeed;

	[Header("Maintainances")]
	[SerializeField] private float passedTargetMaintainance;
	[SerializeField] private float holdSpeedMaintainance;

	[Header("Time Limits")]
	[SerializeField] private int maxAge;
	[SerializeField] private int maxStuckTime;
	[SerializeField] private int maxHoldTime;

	[Header("Sounds")]
	[SerializeField] private AudioSource windSound;
	[SerializeField] private float normalWindVolume;
	[SerializeField] private float holdWindVolume;


	// Set by the player attack script
	[HideInInspector] public Vector2 target;
	[HideInInspector] public bool throwing;

	// Read by it
	[Header("")]
	[SerializeField] public float heightOffset;
	[SerializeField] public float holdHeightOffset;

	private Rigidbody2D rb;
	private BoxCollider2D col;
	private SpriteRenderer ren;

	private BoxCollider2D playerCol;
	private playerMovement playerScript;
	private playerAttack attackScript;
	

	private Vector2 speed;
	private bool hasPassedTarget;
	private bool returning;

	private int age;
	private int stuckTick;
	private int holdTick;


	private Vector2 lastPosition;
	private bool recalling;
	private bool spinDirection;


	private Vector2 GetOffset() {
		return new Vector2(((playerCol.size.x + col.size.x) / 2) * (playerScript.direction? 1 : -1), heightOffset);
	}
	private void MeleePosition() {
		transform.rotation = Quaternion.identity; // Reset the rotation
		if (playerScript.direction) {
			transform.Rotate(new Vector3(0, 0, -135));
			ren.flipX = true;
		}
		else {
			transform.Rotate(new Vector3(0, 0, 135));
			ren.flipX = false;
		}

		transform.position = (Vector2)player.transform.position + new Vector2((((playerCol.size.x + col.bounds.size.x) / 2) + (attackScript.meleeTick * meleeMoveSpeed)) * (playerScript.direction? 1 : -1), holdHeightOffset);
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		if (throwing) {
			if (hasPassedTarget) {
				if (collision.gameObject.CompareTag("PlayerCollectSmall")) {
					gameObject.SetActive(false);
				}
			}
		}	
	}

    private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<BoxCollider2D>();
		ren = GetComponent<SpriteRenderer>();

		playerCol = player.GetComponent<BoxCollider2D>();
		playerScript = player.GetComponent<playerMovement>();
		attackScript = player.GetComponent<playerAttack>();
	}

	public void MultipleStart() { // When thrown. Called by the attack script so it gets run for each throw instead of just once
		gameObject.SetActive(true);
		Vector2 position = (Vector2)player.transform.position + GetOffset();
		transform.position = position;
		lastPosition = position - new Vector2(100, 100); // Don't trigger the hit detection on the first frame
		transform.rotation = Quaternion.identity; // Reset the rotation

		if (throwing) {
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

			ren.flipX = false;
			
			hasPassedTarget = false;
			recalling = false;
			returning = false;
			age = 0;
			stuckTick = 0;
			holdTick = 0;
			spinDirection = Random.Range(0, 2) == 0;
			childLight.SetActive(true);

			windSound.volume = normalWindVolume;
			windSound.time = Random.Range(0, windSound.clip.length / 2f);
			windSound.Play();
		}
		else {
			MeleePosition();
			childLight.SetActive(false);
		}
	}

	private void ThrowTick() {
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
					windSound.Stop();
				}
				else {
					holdTick++;
				}
			}
			else {
				if (! returning) {
					age *= 2;
					returning = true;
				}
			}
		}

		lastPosition = position2;
		if (hasPassedTarget) {
			if (returning) {
				target = player.transform.position;

				Vector2 distance = target - position2;
				Vector2 direction = distance.normalized;
				speed = rb.velocity;
				speed += direction * (recalling? recallAcceleration : returnAcceleration);

				float ratio = Mathf.Abs(speed.x) / Mathf.Abs(speed.y);
				float max = Mathf.Min(Mathf.Max(Mathf.Abs(distance.magnitude) + Mathf.Abs(speed.magnitude), 1), maxReturnSpeed);
				if (Mathf.Abs(speed.x) > max || Mathf.Abs(speed.y) > max) {
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

				rb.velocity = speed;

				age++;
			}
			else {
				rb.velocity *= holdSpeedMaintainance;
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
				if (Mathf.Abs(speed.magnitude) < 0.025f) {
					speed *= 0;
					hasPassedTarget = true;
				}
			}
			rb.velocity = speed;
		}

		if (age > maxAge || stuckTick > maxStuckTime) {
			Vector2 center = attackScript.canThrowCol.bounds.center;
			Vector2 size = attackScript.canThrowCol.bounds.size;

			float distance = (size.x / 2) + 1.45f;
			Vector2Int direction = playerScript.direction? Vector2Int.right : Vector2Int.left;

			RaycastHit2D raycast = Physics2D.BoxCast(center, size, 0, direction, distance, playerScript.groundLayer);
			if (raycast.collider == null || raycast.distance > col.bounds.size.x / 2) {
				if (raycast.collider != null) {
					distance = raycast.distance;
				}

				transform.position = center + new Vector2(distance * direction.x, 0);

				if (! hasPassedTarget) {
					hasPassedTarget = true;
                }
				age = maxAge - 50;
				stuckTick = 0;
			}
			else {
				gameObject.SetActive(false);
			}
		}
	}
	private void MeleeTick() {
		MeleePosition();

		if (attackScript.meleeTick == playerScript.meleeStopTime) {
			gameObject.SetActive(false);
		}
	}

	private void PlayWindSound() {
		if (hasPassedTarget && (! returning)) {
			windSound.volume = holdWindVolume;
		}
		else {
			windSound.volume = normalWindVolume;
		}
	}

	private void FixedUpdate() {
		if (throwing) {
			ThrowTick();
			PlayWindSound();
			rb.angularVelocity = Mathf.Abs(speed.magnitude) * (spinDirection ? rotationSpeed : -rotationSpeed);
		}
		else {
			MeleeTick();
		}
	}
}
