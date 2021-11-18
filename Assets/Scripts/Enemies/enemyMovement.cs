using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyMovement : MonoBehaviour {
	[SerializeField] private GameObject playerObject;
	[SerializeField] private List<Vector2Int> patrolPath;
	[SerializeField] private int delayTime;

	[Header("Speed")]
	[SerializeField] private float acceleration;
	[SerializeField] private float stopMaintainance;
	[SerializeField] private float maxSpeed;


	private Rigidbody2D rb;
	private SpriteRenderer ren;

	private int currentPoint;
	private bool direction;
	private int delayTick;


	private enum States {
		Default,
		Searching,
		Attacking
    }
	private States state;

    private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		ren = GetComponent<SpriteRenderer>();
    }

    private void Start() {
		state = States.Default;
		if (patrolPath.Count >= 2) {
			currentPoint = 1;
			Vector2 nextPoint = patrolPath[currentPoint] + new Vector2(0.5f, 0.25f);
			direction = nextPoint.x > transform.position.x;
			ren.flipX = ! direction;
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
					ren.flipX = ! direction;
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
        }

		rb.velocity = new Vector2(Mathf.Min(Mathf.Abs(vel.x), maxSpeed) * Mathf.Sign(vel.x), vel.y);
	}
}
