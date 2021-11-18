using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAlerter : MonoBehaviour {
    [Header("Objects and Layers")]
    [SerializeField] private BoxCollider2D inLightTester;
    [SerializeField] private playerInLightDetector inLightScript;

    [SerializeField] private LayerMask lightLayer;
    [SerializeField] private LayerMask lightSourceLayer;

    private playerMovement moveScript;
    private LayerMask groundLayer;

    private bool inLight;
    private float inLightOffset;

    // TODO: use physics layer masks and ontrigger enter

    private void Awake() {
        moveScript = GetComponent<playerMovement>();
        groundLayer = moveScript.groundLayer;
        inLightOffset = Mathf.Abs(inLightTester.offset.x);
    }

    private void FixedUpdate() {
        bool inLight = false;
        foreach (GameObject light in inLightScript.inLightAreas) {
            Vector2 distance = light.transform.position - transform.position;
            Vector2 direction = distance.normalized;

            RaycastHit2D hit = Physics2D.Raycast(inLightTester.transform.position, direction, distance.magnitude + 0.05f, moveScript.groundLayer);
        }
        if (inLight) {
            Debug.Log("A");
        }
    }
    private void LateUpdate() {
        Vector2 offset = inLightTester.offset;
        if (moveScript.direction) {
            offset.x = inLightOffset;
        }
        else {
            offset.x = -inLightOffset;
        }
        inLightTester.offset = offset;
    }
}
