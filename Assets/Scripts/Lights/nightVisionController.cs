using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class nightVisionController : MonoBehaviour {
    [SerializeField] private bool isSky;


    private Light2D myLight;
    private SpriteRenderer ren;
	private nightVisionGoggles controller;

	private string lightType;
    private bool activeWas;
    private Color darkColor;
    private float darkIntensity;

    private void Awake() {
		controller = GameObject.Find("Player").GetComponent<nightVisionGoggles>();
        if (isSky) {
            ren = GetComponent<SpriteRenderer>();
            darkColor = ren.color;
        }
        else {
            myLight = GetComponent<Light2D>();
            lightType = myLight.lightType.ToString();
            darkColor = myLight.color;
            darkIntensity = myLight.intensity;
        }
    }

    private void FixedUpdate() {
        if (controller.active != activeWas) {
            if (isSky) {
                if (controller.active) {
                    ren.color = controller.lightSkyColor;
                }
                else {
                    ren.color = darkColor;
                }
            }
            else {
                if (controller.active) {
                    myLight.color = controller.lightGlobalColor;
                    myLight.intensity = lightType == "Global"? controller.lightGlobalIntensity : darkIntensity * controller.lightMultiplier;
                }
                else {
                    myLight.color = darkColor;
                    myLight.intensity = darkIntensity;
                }
            }
            activeWas = controller.active;
        }
    }
}
