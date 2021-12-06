using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class GridShadowCastersGenerator : MonoBehaviour {

    [SerializeField] private Tilemap tilemap; 
    [SerializeField] private GameObject shadowCasterPrefab;
    [SerializeField] private Transform shadowCastersContainer;
    [SerializeField] private float litRadius;
    [SerializeField] private bool removePreviouslyGenerated = true;

    bool[,] hits;
    GameObject[,] instances;

    public GameObject[] Generate() {
        Debug.Log("### Generating ShadowCasters ###");

        /* get the bounds of the area to check */

        // get outer-most bound vertices, defining the area to check

        Vector2Int bottomLeft = (Vector2Int)tilemap.cellBounds.min;
        Vector2Int topRight = (Vector2Int)tilemap.cellBounds.max;

        Debug.Log("Bounds: downLeft = (" + bottomLeft.x + ", " + bottomLeft.y + ")");
        Debug.Log("Bounds: topRight = (" + topRight.x + ", " + topRight.y + ")");

        /* check the area for collisions */

        int countX = Mathf.RoundToInt(topRight.x - bottomLeft.x);
        int countY = Mathf.RoundToInt(topRight.y - bottomLeft.y);

        hits = new bool[countX, countY];
        instances = new GameObject[countX, countY];

        for (int y = 0; y < countY; y++) {
            for (int x = 0; x < countX; x++) {
                hits[x, y] = IsHit((Vector3Int)(bottomLeft + new Vector2Int(x, y)));
            }
        }

        /* instantiate shadow casters, merging single tiles horizontaly */

        // removing old shadow casters! careful!

        if (removePreviouslyGenerated) {
            for (int i = shadowCastersContainer.childCount - 1; i >= 0; i--) {
                DestroyImmediate(shadowCastersContainer.GetChild(i).gameObject);
            }
        }

        // create new ones

        for (int y = 0; y < countY; y++) {
            bool previousWasHit = false;
            GameObject currentInstance = null;

            for (int x = 0; x < countX; x++) {
                if (hits[x, y]) {
                    if (!previousWasHit) {

                        // create new shadowCasterPrefab instance

                        currentInstance = (GameObject)PrefabUtility.InstantiatePrefab(shadowCasterPrefab, shadowCastersContainer);
                        currentInstance.transform.position = new Vector3(bottomLeft.x + x + 0.5f, bottomLeft.y + y + 0.5f, 0.0f);
                    } else {

                        // stretch prevois shadowCasterPrefab instance

                        currentInstance.transform.localScale = new Vector3(currentInstance.transform.localScale.x + 1.0f, 1.0f, 0.0f);
                        currentInstance.transform.Translate(new Vector3(0.5f, 0.0f, 0.0f));
                    }

                    instances[x, y] = currentInstance;
                    previousWasHit = true;
                } else {
                    previousWasHit = false;
                }
            }
        }

        /* merge vertically if they have the same dimensions */

        for (int y = 0; y < countY - 1; y++) { // -1 for skipping last row
            for (int x = 0; x < countX; x++) {
                GameObject bottomInstance = instances[x, y];
                GameObject topInstance = instances[x, y + 1];

                if (bottomInstance != null && topInstance != null) {
                    if (bottomInstance != topInstance && bottomInstance.transform.localScale.x == topInstance.transform.localScale.x) {
                        
                        //merge! enlarge bottom instance...

                        bottomInstance.transform.localScale = new Vector3(bottomInstance.transform.localScale.x, bottomInstance.transform.localScale.y + 1.0f, 0.0f);
                        bottomInstance.transform.Translate(new Vector3(0.0f, 0.5f, 0.0f));

                        // ...destroy top instance, save to instances array

                        for (int i = 0; i < Mathf.RoundToInt(topInstance.transform.localScale.x); i++) {
                            instances[x + i, y + 1] = instances[x + i, y];
                        }

                        DestroyImmediate(topInstance);
                    }
                }
            }
        }

        Debug.Log("ShadowCasters generated.");

        /* return shadow casters */

        List<GameObject> shadowCasterInstances = new List<GameObject>();

        for (int y = 0; y < countY; y++) {
            for (int x = 0; x < countX; x++) {
                GameObject instance = instances[x, y];
                if (instance != null && ! shadowCasterInstances.Contains(instance)) {
                    Vector2 scale = instance.transform.localScale;
                    Vector2 position = instance.transform.localPosition;

                    /*
                    if (FalseOrOoB(hits, x - 1, y, countX, countY)) {
                        scale.x -= litRadius;
                        position.x += litRadius / 2;
                    }
                    if (FalseOrOoB(hits, x + 1, y, countX, countY)) {
                        scale.x -= litRadius;
                        position.x -= litRadius / 2;
                    }
                    if (FalseOrOoB(hits, x, y - 1, countX, countY)) {
                        scale.y -= litRadius;
                        position.y += litRadius / 2;
                    }
                    if (FalseOrOoB(hits, x, y + 1, countX, countY)) {
                        scale.y -= litRadius;
                        position.y -= litRadius / 2;
                    }
                    */

                    instance.transform.localScale = scale;
                    instance.transform.localPosition = position;

                    shadowCasterInstances.Add(instance);
                }
            }
        }

        return shadowCasterInstances.ToArray();
    }

    private bool IsHit(Vector3Int pos) {
        return tilemap.GetTile(pos) != null;
    }

    private bool FalseOrOoB(bool[,] hits, int x, int y, int countX, int countY) {
        if (x >= 0 && y >= 0) {
            if (x < countX) {
                if (y < countY) {
                    return ! hits[x, y];
                }
            }
        }
        return true;
    }
}

[CustomEditor(typeof(GridShadowCastersGenerator))]
public class GridShadowCastersGeneratorEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate")) {
            GridShadowCastersGenerator generator = (GridShadowCastersGenerator)target;
            GameObject[] casters = generator.Generate();
            
            for (int i = 0; i < casters.Length; i++) {
                casters[i].name += "_" + i.ToString();
            }
        }
    }
}
