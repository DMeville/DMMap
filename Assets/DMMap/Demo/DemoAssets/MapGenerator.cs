using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DMM;

public class MapGenerator : MonoBehaviour {
    public GameObject tile;
    public int iterations = 200;

    //private bool done = false;
    private bool generating = false;
    private List<GameObject> tiles = new List<GameObject>();

	void Start () {
	    
	}

    void OnGUI() {
        if (GUI.Button(new Rect((Screen.width / 2) - 75, (Screen.height - 50), 150, 30), "Start Level Generation") && !generating) {
            for (int i = 0; i < tiles.Count; i++) {
                DestroyImmediate(tiles[i]);
            }
            tiles = new List<GameObject>();
            generating = true;

            for (int i = 0; i < iterations; i++) {
                Vector3 pos = new Vector3(Random.Range(-25f, 25f), 0f, Random.Range(-25f, 25f));
                GameObject obj = (GameObject)Instantiate(tile);
                obj.transform.position = pos;
                tiles.Add(obj);
                obj.transform.parent = this.transform;
                obj.transform.localScale = new Vector3(Random.Range(1f, 10f), Random.Range(1f, 10f), 1f);
                generating = false;
            }
            DMMap.instance.Generate();
        }
    }
	
}
