using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerDamage : MonoBehaviour {
	[Header("Objects")]
	[SerializeField] private GameObject healthObject;

	[Header("")]
	[SerializeField] private int maxHealth;
	[SerializeField] private Vector2 knockbackAmount;
	[SerializeField] private float deathPlaneY;

	[Header("Times")]
	[SerializeField] private int stunTime;
	[SerializeField] private int invulnerabilityTime;


	private healthBarController healthDisplay;
	private Rigidbody2D rb;

	private int health;
	private int invulnerabilityTick;
	[HideInInspector] public Vector2 respawnLocation;

	[HideInInspector] public int stunTick { get; private set; }


	private void Awake() {
		rb = GetComponent<Rigidbody2D>();
		healthDisplay = healthObject.GetComponent<healthBarController>();

		health = maxHealth;
	}
    private void Start() {
		respawnLocation = transform.position;
	}

    private void FixedUpdate() {
		if (invulnerabilityTick != 0) {
			invulnerabilityTick--;
		}
		if (stunTick != 0) {
			stunTick--;
		}

		if (transform.position.y < deathPlaneY) {
			Die();
        }
	}

	public bool TakeDamage(int amount) {
		return TakeDamage(amount, Vector2.zero, null, true);
	}
	public bool TakeDamage(int amount, bool stun) {
		return TakeDamage(amount, Vector2.zero, null, stun);
	}
	public bool TakeDamage(int amount, Collider2D collider) {
		return TakeDamage(amount, collider.bounds.center, collider, true);
	}
	public bool TakeDamage(int amount, Collider2D collider, bool stun) {
		return TakeDamage(amount, collider.bounds.center, collider, stun);
	}
	public bool TakeDamage(int amount, Vector2 origin, Collider2D collider, bool stun) {
		int healthWas = health;
		if (invulnerabilityTick == 0 || amount < 0) {
			health = Mathf.Clamp(health - amount, 0, maxHealth);
			healthDisplay.health = health;
		}

		if (collider != null && amount > 0) {
			int knockbackX = 0;
			int knockbackY = 1;
			if (origin.x > transform.position.x + (collider.bounds.size.x / 2)) {
				knockbackX = -1;
			}
			else if (origin.x < transform.position.x - (collider.bounds.size.x / 2)) {
				knockbackX = 1;
			}
			if (origin.y > transform.position.y + (collider.bounds.size.y / 2)) {
				knockbackY = -1;
			}

			Vector2 vel = rb.velocity;
			vel.x += knockbackAmount.x * knockbackX;
			vel.y += knockbackAmount.y * knockbackY;
			rb.velocity = vel;
		}

		if (invulnerabilityTick == 0 && amount > 0) {
			if (health == 0) {
				Die();
			}
			else {
				invulnerabilityTick = invulnerabilityTime;
				if (stun) {
					stunTick = stunTime;
				}
			}
		}
		return health != healthWas;
	}

	private void Die() {
		TakeDamage(-(maxHealth - health));
		transform.position = respawnLocation;
	}
}
