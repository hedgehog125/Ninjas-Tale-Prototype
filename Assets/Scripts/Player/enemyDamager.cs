using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyDamager : MonoBehaviour {
    [SerializeField] public int amount;
    [SerializeField] public Sounds sound;
    [HideInInspector] public enum Sounds {
        None,
        Katana
    };

    private void Awake() {
        if (amount <= 0) {
            Debug.LogWarning("Enemy damager amount is less than or equal to 0.");
        }
    }
}
