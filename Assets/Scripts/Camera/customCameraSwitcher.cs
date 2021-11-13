using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class customCameraSwitcher : MonoBehaviour {
	[SerializeField] private GameObject playerObject;

	private CinemachineVirtualCamera[] cameras;
	private playerMovement playerScript;
	private Rigidbody2D playerRB;


	private void Awake() {
		cameras = new CinemachineVirtualCamera[transform.childCount];
		for (int i = 0; i < transform.childCount; i++) {
			cameras[i] = transform.GetChild(i).gameObject.GetComponent<CinemachineVirtualCamera>();
		}

		playerScript = playerObject.GetComponent<playerMovement>();
		playerRB = playerObject.GetComponent<Rigidbody2D>();
	}

	private void Update() {
		if (playerScript.wasOnGround) {
			SwitchToCamera(0);
		}
		else {
			SwitchToCamera(1);
		}
	}

	private void SwitchToCamera(int id) {
		foreach (CinemachineVirtualCamera cam in cameras) {
			cam.Priority = 0;
		}
		cameras[id].Priority = 1;
	}
}
