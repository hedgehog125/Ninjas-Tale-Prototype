using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyType : MonoBehaviour {
	[SerializeField] public Types type;
	[HideInInspector] public enum Types {
		Normal,
		Torch
	}
}
