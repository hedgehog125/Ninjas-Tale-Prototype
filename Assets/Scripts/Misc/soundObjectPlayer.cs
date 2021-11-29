using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundObjectPlayer : MonoBehaviour {
    [SerializeField] private AudioSource katanaSound;

    private bool started;
    public void Play(enemyDamager.Sounds soundName) {
        if (soundName == enemyDamager.Sounds.Katana) {
            katanaSound.Play();
            started = true;
        }
    }

    private void FixedUpdate() {
        if (started && (! katanaSound.isPlaying)) {
            Destroy(gameObject);
        }
    }
}
