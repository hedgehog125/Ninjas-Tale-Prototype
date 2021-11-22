using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyMovement : MonoBehaviour {
	[Header("Objects and References")]
	[SerializeField] private GameObject playerObject;
	[SerializeField]  private playerConeDetector coneScript;

	[Header("")]
	[SerializeField] private List<Vector2Int> patrolPath;
	[SerializeField] private int delayTime;
	[SerializeField] private int spotTime;

	[Header("Speed")]
	[SerializeField] private float acceleration;
	[SerializeField] private float stopMaintainance;
	[SerializeField] private float maxSpeed;

	[HideInInspector] public bool direction { get; private set; } // Read by enemy visible script

	private Rigidbody2D rb;
	private enemyAlerter alertScript;

	private int currentPoint;
	private int playerTouching;
	private Vector2 knownPlayerPosition;

	private int delayTick;
	private int spotTick;

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
		alertScript = playerObject.GetComponent<enemyAlerter>();
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
			if (delayTick == 0) {
				Vector2 nextPoint = patrolPath[currentPoint] + new Vector2(0.5f, 0.25f);

				if (nextPoint.x > transform.position.x) {
					if (direction) {
						vel.x += acceleration;
					}
					else { // Passed it
						vel.x *= stopMaintainance;
						if (Mathf.Abs(vel.x) < 0.1f) {
							delayTick = 1;
						}
					}
				}
				else {
					if (direction) { // Passed it
						vel.x *= stopMaintainance;
						if (Mathf.Abs(vel.x) < 0.1f) {
							delayTick = 1;
						}
					}
					else {
						vel.x -= acceleration;
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

    private void FixedUpdate() {
		Vector2 vel = rb.velocity;
        if (state == States.Default) {
			DefaultState(ref vel);
			if (playerTouching != 0) {
				spotTick = 0;
				state = States.Attacking;
			}
			else if (coneScript.inCone && alertScript.inLight) {
				knownPlayerPosition = playerObject.transform.position;
				spotTick++;
				if (spotTick == spotTime) {
					spotTick = 0;
					state = States.Attacking;
				}
			}
			else {
				spotTick = 0;
			}
		}

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxSpeed) * Mathf.Sign(vel.x), vel.y);
	}
}
