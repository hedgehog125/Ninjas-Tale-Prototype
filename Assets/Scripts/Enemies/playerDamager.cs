using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerDamager : MonoBehaviour
{
	[SerializeField] private int amount = 1;
	[SerializeField] private float playerThorns;
	
	private playerDamage damageScript;
	private enemyDamage myDamageScript;
	private Collider2D col;


	private void Awake() {
		col = GetComponent<Collider2D>();
		myDamageScript = GetComponent<enemyDamage>();
		damageScript = GameObject.Find("Player").GetComponent<playerDamage>();

		if (amount == 0) {
			Debug.LogWarning("Enemy damager amount is 0.");
		}
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			if (damageScript.TakeDamage(amount, col) && myDamageScript != null) {
				myDamageScript.TakeDamage(Mathf.CeilToInt(amount * playerThorns));
			}
		}
	}
	private void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("PlayerMain")) {
			
			if (damageScript.TakeDamage(amount, col)  && myDamageScript != null) {
				myDamageScript.TakeDamage(Mathf.CeilToInt(amount * playerThorns));
			}
		}
	}
}
