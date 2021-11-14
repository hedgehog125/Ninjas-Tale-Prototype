using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class damagable : MonoBehaviour
{
	[SerializeField] private int amount;
	[SerializeField] private GameObject playerObject;

	private playerDamage damageScript;
	private Collider2D col;


	private void Awake() {
		damageScript = playerObject.GetComponent<playerDamage>();
		col = GetComponent<Collider2D>();
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			damageScript.TakeDamage(amount, col);
		}
	}
}
