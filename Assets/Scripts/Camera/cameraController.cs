using UnityEngine;

public class cameraController : MonoBehaviour {
    [SerializeField] private int switchTime;

    [HideInInspector] public States state { get; private set; } = States.Default;
    [HideInInspector] public enum States {
        Default,
        Combat,
        Searching
    }
    [HideInInspector] public bool inCombat;
    [HideInInspector] public bool enemiesSearching;

    private Transform player;

    private Cinemachine.CinemachineVirtualCamera[] cameras;
    private int switchTick;

    private void Awake() {
        player = GameObject.Find("Player").transform;

        int count = transform.childCount - 1;
        cameras = new Cinemachine.CinemachineVirtualCamera[count];
        for (int i = 0; i < count; i++) {
            Cinemachine.CinemachineVirtualCamera cam = transform.GetChild(i).gameObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            cam.Follow = player;
            cameras[i] = cam;
        }
    }

    private void FixedUpdate() {
        States switchState;
        if (inCombat) {
            switchState = States.Combat;
        }
        else if (enemiesSearching) {
            switchState = States.Searching;
        }
        else {
            switchState = States.Default;
        }

        if (state == switchState) {
            switchTick = 0;
        }
        else {
            if (switchTick == switchTime) {
                state = switchState;
            }
            else {
                switchTick++;
            }
        }

        if (state == States.Combat) {
            cameras[0].Priority = 0;
            cameras[1].Priority = 1;
            cameras[2].Priority = 0;
        }
        else if (state == States.Searching) {
            cameras[0].Priority = 0;
            cameras[1].Priority = 0;
            cameras[2].Priority = 1;
        }
        else {
            cameras[0].Priority = 1;
            cameras[1].Priority = 0;
            cameras[2].Priority = 0;
        }

        inCombat = false;
    }
}
