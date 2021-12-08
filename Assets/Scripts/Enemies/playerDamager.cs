using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerDamager : MonoBehaviour
{
	[SerializeField] private int amount;
	
	private playerDamage damageScript;
	private Collider2D col;


	private void Awake() {
		damageScript = GameObject.Find("Player").GetComponent<playerDamage>();
		col = GetComponent<Collider2D>();

		if (amount <= 0) {
			Debug.LogWarning("Enemy damager amount is less than or equal to 0.");
		}
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			damageScript.TakeDamage(amount, col);
		}
	}
}
