using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAlerter : MonoBehaviour {
    [Header("Objects and Layers")]
    [SerializeField] private GameObject inLightTester;
    [SerializeField] private playerInLightDetector inLightScript;

	[Header("Raycasting")]
	[SerializeField] private float topCast;
	[SerializeField] private float bottomCast;


	[HideInInspector] public bool inLight { get; private set; }

	private BoxCollider2D inLightCol;
    private playerMovement moveScript;

    private float inLightOffset;

    private void Awake() {
		inLightCol = inLightScript.GetComponent<BoxCollider2D>();
		moveScript = GetComponent<playerMovement>();


        inLightOffset = Mathf.Abs(inLightCol.offset.x);
    }

    private void FixedUpdate() {
		inLight = false;
        for (int i = 0; i < inLightScript.inLightAreas.Count; i++) {
			GameObject currentLight = inLightScript.inLightAreas[i];
			if (currentLight == null) { // Might have been destroyed
				inLightScript.inLightAreas.Remove(currentLight);
				continue;
            }

            Vector2 distance = currentLight.transform.position - transform.position;
            Vector2 direction = distance.normalized;

			Vector2 center = inLightTester.transform.position;
			Vector2 position = center;
			position.y += topCast;
			RaycastHit2D hit = Physics2D.Raycast(position, direction, distance.magnitude + 0.05f, moveScript.groundLayer);
			if (hit.collider == null) {
				inLight = true;
				break;
			}

			position.y = center.y + bottomCast;
			hit = Physics2D.Raycast(position, direction, distance.magnitude + 0.05f, moveScript.groundLayer);
			if (hit.collider == null) {
				inLight = true;
				break;
			}
		}
    }
    private void LateUpdate() {
        Vector2 offset = inLightCol.offset;
        if (moveScript.direction) {
            offset.x = -inLightOffset;
        }
        else {
            offset.x = inLightOffset;
        }
		inLightCol.offset = offset;
    }
}
