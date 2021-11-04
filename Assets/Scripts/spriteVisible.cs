using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spriteVisible : MonoBehaviour
{
    [SerializeField] private GameObject playerPhysics;
    [SerializeField] private float stretchAmount;
    [SerializeField] private float maxStretch;
    [SerializeField] private float stretchMaintenance;

    private Rigidbody2D rb;
    private SpriteRenderer ren;

    private float stretchVel;

    private void Awake() {
        rb = playerPhysics.GetComponent<Rigidbody2D>();
        ren = GetComponent<SpriteRenderer>();
    }
    void FixedUpdate() {
        // Flipping left/right
        if (Mathf.Abs(rb.velocity.x) > 0.1f) {
            ren.flipX = rb.velocity.x < 0;
        }

        Vector3 newScale = transform.localScale;
        stretchVel += rb.velocity.y * stretchAmount;
        newScale.x = Mathf.Max(Mathf.Min(1.0f + stretchVel, 1 + maxStretch), 1 - maxStretch);
        newScale.y = Mathf.Max(Mathf.Min(1.0f - stretchVel, 1 + maxStretch), 1 - maxStretch);
        stretchVel *= stretchMaintenance;

        transform.localScale = newScale;
    }
}
