using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyMovement : MonoBehaviour {
	[Header("Objects and References")]
	[SerializeField] private GameObject playerObject;
	[SerializeField] private GameObject visionCone;
	[SerializeField] private enemyCollisionCheck largeHitboxScript;
	[SerializeField] private GameObject playerWasObject;
	[SerializeField] private LayerMask groundLayer;

	[Header("")]
	[SerializeField] private List<Vector2Int> patrolPath;

	[Header("Timings")]
	[SerializeField] private int delayTime;
	[SerializeField] private int checkBehindTime;

	[Header("Speed")]
	[SerializeField] private float acceleration;
	[SerializeField] private float stopMaintainance;
	[SerializeField] private float maxSpeed;

	[Header("")]
	[SerializeField] private float jumpPower;

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
	private int stuckTick;

	private bool triedJumpingObstacle;
	private bool aboutToPathfind;
	private int checkBehindTick;
	private bool pathfinding;
	private bool surprisedJumpActive;

	private bool searchDirection;

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
		transform.position += transform.parent.transform.position;
		transform.parent.transform.position = new Vector2();

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
					if (MoveTick(ref vel, nextPoint, false)) { // Passed target
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
	private bool DetectPlayer(ref Vector2 vel) {
		if (playerTouching != 0) {
			Spotted(ref vel);
		}
		else if (
			(coneScript.inCone && alertScript.inLight)
			|| (state != States.Default && coneScript.inLargeCone)
		) {
			Vector2 distance = playerObject.transform.position - transform.position;
			Vector2 castDirection = distance.normalized;

			Vector2 position = visionCone.transform.position;
			RaycastHit2D hit = Physics2D.Raycast(position, castDirection, distance.magnitude + 0.05f, groundLayer);
			if (hit.collider == null) { // There's line of sight
				if (state == States.Default) {
					spotTick++;
					if (spotTick >= spotTime) {
						Spotted(ref vel);
						return true;
					}
				}
				else {
					Spotted(ref vel);
					return true;
                }
			}
			else if (state == States.Default) {
				spotTick -= spotCancelSpeed;
				if (spotTick < 0) spotTick = 0;
			}
		}
		else if (state == States.Default) {
			spotTick--;
			if (spotTick < 0) spotTick = 0;
		}
		return false;
	}

	private void AttackState(ref Vector2 vel, bool lineOfSight) {
		if (surprisedJumpActive) {
			if (isOnGround() && vel.y <= 0) {
				surprisedJumpActive = false;
				lastPosition.x += 2; // Ignore lack of movement on this frame
			}
		}
		else {
			if (checkBehindTick != 0) {
				if (lineOfSight) {
					checkBehindTick = 0;
                }
				else {
					if (checkBehindTick == checkBehindTime) {
						checkBehindTick = 0;
						StartSearching();
					}
					else {
						checkBehindTick++;
                    }
                }
			}
			else if (aboutToPathfind && (! lineOfSight)) {
				pathfinding = true;
				aboutToPathfind = false;
			}

			if (largeHitboxScript.inCollider) { // Near to the known position
				if (checkBehindTick == 0) {
					float difference = knownPlayerPosition.x - transform.position.x;
					float steepness = Mathf.Abs((knownPlayerPosition.y / transform.position.y) / difference);
					bool worthTurning = (difference > col.bounds.size.x) && steepness < 15;

					if ((! coneScript.inLargeCone) && (worthTurning || Vector2.Distance(knownPlayerPosition, playerObject.transform.position) > 0.5f)) {
						direction = ! direction;
						checkBehindTick = 1;
					}
                }
			}
			else {
				if (pathfinding) {
					Debug.Log("A");
				}
				else {
					MoveTick(ref vel, knownPlayerPosition, true);
				}
			}
		}
	}
	private void SearchTick(ref Vector2 vel, bool lineOfSight) {
		if (lineOfSight) {
			state = States.Attacking;
		}
		MoveTick(ref vel, (Vector2)transform.position + new Vector2(searchDirection? 1 : -1, 0), true);
	}

	private void Spotted(ref Vector2 vel) {
		if (state == States.Default) {
			spotTick = 0;
			state = States.Attacking;
			if (isOnGround()) {
				surprisedJumpActive = true;
				vel.y += jumpPower / 1.5f;
			}
		}
		knownPlayerPosition = playerObject.transform.position;
	}
	private void StartSearching() {
		searchDirection = knownPlayerPosition.x > transform.position.x;
		state = States.Searching;
	}

	private bool MoveTick(ref Vector2 vel, Vector2 target, bool autoTurn) {
		if (Mathf.Abs(transform.position.x - lastPosition.x) <= 0.05f) { // Hit an obstacle
			if (stuckTick == 5) {
				if (state != States.Default) {
					if (triedJumpingObstacle && isOnGround() && vel.y <= 0) {
						direction = ! direction;
						aboutToPathfind = true;
					}
				}
				if (isOnGround()) {
					vel.y += jumpPower;
					triedJumpingObstacle = true;
				}
			}
			else {
				stuckTick++;
			}
		}
		else {
			triedJumpingObstacle = false;
			stuckTick = 0;
		}
		lastPosition = transform.position;

		if (target.x > transform.position.x) {
			if (autoTurn) direction = true;
			if (direction) {
				vel.x += acceleration;
			}
			else { // Passed it
				vel.x *= stopMaintainance;
				return true;
			}
		}
		else {
			if (autoTurn) direction = false;
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
	private bool isOnGround() {
		Vector2 center = col.bounds.center;
		Vector2 size = col.bounds.size;
		size.x -= 0.05f;
		size.y = 0.1f;
		center.y += 0.05f - (col.bounds.size.y / 2);

		RaycastHit2D hit = Physics2D.BoxCast(center, size, 0, Vector2.down, 0.02f, groundLayer);
		return hit.collider != null;
	}

    private void FixedUpdate() {
		Vector2 vel = rb.velocity;
		bool lineOfSight = DetectPlayer(ref vel);
		if (state == States.Default) {
			DefaultState(ref vel);
		}
		else if (state == States.Attacking) {
			AttackState(ref vel, lineOfSight);
		}
		else {
			SearchTick(ref vel, lineOfSight);
        }

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxSpeed) * Mathf.Sign(vel.x), vel.y);
	}

    private void LateUpdate() {
		playerWasObject.transform.position = knownPlayerPosition;
    }
}
