using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class katanaMovement : MonoBehaviour
{
	[SerializeField] private GameObject player;

	[SerializeField] private float maxSpeed;
	[SerializeField] private Vector2 playerOffset;
	[SerializeField] private float passedTargetMaintainance;
	[SerializeField] private int maxAge;



	// Set by the player attack script
	public Vector2 target;

	private Rigidbody2D rb;
	private playerMovement playerScript;

	private Vector2 speed;
	private bool hasPassedTarget;
	private int age;


	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		playerScript = player.GetComponent<playerMovement>();
	}

	private void Start() { // When thrown
		Vector2 position = new Vector2(player.transform.position.x, player.transform.position.y) + (playerOffset * new Vector2Int(playerScript.direction? 1 : -1, 0));
		transform.position = position;

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
	}

	private void FixedUpdate() {
		if (hasPassedTarget) {
			gameObject.SetActive(false);
			return;
		}
		else {
			bool slowDown = false;
			if (true || Mathf.Abs(speed.x) > Mathf.Abs(speed.y)) {
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
			}
			else {
				if (speed.y > 0) {

				}
				else {

				}
			}

			if (slowDown) {
				speed *= passedTargetMaintainance;
				if (Mathf.Abs(speed.magnitude) < 0.2f) {
					speed *= 0;
					hasPassedTarget = true;
                }
			}
		}
		rb.velocity = speed;

		age++;
		if (age > maxAge) {
			gameObject.SetActive(false);
        }
	}
}
