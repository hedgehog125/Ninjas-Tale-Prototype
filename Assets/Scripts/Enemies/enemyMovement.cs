using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyMovement : MonoBehaviour {
	[Header("Objects and References")]
	[SerializeField] private GameObject visionCone;
	[SerializeField] private GameObject playerWasObject;
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask raycastLayers;

	[SerializeField] public GameObject katanaPrefab;

	[Header("")]
	[SerializeField] private bool startDirection;
	[SerializeField] private bool isDummy;
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

	[Header("Jumping and falling")]
	[SerializeField] private float jumpPower;
	[SerializeField] private float maxFallDistance;

	[Header("Stealth")]
	[SerializeField] private int spotTime;
	[SerializeField] private float spotCancelSpeed;


	[HideInInspector] public bool direction { get; private set; } // Read by enemy visible script
	private bool leaveDefaultDirection;

	private Rigidbody2D rb;
	private BoxCollider2D col;

	private GameObject playerObject;
	private cameraController cameraScript;
	private musicController musicScript;
	private enemyAlerter alertScript;
	private BoxCollider2D playerCol;
	private Rigidbody2D playerRb;
	private playerConeDetector coneScript;

	private int currentPoint;
	private int playerTouching;
	private bool running;
	private bool isOnGround;
	private bool jumpedSinceGround;

	private Vector2 knownPlayerPosition;
	private Vector2 knownPlayerVel;

	private Vector2 lastPosition;
	private Vector2 returnPoint;
	private float lastMoveTarget;
	private bool pathfindPlayerSide;
	private bool lastMoveSide;

	private int delayTick;
	private float spotTick;
	private int searchTick;

	private bool triedJumpingObstacle;
	private bool aboutToGiveUp;
	private int checkBehindTick;
	[HideInInspector] public bool surprisedJumpActive { get; private set; } // Read by visible

	private bool searchDirection;
	private bool searchTurned;
	private bool waitingForLanding;

	private int inCombatTick;

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


	[HideInInspector]
	public enum States {
		Default,
		Searching,
		Attacking,
		Returning
	}
	[HideInInspector] public States state { get; private set; }

	private void Awake() {
		transform.position += transform.parent.transform.position;
		transform.parent.transform.position = new Vector2();

		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<BoxCollider2D>();

		playerObject = GameObject.Find("Player");
		cameraScript = GameObject.Find("Cameras").GetComponent<cameraController>();
		musicScript = GameObject.Find("Music").GetComponent<musicController>();


		alertScript = playerObject.GetComponent<enemyAlerter>();
		playerCol = playerObject.GetComponent<BoxCollider2D>();
		playerRb = playerObject.GetComponent<Rigidbody2D>();
		coneScript = visionCone.GetComponent<playerConeDetector>();

		direction = startDirection;
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
					if (MoveTick(ref vel, nextPoint, false)[0]) { // Passed target
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
			position.x += direction ? 0.5f : -0.5f;
			RaycastHit2D hit = Physics2D.Raycast(position, castDirection, distance.magnitude - 0.05f, raycastLayers);
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

			//distance = playerWasObject.transform.position - transform.position;
			//castDirection = distance.normalized;

			//position = visionCone.transform.position;
			//hit = Physics2D.Raycast(position, castDirection, distance.magnitude + 0.05f, raycastLayers);
			// TODO: save and use this value. Search when player was is detected but not the player
		}
		else if (state == States.Default) {
			spotTick--;
			if (spotTick < 0) spotTick = 0;
		}
		return false;
	}

	private void AttackState(ref Vector2 vel, bool lineOfSight) {
		if (surprisedJumpActive) {
			if (isOnGround) {
				if (vel.y <= 0) {
					surprisedJumpActive = false;
					lastPosition.x += 2; // Ignore lack of movement on this frame
				}
			}
			else {
				jumpedSinceGround = false;
			}
		}
		else {
			if (waitingForLanding) {
				if (isOnGround) {
					waitingForLanding = false;
					lastPosition.x += 2; // Ignore lack of movement on this frame
				}
			}

			if (checkBehindTick != 0) {
				if (lineOfSight) {
					checkBehindTick = 0;
				}
				else {
					if (isOnGround) {
						if (checkBehindTick == checkBehindTime) {
							checkBehindTick = 0;
							StartSearching();
						}
						else {
							checkBehindTick++;
						}
					}
				}
			}
			else if (aboutToGiveUp && (!lineOfSight)) {
				aboutToGiveUp = false;
				GiveUp();
			}

			if (MoveTick(ref vel, knownPlayerPosition + new Vector2(pathfindPlayerSide ? 1 : -1, 0), waitingForLanding || (!isOnGround))[0] && (!lineOfSight) && isOnGround) {
				StartSearching();
			}
		}
	}
	private void SearchTick(ref Vector2 vel, bool lineOfSight) {
		if (isOnGround) {
			waitingForLanding = false;
		}
		if (lineOfSight) {
			Spotted(ref vel);
		}
		else {
			if (!waitingForLanding) {
				if (searchTick == maxSearchTime) {
					GiveUp();
				}
				else {
					searchTick++;
				}
			}
		}
		if (!waitingForLanding) {
			if (MoveTick(ref vel, (Vector2)transform.position + new Vector2(searchDirection ? 1 : -1, 0), false)[1]) {
				if (!searchTurned) {
					searchDirection = !searchDirection;
					searchTurned = true;
				}
			}
		}
	}

	private void ReturnTick(ref Vector2 vel) {
		if (MoveTick(ref vel, returnPoint, false)[0]) {
			state = States.Default;
			direction = leaveDefaultDirection;
		}
	}

	private void Spotted(ref Vector2 vel) {
		if (state != States.Attacking) {
			spotTick = 0;
			if (isOnGround && (!jumpedSinceGround)) {
				vel.y += jumpPower / 1.5f;
				surprisedJumpActive = true;
				jumpedSinceGround = true;
			}
			else {
				waitingForLanding = true;
			}
			if (state == States.Default) {
				leaveDefaultDirection = direction;
			}
			state = States.Attacking;

			pathfindPlayerSide = true;
			Vector2 center = playerCol.bounds.center;
			Vector2 size = playerCol.bounds.size;
			size.x -= 0.05f;
			size.y -= 0.05f;
			float distanceNeeded = col.bounds.size.x + 0.1f;

			if (Physics2D.BoxCast(center + new Vector2(size.x / 2, 0), size, 0, Vector2.right, distanceNeeded, groundLayer).collider != null) { // Can't go to the right of the player
				if (Physics2D.BoxCast(center - new Vector2(size.x / 2, 0), size, 0, Vector2.left, distanceNeeded, groundLayer).collider != null) { // Or the left
					GiveUp();
				}
				else {
					pathfindPlayerSide = false;
				}
			}
		}
		knownPlayerPosition = playerObject.transform.position;
		if (Mathf.Abs(playerRb.velocity.x) > 1.5f) {
			knownPlayerVel = playerRb.velocity;
		}
	}
	private void StartSearching() {
		state = States.Searching;
		searchTick = 0;
		searchDirection = Mathf.Abs(knownPlayerVel.x) > 0.5f ? knownPlayerVel.x > 0 : knownPlayerPosition.x > transform.position.x;
		searchTurned = false;
		waitingForLanding = true;
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
	}

	private bool[] MoveTick(ref Vector2 vel, Vector2 target, bool preventTurning) {
		bool[] toReturn = new bool[2];

		if (Mathf.Abs(target.x - transform.position.x) < 0.1f) {
			vel.x *= stopMaintainance;
			toReturn[0] = isOnGround;
			return toReturn;
		}
		bool pendingDirection = target.x > transform.position.x;
		bool wallInFront = false;
		if (isOnGround) {
			RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, (Vector2)col.bounds.size - new Vector2(0, 0.05f), 0, pendingDirection ? Vector2.right : Vector2.left, col.bounds.size.x, groundLayer);
			if (hit.collider == null) { // Make sure it's not in a wall
				hit = Physics2D.BoxCast((Vector2)col.bounds.center + new Vector2(pendingDirection ? 1 : -1, 0), new Vector2(0.1f, 0.1f), 0, Vector2.down, maxFallDistance, groundLayer);
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
				Debug.Log(hit.collider.gameObject.name);
			}
		}


		if (wallInFront) { // Hit an obstacle
			if (triedJumpingObstacle) {
				if (state != States.Default) {
					if (isOnGround && vel.y <= 0) {
						pendingDirection = !pendingDirection;
						aboutToGiveUp = true;
					}
				}
			}
			else if (isOnGround && (!jumpedSinceGround)) {
				vel.y += jumpPower;
				triedJumpingObstacle = true;
				jumpedSinceGround = true;
			}
		}
		else {
			triedJumpingObstacle = false;
		}
		lastPosition = transform.position;

		bool canHavePassed = Mathf.Abs(target.x - lastMoveTarget) < 0.01f;
		if (target.x > transform.position.x) {
			if ((lastMoveSide || (!canHavePassed)) && ((!preventTurning) || direction == pendingDirection)) {
				lastMoveSide = true;
				lastMoveTarget = target.x;
				direction = pendingDirection;

				vel.x += running ? runAcceleration : walkAcceleration;
			}
			else { // Passed it
				lastMoveTarget = target.x;

				vel.x *= stopMaintainance;
				toReturn[0] = isOnGround;
				return toReturn;
			}
		}
		else {
			if (lastMoveSide && canHavePassed || (preventTurning && direction != pendingDirection)) { // Passed it
				lastMoveSide = false;
				lastMoveTarget = target.x;

				vel.x *= stopMaintainance;
				toReturn[0] = isOnGround;
				return toReturn;
			}
			else {
				lastMoveTarget = target.x;
				direction = pendingDirection;

				vel.x -= running ? runAcceleration : walkAcceleration;
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

		if (!isDummy) {
			bool lineOfSight = DetectPlayer(ref vel);
			running = state == States.Attacking;
			isOnGround = DetectGround();
			if (!isOnGround) {
				jumpedSinceGround = false;
			}

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
				ReturnTick(ref vel);
			}

			if (state == States.Attacking && (lineOfSight || playerTouching != 0)) {
				inCombatTick = musicScript.spotMusicCooldown;
			}
			else if (inCombatTick > 0) {
				inCombatTick--;
			}

			if (inCombatTick != 0) {
				cameraScript.inCombat = true;
				musicScript.inCombat = true;
			}
			else if (state == States.Searching) {
				cameraScript.enemiesSearching = true;
			}
		}

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), running ? maxRunSpeed : maxWalkSpeed) * Mathf.Sign(vel.x), vel.y);
	}

	private void LateUpdate() {
		knownPlayerPosition += knownPlayerVel * Time.deltaTime;
		playerWasObject.transform.position = knownPlayerPosition;
	}
}
