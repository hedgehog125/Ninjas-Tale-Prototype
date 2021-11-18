using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAlerter : MonoBehaviour {
    [Header("Objects and Layers")]
    [SerializeField] private GameObject inLightTester;
    [SerializeField] private playerInLightDetector inLightScript;

    [SerializeField] private LayerMask lightLayer;
    [SerializeField] private LayerMask lightSourceLayer;

	private BoxCollider2D inLightCol;
    private playerMovement moveScript;
    private LayerMask groundLayer;

    private bool inLight;
    private float inLightOffset;

    // TODO: use physics layer masks and ontrigger enter

    private void Awake() {
		inLightCol = inLightScript.GetComponent<BoxCollider2D>();
		moveScript = GetComponent<playerMovement>();
        groundLayer = moveScript.groundLayer;


        inLightOffset = Mathf.Abs(inLightCol.offset.x);
    }

    private void FixedUpdate() {
        bool isInLight = false;
        foreach (GameObject currentLight in inLightScript.inLightAreas) {
            Vector2 distance = currentLight.transform.position - transform.position;
            Vector2 direction = distance.normalized;

            RaycastHit2D hit = Physics2D.Raycast(inLightTester.transform.position, direction, distance.magnitude + 0.05f, moveScript.groundLayer);
			if (hit.collider == null) {
				isInLight = true;
				break;
			}
        }
        if (isInLight) {
            Debug.Log("A");
        }
    }
    private void LateUpdate() {
        Vector2 offset = inLightCol.offset;
        if (moveScript.direction) {
            offset.x = inLightOffset;
        }
        else {
            offset.x = -inLightOffset;
        }
		inLightCol.offset = offset;
    }
}
