using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spriteVisible : MonoBehaviour {
	[Header("Objects")]
    [SerializeField] private GameObject playerPhysics;

	[Header("Stretching")]
    [SerializeField] private float stretchAmount;
    [SerializeField] private float maxStretch;
    [SerializeField] private float stretchMaintenance;
	[SerializeField] private float stretchAcceleration;
	[SerializeField] private float downwardsBias;

	[Header("Walk Bobbing")]
	[SerializeField] private float walkBobSpeed;
	[SerializeField] private float walkBobAmount;

	[Header("Audio")]
	[SerializeField] private AudioSource walkSound;


	private Rigidbody2D rb;
    private SpriteRenderer ren;
    private playerMovement playerScript;

    private float stretchVel;
	private float currentStretch;

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
		else {
			ren.flipX = ! playerScript.direction; 
		}
		bool direction = ! ren.flipX;

		Vector3 newScale = transform.localScale;
        stretchVel += (playerScript.yAcceleration > 0? playerScript.yAcceleration : (playerScript.yAcceleration * (downwardsBias + 1))) * stretchAmount;
		currentStretch += (stretchVel - currentStretch) * stretchAcceleration;

		newScale.x = Mathf.Max(Mathf.Min(1.0f + currentStretch, 1 + maxStretch), 1 - maxStretch);
        newScale.y = Mathf.Max(Mathf.Min(1.0f - currentStretch, 1 + maxStretch), 1 - maxStretch);
        stretchVel *= stretchMaintenance;

		float bobTickWas = walkBobTick;
		if (playerScript.moveInputNeutralX || (! playerScript.wasOnGround)) {
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
		float offset = Mathf.Sin(walkBobTick) * walkBobAmount;
		if (offset < 0 && Mathf.Sin(bobTickWas) * walkBobAmount > 0) {
			walkSound.Play();
		}
		transform.localPosition = new Vector3(transform.localPosition.x, offset);


		transform.localScale = newScale;
        playerScript.direction = direction;
    }
}
