using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerInLightDetector : MonoBehaviour {
    [HideInInspector] public List<GameObject> inLightAreas;

    private void OnTriggerEnter2D(Collider2D collision) {
        inLightAreas.Add(collision.gameObject);
    }
    private void OnTriggerExit2D(Collider2D collision) {
        inLightAreas.Remove(collision.gameObject);
    }
}
