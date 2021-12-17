using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyVisible : MonoBehaviour {
	[Header("Objects and References")]
	[SerializeField] private GameObject mainObject;
	[SerializeField] private Sprite[] images;
	[SerializeField] private Sprite dummyImage;
	[SerializeField] private GameObject torchLightPrefab;


	private enemyMovement moveScript;
	private enemyType.Types type;
	private SpriteRenderer ren;
	private GameObject item;

	private float baseScale;
	private bool attackState;

	private void Awake() {
		moveScript = mainObject.GetComponent<enemyMovement>();
		type = mainObject.GetComponent<enemyType>().type;
		ren = GetComponent<SpriteRenderer>();

		// I refuse to use a switch statement because they are ugly, change my mind
		attackState = true; // So it initialises
		InitType();
		baseScale = Mathf.Abs(transform.localScale.x);
	}

	private void LateUpdate() {
		if (moveScript.state == enemyMovement.States.Attacking && (! moveScript.surprisedJumpActive)) {
			InitAttack();
		}
		else {
			InitType();
		}

		Vector2 scale = transform.localScale;
		scale.x = moveScript.direction? baseScale : -baseScale;
		transform.localScale = scale;
	}

	public void InitType() {
		if (! attackState) return;
		if (item) {
			Destroy(item);
		}

		int image = -1;
		if (moveScript.isDummy) {
			ren.sprite = dummyImage;
		}
		else {
			if (type == enemyType.Types.Normal) {
				image = 0;
			}
			else if (type == enemyType.Types.Torch) {
				image = 1;
				item = Instantiate(torchLightPrefab, transform);
			}

			if (image != -1) {
				ren.sprite = images[image];
			}
		}
		attackState = false;
	}
	public void InitAttack() {
		if (attackState) return;
		if (item) {
			Destroy(item);
		}

		/*
		if (playerScript.direction) {
			transform.Rotate(new Vector3(0, 0, -135));
			ren.flipX = true;
		}
		else {
			transform.Rotate(new Vector3(0, 0, 135));
			ren.flipX = false;
		}
		*/

		item = Instantiate(moveScript.katanaPrefab, transform);
		ren.sprite = images[0];
		attackState = true;
	}
}