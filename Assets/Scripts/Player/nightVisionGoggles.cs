using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Experimental.Rendering.Universal;

public class nightVisionGoggles : MonoBehaviour {
    [Header("Game Objects")]
    [SerializeField] private GameObject goggles;
	[SerializeField] private GameObject minLights;

	// Also read by the controller script
	[Header("When Active")]
    [SerializeField] public float lightGlobalIntensity;
    [SerializeField] public float lightMultiplier;

	[Header("When In Light Area")]
	[SerializeField] public float lightAreaSkyReduce;
	[SerializeField] public float lightAreaObjectReduce;
	[SerializeField] public float lightAreaChangeSpeed;


	[Header("Colours When Active")]
    [SerializeField] public Color lightGlobalColor;
    [SerializeField] public Color lightSkyColor;

    private bool gogglesInput;

    private nightVisionAnimation gogglesScript;
	private enemyAlerter alertScript;
	private Light2D minLightObjects;
	private Light2D minLightSky;

	[HideInInspector] public bool active; // Set by animation script

	private float originalMinLightObjects;
	private float originalMinLightSky;

	private void Awake() {
        gogglesScript = goggles.GetComponent<nightVisionAnimation>();
		alertScript = GetComponent<enemyAlerter>();

		minLightObjects = minLights.GetComponent<Light2D>();
		minLightSky = minLights.transform.GetChild(0).GetComponent<Light2D>();
		originalMinLightObjects = minLightObjects.intensity;
		originalMinLightSky = minLightSky.intensity;


		if (lightGlobalColor.a < 0.99f) {
            Debug.LogWarning("Global light colour is at least partly transparent.");
        }
        if (lightSkyColor.a < 0.99f) {
            Debug.LogWarning("Sky light colour is at least partly transparent.");
        }
    }

    private void OnNightVision(InputValue input) {
        gogglesInput = input.isPressed;
    }

    private void Update() {
        if (! gogglesScript.animating) {
            if (gogglesInput) {
                active = ! active;
                gogglesScript.Toggle();

                gogglesInput = false;
            }
        }

		if (active) {
			minLightObjects.intensity = originalMinLightObjects;
			minLightSky.intensity = originalMinLightSky;
		}
		else {
			if (alertScript.inLight) {
				if ()
				minLightObjects.intensity -= lightAreaChangeSpeed * Time.deltaTime;
				minLightSky.intensity += ;
			}
		}
    }
}
