using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spriteVisible : MonoBehaviour
{
    [SerializeField] private GameObject playerPhysics;
    [SerializeField] private float stretchAmount;
    [SerializeField] private float maxStretch;
    [SerializeField] private float stretchMaintenance;
	[SerializeField] private float walkBobSpeed;
	[SerializeField] private float walkBobAmount;


	private Rigidbody2D rb;
    private SpriteRenderer ren;
    private playerMovement playerScript;

    private float stretchVel;
	private float walkBobTick;
	private bool walkAnimation;
	private bool walkAnimationEnding;
	private float walkVelWas;

    private void Awake() {
        rb = playerPhysics.GetComponent<Rigidbody2D>();
        ren = GetComponent<SpriteRenderer>();
        playerScript = playerPhysics.GetComponent<playerMovement>();
    }
   private void FixedUpdate() {
        // Flipping left/right
        if (Mathf.Abs(rb.velocity.x) > 0.1f) {
            ren.flipX = rb.velocity.x < 0;
        }
		bool direction = ! ren.flipX;

		Vector3 newScale = transform.localScale;
        stretchVel += rb.velocity.y * stretchAmount;
        newScale.x = Mathf.Max(Mathf.Min(1.0f + stretchVel, 1 + maxStretch), 1 - maxStretch);
        newScale.y = Mathf.Max(Mathf.Min(1.0f - stretchVel, 1 + maxStretch), 1 - maxStretch);
        stretchVel *= stretchMaintenance;

		if (playerScript.moveInputNeutralX) {
			if (walkAnimation) {
				if (walkAnimationEnding) {
					walkAnimationEnding = false;
					walkVelWas = rb.velocity.x;
				}
				else {
					bool signWas = Mathf.Sin(walkBobTick) > 0;
					walkBobTick += walkVelWas * walkBobSpeed;

					if (Mathf.Sin(walkBobTick) > 0 != signWas) { // Gone past neutral
						walkBobTick = 0;
						walkAnimation = false;
					}
				}
			}
		}
		else {
			walkBobTick += rb.velocity.x * walkBobSpeed;
			walkAnimationEnding = true;
			walkAnimation = true;
		}
		transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Sin(walkBobTick) * walkBobAmount);


		transform.localScale = newScale;
        playerScript.direction = direction;
    }
}
