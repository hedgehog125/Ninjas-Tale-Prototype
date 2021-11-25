using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class deleteOnOtherDelete : MonoBehaviour {
    [SerializeField] private GameObject otherObject;

    private void FixedUpdate() {
        if (otherObject == null) {
            Destroy(gameObject);
        }
    }
}
