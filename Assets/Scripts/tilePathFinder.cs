using System;
using performance = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class tilePathFinder : MonoBehaviour {
	public Vector2 setTarget;
	public GameObject tilesObject;
	public int maxSearchDistance;
	public int maxAwaySearchDistance;
	public int maxTilesSearch;
	public int maxJumpHeight;
	public List<string> passableTiles;
	public List<int> actionTimes;
	public string state;
	public GameObject pathSquare;

	private Hashtable passableTilesIndex = new Hashtable();
	private List<Vector2Int> activePath;
    private Tilemap tilemap;
    private Rigidbody2D rb;
	private int pathIndex;
	private int pathDelay;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()  {
        tilemap = tilesObject.GetComponent<Tilemap>();

		IndexPassables();
		activePath = FindPath(transform.position, setTarget);
    }

    void FixedUpdate() {
		if (activePath != null && activePath.Count != 0) {
			if (pathDelay == 10) {
				pathDelay = 0;
				pathIndex++;
				if (pathIndex == activePath.Count) {
					pathIndex = 0;
				}
				transform.position = activePath[pathIndex] + new Vector2(0.5f, 0.5f);
			}
			else {
				pathDelay++;
			}
		}
    }

	private void MoveAlongPath() {
		
	}

	private List<Vector2Int> FindPath(Vector3 start, Vector2 target) {
		return FindPath(new Vector2Int((int)start.x, (int)start.y), new Vector2Int((int)target.x, (int)target.y));
	}

	private List<Vector2Int> FindPath(Vector2Int start, Vector2Int target) {
		Vector3Int target3 = tilemap.WorldToCell(new Vector3(target.x, target.y));
		if (! isPassable(GetTileName(target3))) {
			state = "nonPassableTarget";
			return null;
        }

		state = "findingPath";
		Hashtable processed = new Hashtable();
		List<Vector2Int> path = new List<Vector2Int>();
		int time = 0;

		Instantiate(pathSquare, new Vector3(target.x + 0.5f, target.y + 0.5f), Quaternion.identity);

		int count = 0;
		if (FindPathSub(start - new Vector2Int(1, 1), target, processed, new Hashtable(), path, 0.0f, ref time, ref state, ref count)) {
			state = "foundPath";
			return path;
		}
		return null;
    }
    private bool FindPathSub(Vector2Int currentPosition2, Vector2Int target2, Hashtable oldProcessed, Hashtable newProcessed, List<Vector2Int> path, float distanceTravelledAway, ref int time, ref string state, ref int count) {
		Vector3Int currentPosition3 = tilemap.WorldToCell(new Vector3(currentPosition2.x, currentPosition2.y));
		Vector3Int target3 = tilemap.WorldToCell(new Vector3(target2.x, target2.y));

		string key = currentPosition2.x + "," + currentPosition2.y;
		if (oldProcessed[key] != null) {
			state = "deadEnd";
			return false;
		};
		newProcessed[key] = 1;
		count++;

		GameObject square = Instantiate(pathSquare, new Vector3(currentPosition2.x + 0.5f, currentPosition2.y + 0.5f), Quaternion.identity);
		float size = Mathf.Min((count / 150.0f) + 0.1f, 1);
		square.transform.localScale = new Vector3(size, size, 1);
		square.transform.Rotate(new Vector3(0, 0, -((count * 20) % 90)));

		if (Vector3Int.Distance(currentPosition3, target3) > maxSearchDistance) {
			state = "reachedMaxSearch";
			return false;
		}
		if (distanceTravelledAway > maxAwaySearchDistance) {
			state = "reachedMaxTravelledAway";
			return false;
		}
		if (count > maxTilesSearch) {
			state = "reachedMaxProcessed";
			return false;
		}

		Vector2Int[] directions = new Vector2Int[4];
		int[] directionTypes = new int[4];
		Vector2Int direction2 = new Vector2Int();
		path.Add(currentPosition2);
		if (
			currentPosition2.x == target2.x
			&& currentPosition2.y == target2.y
		) return true;

		int index = 0;
		int jumpIndex = -1;

		if (TestDirection(currentPosition2, Vector2Int.left, ref direction2)) {
			directions[index] = direction2;
			directionTypes[index] = 0;
			index++;
		}
		if (TestDirection(currentPosition2, Vector2Int.right, ref direction2)) {
			directions[index] = direction2;
			directionTypes[index] = 1;
			index++;
		}

		if (isPassable(GetTileName(currentPosition3 + Vector3Int.down))) { // Can just fall
			directions[index] = currentPosition2 + Vector2Int.down;
			directionTypes[index] = 3;
			index++;
		}
		else {
			direction2 = Vector2Int.up;
			if (isPassable(GetTileName(currentPosition3 + (Vector3Int)direction2))) {
				int i = 1;
				while (isPassable(GetTileName(currentPosition3 + (Vector3Int)direction2))) {
					direction2 += Vector2Int.up;
					i++;
					if (i > maxJumpHeight) break;
				}

				directions[index] = currentPosition2 + direction2;
				jumpIndex = index;
				directionTypes[index] = 2;
				index++;
			}
		}

		if (index == 0) {
			state = "deadEnd";
			return false;
		}


		List<Vector2Int>[] newPaths = new List<Vector2Int>[2];
		Hashtable[] newProcessedDeluxe = new Hashtable[2];
		int[] newTimes = new int[2];
		int pathCount = 1;
		bool[] outputs = new bool[2];

		float min;
		int minIndex = -1;
		List<Vector2Int> newPath;
		// Shortest valid route in the short term
		for (int i = 0; i < index; i++) { // This is less efficient than using a proper sorting algorithm, however, most of the time it won't need to be fully sorted
			min = Mathf.Infinity;
			minIndex = -1;
			for (int c = i; c < index; c++) {
				float distance = Vector2Int.Distance(target2, directions[c]);
				if (distance < min) {
					min = distance;
					minIndex = c;
				}
			}

			newPath = new List<Vector2Int>();
			newProcessedDeluxe[0] = new Hashtable();
			int directionType = directionTypes[minIndex];
			newTimes[0] = actionTimes[directionType];

			Hashtable alreadyProcessed = new Hashtable(oldProcessed);
			foreach (DictionaryEntry item in newProcessed) {
				alreadyProcessed[item.Key] = 1;
            }
			outputs[0] = FindPathSub(directions[minIndex], target2, alreadyProcessed, newProcessedDeluxe[0], newPath, GetDistanceTravelledAway(distanceTravelledAway, currentPosition2, target2, directions[minIndex]), ref newTimes[0], ref state, ref count);
			newPaths[0] = newPath;
			if (outputs[0]) {
				break;
            }
			if (i + 1 == index) {
				state = "deadEnd";
				return false;
            }
		}

		// Possibly shorter overall, jumping can require getting further away initially
		if (newProcessed.Count <= maxTilesSearch) {
			if (jumpIndex != -1 && minIndex != jumpIndex) {
				newPath = new List<Vector2Int>();
				newProcessedDeluxe[1] = new Hashtable();
				newTimes[1] = actionTimes[2];

				Hashtable alreadyProcessed = new Hashtable(oldProcessed);
				foreach (DictionaryEntry item in newProcessed) {
					alreadyProcessed[item.Key] = 1;
				}
				outputs[1] = FindPathSub(directions[jumpIndex], target2, alreadyProcessed, newProcessedDeluxe[1], newPath, GetDistanceTravelledAway(distanceTravelledAway, currentPosition2, target2, directions[jumpIndex]), ref newTimes[1], ref state, ref count);
				newPaths[1] = newPath;
				pathCount++;
			}
		}
		
		min = Mathf.Infinity;
		minIndex = -1;
		for (int i = 0; i < pathCount; i++) {
			int value = newTimes[i];
			if (outputs[i] && value < min) {
				min = value;
				minIndex = i;
			}
		}
		if (minIndex == -1) {
			state = "deadEnd";
			return false;
		}

		foreach (Vector2Int item in newPaths[minIndex]) {
			path.Add(item);
		}
		foreach (DictionaryEntry item in newProcessedDeluxe[minIndex]) {
			newProcessed[item.Key] = 1;
		}
		time += newTimes[minIndex];
		return true;
	}
	private float GetDistanceTravelledAway(float original, Vector2Int currentPosition2, Vector2Int target2, Vector2Int newPoint) {
		return original - Vector2Int.Distance(
			newPoint, target2
		).CompareTo(
			Vector2Int.Distance(
				currentPosition2, target2
			)
		);
	}

    private string GetTileName(Vector3Int tileCoord) {
        TileBase tile = tilemap.GetTile(tileCoord);
        if (tile) return tile.name;
        return null;
    }

	private void IndexPassables() {
		foreach (string i in passableTiles) {
			passableTilesIndex[i] = "1";
		}
	}
	private bool isPassable(string tileName) {
		if (tileName == null) return true;
		if ((string)passableTilesIndex[tileName] == "1") return true;
		return false;
	}
	private bool TestDirection(Vector2Int currentPosition2, Vector2Int testDirection2, ref Vector2Int direction2) {
		Vector3Int testDirection3 = (Vector3Int)testDirection2;
		Vector3Int currentPosition3 = (Vector3Int)currentPosition2;
		bool canMove = false;
		if (isPassable(GetTileName(currentPosition3 + Vector3Int.down))) {
			if (isPassable(GetTileName(currentPosition3 + testDirection3 + Vector3Int.down))){
				direction2 = currentPosition2 + testDirection2 + Vector2Int.down;
				canMove = true;
            }
		}
		else if (isPassable(GetTileName(currentPosition3 + testDirection3))) {
			direction2 = currentPosition2 + testDirection2;
			canMove = true;
		}
		return canMove;
	}
}
