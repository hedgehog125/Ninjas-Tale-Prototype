using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class mainMenuButtonClick : MonoBehaviour {
    public void Play() {
        SceneManager.LoadScene("Level1");
    }
    public void TestScene1() {
        SceneManager.LoadScene("Testing");
    }
    public void TestScene2() {
        SceneManager.LoadScene("StealthTest");
    }
}
