using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Experimental.Rendering.Universal;

public class nightVisionGoggles : MonoBehaviour {
    [Header("Game Objects and References")]
    [SerializeField] private GameObject goggles;
	[SerializeField] private GameObject minLights;
	[SerializeField] private Sprite lightImage;
	[SerializeField] private SpriteRenderer ren;

	[Header("")]
	[SerializeField] private bool unlocked;

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
	private Sprite darkImage;

	[HideInInspector] public bool active; // Set by animation script

	[HideInInspector] public float physicalLightAdd { get; private set; }

	private float originalMinLightObjects;
	private float originalMinLightSky;

	private void Awake() {
        gogglesScript = goggles.GetComponent<nightVisionAnimation>();
		alertScript = GetComponent<enemyAlerter>();

		minLightObjects = minLights.GetComponent<Light2D>();
		minLightSky = minLights.transform.GetChild(1).GetComponent<Light2D>();
		originalMinLightObjects = minLightObjects.intensity;
		originalMinLightSky = minLightSky.intensity;
		darkImage = ren.sprite;


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
		if (unlocked) {
			if (! gogglesScript.animating) {
				if (gogglesInput) {
					active = ! active;
					gogglesScript.Toggle();

					gogglesInput = false;
				}
			}
		}
		else {
			active = false;
		}

		if (active) {
			minLightSky.intensity = originalMinLightSky;
			physicalLightAdd = 0;
		}
		else {
			if (alertScript.inLight) {
				ren.sprite = lightImage;

				minLightObjects.intensity -= lightAreaChangeSpeed * Time.deltaTime;
				physicalLightAdd += lightAreaChangeSpeed * Time.deltaTime;
				if (originalMinLightObjects - minLightObjects.intensity > lightAreaObjectReduce) {
					minLightObjects.intensity = originalMinLightObjects - lightAreaObjectReduce;
					physicalLightAdd = lightAreaObjectReduce;
				}
				minLightSky.intensity -= lightAreaChangeSpeed * Time.deltaTime;
				if (originalMinLightSky - minLightSky.intensity > lightAreaSkyReduce) {
					minLightSky.intensity = originalMinLightSky - lightAreaSkyReduce;
				}
			}
			else {
				ren.sprite = darkImage;

				minLightObjects.intensity += lightAreaChangeSpeed * Time.deltaTime;
				physicalLightAdd -= lightAreaChangeSpeed * Time.deltaTime;
				if (minLightObjects.intensity > originalMinLightObjects) {
					minLightObjects.intensity = originalMinLightObjects;
					physicalLightAdd = 0;
				}
				minLightSky.intensity += lightAreaChangeSpeed * Time.deltaTime;
				if (minLightSky.intensity > originalMinLightSky) {
					minLightSky.intensity = originalMinLightSky;
				}
			}
		}
	}
}
