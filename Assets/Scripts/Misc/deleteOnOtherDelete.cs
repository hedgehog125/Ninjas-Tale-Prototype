using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class deleteOnOtherDelete : MonoBehaviour {
    [SerializeField] private GameObject otherObject;
	[SerializeField] private bool waitAFrame;

	private bool frameWaitTick;

    private void FixedUpdate() {
        if (otherObject == null) {
			if (frameWaitTick || (! waitAFrame)) {
				Destroy(gameObject);
			}
			else {
				frameWaitTick = true;
			}
        }
    }
}
