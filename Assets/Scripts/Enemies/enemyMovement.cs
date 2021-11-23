using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyMovement : MonoBehaviour {
	[Header("Objects and References")]
	[SerializeField] private GameObject playerObject;
	[SerializeField] private GameObject visionCone;
	[SerializeField] private LayerMask groundLayer;

	[Header("")]
	[SerializeField] private List<Vector2Int> patrolPath;
	[SerializeField] private int delayTime;

	[Header("Speed")]
	[SerializeField] private float acceleration;
	[SerializeField] private float stopMaintainance;
	[SerializeField] private float maxSpeed;

	[Header("")]
	[SerializeField] private float jumpHeight;

	[Header("Stealth")]
	[SerializeField] private int spotTime;
	[SerializeField] private float spotCancelSpeed;


	[HideInInspector] public bool direction { get; private set; } // Read by enemy visible script

	private Rigidbody2D rb;
	private BoxCollider2D col;

	private enemyAlerter alertScript;
	private playerConeDetector coneScript;

	private int currentPoint;
	private int playerTouching;
	private Vector2 knownPlayerPosition;
	private Vector2 lastPosition;

	private int delayTick;
	private float spotTick;

    private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			playerTouching++;
        }
	}
	private void OnCollisionExit2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			playerTouching--;
		}
	}


	private enum States {
		Default,
		Searching,
		Attacking
    }
	private States state;

    private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<BoxCollider2D>();

		alertScript = playerObject.GetComponent<enemyAlerter>();
		coneScript = visionCone.GetComponent<playerConeDetector>();
    }

    private void Start() {
		state = States.Default;
		if (patrolPath.Count >= 2) {
			currentPoint = 1;
			Vector2 nextPoint = patrolPath[currentPoint] + new Vector2(0.5f, 0.25f);
			direction = nextPoint.x > transform.position.x;
		}
	}

	private void DefaultState(ref Vector2 vel) {
		if (patrolPath.Count >= 2) {
			if (spotTick > 0) {
				vel.x *= stopMaintainance;
			}
			else {
				if (delayTick == 0) {
					Vector2 nextPoint = patrolPath[currentPoint] + new Vector2(0.5f, 0.25f);
					if (MoveTick(ref vel, nextPoint)) { // Passed target
						if (Mathf.Abs(vel.x) < 0.1f) {
							delayTick = 1;
						}
					}
				}
				else {
					if (delayTick == delayTime || delayTime == 0) {
						delayTick = 0;
						currentPoint++;
						if (currentPoint == patrolPath.Count) {
							currentPoint = 0;
						}
						direction = patrolPath[currentPoint].x > transform.position.x;
					}
					else {
						delayTick++;
					}
				}
			}
		}
    }
	private void DetectPlayer(ref Vector2 vel) {
		if (playerTouching != 0) {
			Spotted(ref vel);
		}
		else if (coneScript.inCone && alertScript.inLight) {
			Vector2 distance = playerObject.transform.position - transform.position;
			Vector2 castDirection = distance.normalized;

			Vector2 position = visionCone.transform.position;
			RaycastHit2D hit = Physics2D.Raycast(position, castDirection, distance.magnitude + 0.05f, groundLayer);
			if (hit.collider == null) { // There's line of sight
				knownPlayerPosition = playerObject.transform.position;
				spotTick++;
				if (spotTick >= spotTime) {
					Spotted(ref vel);
				}
			}
			else {
				spotTick -= spotCancelSpeed;
				if (spotTick < 0) spotTick = 0;
			}
		}
		else {
			spotTick--;
			if (spotTick < 0) spotTick = 0;
		}
	}

	private void AttackState(ref Vector2 vel) {
		direction = knownPlayerPosition.x > transform.position.x;

		if (Mathf.Abs(transform.position.x - knownPlayerPosition.x) > 1.5f) {
			if (Mathf.Abs(transform.position.x - lastPosition.x) <= 0.01f) { // Hit an obstacle
				Vector2 center = col.bounds.center;
				Vector2 size = col.bounds.size;
				size.x -= 0.05f;
				size.y = 0.1f;
				center.y += 0.05f - (col.bounds.size.y / 2);

				RaycastHit2D hit = Physics2D.BoxCast(center, size, 0, Vector2.down, 0.02f, groundLayer);
				if (hit.collider != null) {
					vel.y += jumpHeight;
				}
			}
			lastPosition = transform.position;
			MoveTick(ref vel, knownPlayerPosition);
		}
		else if (! coneScript.inCone) {
			direction = ! direction;
        }
	}

	private void Spotted(ref Vector2 vel) {
		spotTick = 0;
		state = States.Attacking;
		vel.y += jumpHeight;
		lastPosition = transform.position;
	}
	private bool MoveTick(ref Vector2 vel, Vector2 target) {
		if (target.x > transform.position.x) {
			if (direction) {
				vel.x += acceleration;
			}
			else { // Passed it
				vel.x *= stopMaintainance;
				return true;
			}
		}
		else {
			if (direction) { // Passed it
				vel.x *= stopMaintainance;
				return true;
			}
			else {
				vel.x -= acceleration;
			}
		}
		return false;
	}

    private void FixedUpdate() {
		Vector2 vel = rb.velocity;
		DetectPlayer(ref vel);
		if (state == States.Default) {
			DefaultState(ref vel);
		}
		else if (state == States.Attacking) {
			AttackState(ref vel);
		}

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxSpeed) * Mathf.Sign(vel.x), vel.y);
	}
}
