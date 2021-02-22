using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMM {
    public class DMMapWaypointDemo : MonoBehaviour {

        public GameObject waypoint;

        void Start() {

        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.Q)) {
                DMMap.instance.CreateWaypoint(waypoint, Input.mousePosition);
                
            }
        }
    }
}


