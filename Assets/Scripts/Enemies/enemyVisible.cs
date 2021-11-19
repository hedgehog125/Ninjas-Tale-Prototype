using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyVisible : MonoBehaviour {
	[Header("Objects and References")]
	[SerializeField] private GameObject mainObject;
	[SerializeField] private GameObject torchLightPrefab;
	[SerializeField] private Sprite[] images;


	private enemyMovement moveScript;
	private enemyType.Types type;
	private SpriteRenderer ren;

	private float baseScale;

	private void Awake() {
		moveScript = mainObject.GetComponent<enemyMovement>();
		type = mainObject.GetComponent<enemyType>().type;
		ren = GetComponent<SpriteRenderer>();

		// I refuse to use a switch statement because they are ugly, change my mind
		int image = -1;
		if (type == enemyType.Types.Normal) {
			image = 0;
		}
		else if (type == enemyType.Types.Torch) {
			image = 1;
			Instantiate(torchLightPrefab, transform);
		}

		if (image != -1) {
			ren.sprite = images[image];
		}

		baseScale = Mathf.Abs(transform.localScale.x);
	}

	private void LateUpdate() {
		Vector2 scale = transform.localScale;
		scale.x = moveScript.direction? baseScale : -baseScale;
		transform.localScale = scale;
	}
}
