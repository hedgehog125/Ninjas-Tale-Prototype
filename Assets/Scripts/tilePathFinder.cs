using System;
using performance = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class tilePathFinder : MonoBehaviour {
	// These are public instead of serialised so they can also be modified by other scripts
	public Vector2 setTarget;
	public GameObject tilesObject;
	public int maxSearchDistance;
	public int maxAwaySearchDistance;
	public int maxTilesSearch;
	public int maxJumpHeight;
	public List<string> passableTiles;
	public List<int> actionTimes;

	public string state;
	public float findTime;

	private Hashtable passableTilesIndex = new Hashtable();
	private List<Vector2Int> activePath;
    private Tilemap tilemap;
	private CompositeCollider2D tilemapCollider;
	private Rigidbody2D rb;
	private int pathIndex;
	private int pathDelay;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()  {
        tilemap = tilesObject.GetComponent<Tilemap>();
		tilemapCollider = tilesObject.GetComponent<CompositeCollider2D>();

		IndexPassables();
		activePath = FindPath(transform.position, setTarget);
    }

    void FixedUpdate() {
		if (activePath != null && activePath.Count != 0) {
			if (pathDelay == 20) {
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
		performance.Stopwatch stopwatch = new performance.Stopwatch();
		stopwatch.Start();

		start -= new Vector2Int(1, 1);
		Vector3Int target3 = tilemap.WorldToCell(new Vector3(target.x, target.y));
		if (! isPassable(GetTileName(target3))) {
			state = "nonPassableTarget";
			findTime = stopwatch.ElapsedMilliseconds;
			return null;
        }
		List<Vector2Int> path = new List<Vector2Int>();
		if (
			start.x == target.x
			&& start.y == target.y
		) {
			state = "foundPath";
			findTime = stopwatch.ElapsedMilliseconds;
			path.Add(start);
			return path;
		}

		state = "deadEnd";
		Hashtable processed = new Hashtable();
		int time = 0;
		int count = 0;
		if (FindPathSub(start, target, processed, new Hashtable(), path, start, ref time, ref state, ref count)) {
			state = "foundPath";
			findTime = stopwatch.ElapsedMilliseconds;
			return path;
		}
		findTime = stopwatch.ElapsedMilliseconds;
		return null;
    }
    private bool FindPathSub(Vector2Int currentPosition2, Vector2Int target2, Hashtable processed, Hashtable newProcessed, List<Vector2Int> path, Vector2Int startPosition2, ref int time, ref string currentState, ref int count) {
		float distanceTravelledAway = Vector2Int.Distance(currentPosition2, target2) - Vector2Int.Distance(startPosition2, target2);
		Vector3Int currentPosition3 = tilemap.WorldToCell(new Vector3(currentPosition2.x, currentPosition2.y));
		Vector3Int target3 = tilemap.WorldToCell(new Vector3(target2.x, target2.y));

		string key = currentPosition2.x + "," + currentPosition2.y;
		if (processed[key] != null && (int)processed[key] == 1) {
			currentState = "deadEndProcessed";
			return false;
		}
		processed[key] = 1;
		newProcessed[key] = 1;
		count++;

		if (Vector3Int.Distance(currentPosition3, target3) > maxSearchDistance) {
			currentState = "reachedMaxSearch";
			return false;
		}
		if (distanceTravelledAway > maxAwaySearchDistance) {
			currentState = "reachedMaxTravelledAway";
			return false;
		}
		if (count > maxTilesSearch) {
			currentState = "reachedMaxProcessed";
			return false;
		}
		if (! tilemapCollider.bounds.Contains(currentPosition3)) {
			currentState = "deadEndOoB";
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
			currentState = "deadEnd";
			return false;
		}


		List<Vector2Int>[] newPaths = new List<Vector2Int>[2];
		Hashtable[] newProcessedDeluxe = new Hashtable[2];
		int[] newTimes = new int[2];
		int pathCount = 1;
		bool[] outputs = new bool[2];

		bool processedJump = false;
		bool[] alreadyTried = new bool[4];
		float min;
		int minIndex = -1;
		// Shortest valid route in the short term
		for (int i = 0; i < index; i++) { // This is less efficient than using a proper sorting algorithm, however, most of the time it won't need to be fully sorted
			if (i == jumpIndex) processedJump = true;
			min = Mathf.Infinity;
			minIndex = -1;
			for (int c = 0; c < index; c++) {
				float distance = Mathf.Abs(Vector2Int.Distance(target2, directions[c]));
				if (distance <= min && (! alreadyTried[c])) {
					min = distance;
					minIndex = c;
				}
			}
			alreadyTried[minIndex] = true;

			newPaths[0] = new List<Vector2Int>();
			newProcessedDeluxe[0] = new Hashtable();
			int directionType = directionTypes[minIndex];
			newTimes[0] = actionTimes[directionType];

			outputs[0] = FindPathSub(directions[minIndex], target2, processed, newProcessedDeluxe[0], newPaths[0], startPosition2, ref newTimes[0], ref currentState, ref count);

			if (outputs[0] && (jumpIndex == -1 || processedJump)) {
				foreach (Vector2Int item in newPaths[0]) {
					path.Add(item);
				}
				return true;
			}
			foreach (DictionaryEntry item in newProcessedDeluxe[0]) {
				processed[item.Key] = 0;
			}
			if (outputs[0]) {
				break;
            }
			if (i + 1 == index) return false;
		}
		if (processedJump) return false;

		// Possibly shorter overall, jumping can require getting further away initially
		if (newProcessed.Count <= maxTilesSearch) {
			if (jumpIndex != -1 && minIndex != jumpIndex) {
				newPaths[1] = new List<Vector2Int>();
				newProcessedDeluxe[1] = new Hashtable();
				newTimes[1] = actionTimes[2];

				Hashtable alreadyProcessed = new Hashtable(processed);
				foreach (DictionaryEntry item in newProcessed) {
					alreadyProcessed[item.Key] = 1;
				}
				outputs[1] = FindPathSub(directions[jumpIndex], target2, alreadyProcessed, newProcessedDeluxe[1], newPaths[1], startPosition2, ref newTimes[1], ref currentState, ref count);
				foreach (DictionaryEntry item in newProcessedDeluxe[1]) {
					processed[item.Key] = 0;
				}
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
		if (minIndex == -1) return false;

		foreach (Vector2Int item in newPaths[minIndex]) {
			path.Add(item);
		}
		foreach (DictionaryEntry item in newProcessedDeluxe[minIndex]) {
			processed[item.Key] = 1;
		}
		time += newTimes[minIndex];
		return true;
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
