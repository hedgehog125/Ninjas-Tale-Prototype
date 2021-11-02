using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthBarController : MonoBehaviour
{
	private int _health;
	private Image[] hearts;

	public int maxHealth;
	public Sprite[] heartImages;
	public int health {
		set {
			int healthLeft = value;
			foreach (Image heart in hearts) {
				if (healthLeft >= 2) {
					heart.sprite = heartImages[2];
					healthLeft -= 2;
				}
				else if (healthLeft == 1) {
					heart.sprite = heartImages[1];
					healthLeft--;
				}
				else {
					heart.sprite = heartImages[0];
				}
			}
			_health = value;
		}
		get {
			return _health;
		}
	}
    void Awake()
    {
		int count = transform.childCount;
		hearts = new Image[count];
		for (int i = 0; i < count; i++) {
			hearts[i] = transform.GetChild(i).gameObject.GetComponent<Image>();
		}

		health = maxHealth; // Trigger update
	}

	// Update is called once per frame
	void Update()
    {
        
    }
}
