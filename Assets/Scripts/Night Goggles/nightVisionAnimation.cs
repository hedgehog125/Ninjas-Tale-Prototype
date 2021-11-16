using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nightVisionAnimation : MonoBehaviour {
    [Header("GameObjects")]
    [SerializeField] private GameObject playerVisible;

    [Header("Movement")]
    [SerializeField] private float startOffset;
    [SerializeField] private float endOffset;
    [SerializeField] private float moveSpeed;

    [HideInInspector] public bool animating { get; private set; }
    
    private SpriteRenderer ren;
    private SpriteRenderer playerRenderer;

    private bool equipping;


    private void Awake() {
        ren = GetComponent<SpriteRenderer>();
        playerRenderer = playerVisible.GetComponent<SpriteRenderer>();
    }

    public void Toggle() {
        equipping = ! gameObject.activeSelf;
        if (equipping) {
            gameObject.SetActive(true);
            transform.localPosition = new Vector2(0, startOffset);
        }
        animating = true;
    }

    private void FixedUpdate() {
        ren.flipX = playerRenderer.flipX;
    }

    private void Update() {
        if (animating) {
            Vector2 pos = transform.localPosition;
            float target = equipping? endOffset : startOffset;
            float speed = target > pos.y? moveSpeed : -moveSpeed;

            pos.y += speed * Time.deltaTime;
            if (speed > 0) {
                if (pos.y > target) {
                    animating = false;
                }
            }
            else {
                if (pos.y < target) {
                    animating = false;
                }
            }
            if (! animating) { // Just finished
                pos.y = target;
            }
            transform.localPosition = pos;
        }
        else if (! equipping) {
            gameObject.SetActive(false);
        }
    }
}
