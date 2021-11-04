using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class tilemapShadow : MonoBehaviour
{
    void Awake() {
        ShadowCaster2D caster = GetComponent<ShadowCaster2D>();
        Debug.Log("A");
    }
}
