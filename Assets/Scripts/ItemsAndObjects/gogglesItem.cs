using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gogglesItem : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        collision.gameObject.GetComponent<nightVisionGoggles>().unlocked = true;
        Destroy(gameObject);
    }
}
