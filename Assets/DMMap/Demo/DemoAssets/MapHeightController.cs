using UnityEngine;
using System.Collections;
using DMM;

public class MapHeightController : MonoBehaviour {

	void Start () {
	
	}
	
	void Update () {
        if (this.gameObject.transform.position.y <= 14f) {
            DMMap.instance.SetActiveLayer(0);
        } else if (this.gameObject.transform.position.y <= 30f) {
            DMMap.instance.SetActiveLayer(1);
        } else {
            DMMap.instance.SetActiveLayer(2);
        }
	}
}
