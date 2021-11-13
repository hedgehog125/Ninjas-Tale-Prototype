using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class vcam : MonoBehaviour {
	private CinemachineCameraOffset offset;

	private void Awake() {
		offset = gameObject.GetComponent<CinemachineVirtualCamera>().GetComponent<CinemachineCameraOffset>();
	}

	private void Update() {
		Vector3 newOffset = offset.m_Offset;

		//newOffset.y = ;

		offset.m_Offset = newOffset;
	}
}
