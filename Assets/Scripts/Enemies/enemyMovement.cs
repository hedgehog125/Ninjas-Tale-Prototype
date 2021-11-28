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
	[SerializeField] private int maxSearchTime;

	[Header("Speed")]
	[SerializeField] private float walkAcceleration;
	[SerializeField] private float runAcceleration;
	[SerializeField] private float stopMaintainance;
	[SerializeField] private float maxWalkSpeed;
	[SerializeField] private float maxRunSpeed;

	[Header("")]
	[SerializeField] private float jumpPower;
	[SerializeField] private float maxFallDistance;

	[Header("Stealth")]
	[SerializeField] private int spotTime;
	[SerializeField] private float spotCancelSpeed;


	[HideInInspector] public bool direction { get; private set; } // Read by enemy visible script
	private bool leaveDefaultDirection;

	private Rigidbody2D rb;
	private BoxCollider2D col;

	private enemyAlerter alertScript;
	private Rigidbody2D playerRb;
	private playerConeDetector coneScript;

	private int currentPoint;
	private int playerTouching;
	private bool running;
	private bool isOnGround;

	private Vector2 knownPlayerPosition;
	private Vector2 knownPlayerVel;

	private Vector2 lastPosition;
	private Vector2 returnPoint;
	private float lastMoveTarget;
	private bool lastMoveSide;

	private int delayTick;
	private float spotTick;
	private int searchTick;

	private bool triedJumpingObstacle;
	private bool aboutToPathfind;
	private int checkBehindTick;
	private bool pathfinding;
	private bool surprisedJumpActive;

	private bool searchDirection;
	private bool searchTurned;

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
		Attacking,
		Returning
    }
	private States state;

    private void Awake() {
		transform.position += transform.parent.transform.position;
		transform.parent.transform.position = new Vector2();

		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<BoxCollider2D>();

		alertScript = playerObject.GetComponent<enemyAlerter>();
		playerRb = playerObject.GetComponent<Rigidbody2D>();
		coneScript = visionCone.GetComponent<playerConeDetector>();
    }

    private void Start() {
		state = States.Default;
		if (patrolPath.Count >= 2) {
			currentPoint = 1;
			Vector2 nextPoint = patrolPath[currentPoint] + new Vector2(0.5f, 0.25f);
			direction = nextPoint.x > transform.position.x;
		}
		else if (patrolPath.Count == 0) {
			patrolPath.Add(new Vector2Int((int)(transform.position.x - 0.5f), (int)(transform.position.y - 0.25f))); // So the enemy knows where to go back to after giving up searching
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
					if (MoveTick(ref vel, nextPoint)[0]) { // Passed target
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
			|| ((state == States.Attacking || state == States.Searching) && coneScript.inLargeCone)
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
			if (isOnGround && vel.y <= 0) {
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

			if (largeHitboxScript.inCollider && lineOfSight) { // Near to the known position
				if (checkBehindTick == 0) {
					float difference = knownPlayerPosition.x - transform.position.x;
					float steepness = Mathf.Abs((knownPlayerPosition.y / transform.position.y) / difference);
					bool worthTurning = (difference > col.bounds.size.x) && steepness < 15;

					if (! coneScript.inLargeCone) {
						if (worthTurning || Vector2.Distance(knownPlayerPosition, playerObject.transform.position) > 0.5f) {
							direction = ! direction;
							checkBehindTick = 1;
						}
						else {
							StartSearching();
                        }
					}
                }
			}
			else {
				if (pathfinding) {
					GiveUp();
				}
				else {
					if (MoveTick(ref vel, knownPlayerPosition)[0] && (! lineOfSight)) {
						StartSearching();
					}
				}
			}
		}
	}
	private void SearchTick(ref Vector2 vel, bool lineOfSight) {
		if (lineOfSight) {
			Spotted(ref vel);
		}
		else {
			if (searchTick == maxSearchTime) {
				GiveUp();
            }
			else {
				searchTick++;
            }
        }
		if (MoveTick(ref vel, (Vector2)transform.position + new Vector2(searchDirection? 1 : -1, 0))[1]) {
			if (! searchTurned) {
				searchDirection = ! searchDirection;
				searchTurned = true;
			}
		}
	}

	private void ReturnTick(ref Vector2 vel, bool lineOfSight) {
		if (MoveTick(ref vel, returnPoint)[0]) {
			state = States.Default;
			direction = leaveDefaultDirection;
        }
    }

	private void Spotted(ref Vector2 vel) {
		if (state != States.Attacking) {
			spotTick = 0;
			if (isOnGround) {
				surprisedJumpActive = true;
				vel.y += jumpPower / 1.5f;
			}
			if (state == States.Default) {
				leaveDefaultDirection = direction;
			}
			state = States.Attacking;
		}
		knownPlayerPosition = playerObject.transform.position;
		knownPlayerVel = playerRb.velocity;
	}
	private void StartSearching() {
		state = States.Searching;
		searchTick = 0;
		searchDirection = Mathf.Abs(knownPlayerVel.x) > 0.5f? knownPlayerVel.x > 0 : knownPlayerPosition.x > transform.position.x;
		searchTurned = false;
	}
	private void GiveUp() {
		state = States.Returning;

		float closest = Mathf.Infinity;
		int closestID = 0;
		for (int i = 0; i < patrolPath.Count; i++) {
			Vector2 point = patrolPath[i];
			float distance = Mathf.Abs((point.x + 0.5f) - transform.position.x);
			if (distance < closest) {
				closest = distance;
				closestID = i;
            }
        }
		returnPoint = patrolPath[closestID] + new Vector2(0.5f, 0.25f);
		currentPoint = closestID;

		direction = returnPoint.x > transform.position.x;
    }

	private bool[] MoveTick(ref Vector2 vel, Vector2 target) {
		bool[] toReturn = new bool[2];

		if (Mathf.Abs(target.x - transform.position.x) < 0.1f) {
			vel.x *= stopMaintainance;
			toReturn[0] = true;
			return toReturn;
		}
		direction = target.x > transform.position.x;
		bool wallInFront = false;
		if (isOnGround) {
			RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, (Vector2)col.bounds.size - new Vector2(0, 0.05f), 0, direction? Vector2.right : Vector2.left, col.bounds.size.x, groundLayer);
			if (hit.collider == null) { // Make sure it's not in a wall
				hit = Physics2D.BoxCast((Vector2)col.bounds.center + new Vector2(direction? 1 : -1, 0), new Vector2(0.1f, 0.1f), 0, Vector2.down, maxFallDistance, groundLayer);
				if (hit.collider == null) {
					lastPosition = transform.position;

					toReturn[0] = true;
					toReturn[1] = true;
					vel.x *= stopMaintainance;
					return toReturn;
				}
            }
			else {
				wallInFront = true;
			}
		}

		if (wallInFront && Mathf.Abs(target.x - transform.position.x) > 0.8f) { // Hit an obstacle
			if (state != States.Default) {
				if (triedJumpingObstacle && isOnGround && vel.y <= 0) {
					direction = ! direction;
					aboutToPathfind = true;
				}
			}
			if (isOnGround) {
				vel.y += jumpPower;
				triedJumpingObstacle = true;
			}
		}
		else {
			triedJumpingObstacle = false;
		}
		lastPosition = transform.position;

		bool canHavePassed = Mathf.Abs(target.x - lastMoveTarget) < 0.01f;
		if (target.x > transform.position.x) {
			if (lastMoveSide || (! canHavePassed)) {
				lastMoveSide = true;
				lastMoveTarget = target.x;

				vel.x += running? runAcceleration : walkAcceleration;
			}
			else { // Passed it
				lastMoveSide = true;
				lastMoveTarget = target.x;

				vel.x *= stopMaintainance;
				toReturn[0] = true;
				return toReturn;
			}
		}
		else {
            if (lastMoveSide && canHavePassed) { // Passed it
                lastMoveSide = false;
                lastMoveTarget = target.x;

                vel.x *= stopMaintainance;
				toReturn[0] = true;
				return toReturn;
			}
            else {
                lastMoveSide = false;
                lastMoveTarget = target.x;

                vel.x -= running? runAcceleration : walkAcceleration;
            }
        }
		return toReturn;
	}
	private bool DetectGround() {
		Vector2 center = col.bounds.center;
		Vector2 size = col.bounds.size;
		size.y = 0.1f;
		center.y += 0.1f - (col.bounds.size.y / 2);

		RaycastHit2D hit = Physics2D.BoxCast(center, size, 0, Vector2.down, 0.07f, groundLayer);
		return hit.collider != null;
	}

    private void FixedUpdate() {
		Vector2 vel = rb.velocity;
		bool lineOfSight = DetectPlayer(ref vel);
		running = state == States.Attacking;
		isOnGround = DetectGround();

		if (state == States.Default) {
			DefaultState(ref vel);
		}
		else if (state == States.Attacking) {
			AttackState(ref vel, lineOfSight);
		}
		else if (state == States.Searching) {
			SearchTick(ref vel, lineOfSight);
        }
		else if (state == States.Returning) {
			ReturnTick(ref vel, lineOfSight);
		}

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), running? maxRunSpeed : maxWalkSpeed) * Mathf.Sign(vel.x), vel.y);
	}

    private void LateUpdate() {
		playerWasObject.transform.position = knownPlayerPosition;
    }
}
