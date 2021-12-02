using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class katanaItem : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("PlayerMain")) {
            collision.gameObject.GetComponent<playerAttack>().enableKatana = true;
            Destroy(gameObject);
        }
    }
}
