using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMM {
    public class DMMapShape : MonoBehaviour {

        //[HideInInspector]
        public List<GameObject> verts = new List<GameObject>();
        /// <summary>
        /// The draw mode of the shape.
        /// Additive shapes are ALL combined first.
        /// Then all subtractive shapes are subtracted from the additive result.
        /// </summary>
        public DrawMode mode;
        /// <summary>
        /// The layer of the map.  All shapes will be processed with all other shapes that share the same layer value.
        /// Useful for creating multi-floor levels.  (Legend of Zelda dungeons, for example)
        /// </summary>
        public int layer = 0;

        public void OnDrawGizmos() {
            Color c;
            if (mode == DrawMode.Additive) {
                c = new Color(0f, 1f - (layer / 100f), 0f);
            } else if (mode == DrawMode.Subtractive) {
                c = new Color(1f - (layer / 100f), 0f, 0f);
            } else {
                c = Color.blue;
            }
            Gizmos.color = c;
            if (verts.Count >= 2) {
                for (int i = 0; i < verts.Count - 1; i++) {
                    Gizmos.DrawLine(verts[i].transform.position, verts[i + 1].transform.position);
                }
                Gizmos.DrawLine(verts[verts.Count - 1].transform.position, verts[0].transform.position);
            }
        }

        void Update() {

        }

        public GameObject NewPoint() {
            //Vector3 pos = this.gameObject.transform.position;
            //pos.y = DMMap.instance.shapePlacementHeight;
            //this.gameObject.transform.position = pos;
            GameObject v = new GameObject("mappoint_" + verts.Count);
            DMMapPoint p = v.AddComponent<DMMapPoint>();
            p.parentShape = this;
            verts.Add(v);
            v.transform.parent = this.transform;
            v.transform.localPosition= Vector3.zero;
            v.transform.localScale = Vector3.one;
            return v;
        }

        public GameObject NewPoint(Vector3 pos) {
            GameObject v = NewPoint();
            v.transform.position = pos;
            return v;
        }

        public void RemovePoint(DMMapPoint point) {
            List<GameObject> r = new List<GameObject>();
            foreach (GameObject g in verts) {
                if (g.GetComponent<DMMapPoint>() == point) {
                    r.Add(g);
                }
            }
            foreach (GameObject g in r) {
                verts.Remove(g);
            }
        }
        public void SetupBaseShape() {
            Collider col = GetComponent<Collider>();
            if (col)
            {
                NewPoint(new Vector3(col.bounds.min.x, col.bounds.max.y, col.bounds.min.z));
                NewPoint(new Vector3(col.bounds.min.x, col.bounds.max.y, col.bounds.max.z));
                NewPoint(new Vector3(col.bounds.max.x, col.bounds.max.y, col.bounds.max.z));
                NewPoint(new Vector3(col.bounds.max.x, col.bounds.max.y, col.bounds.min.z));
            }
            else {
                Debug.LogWarning("No Collider found on GameObject " + gameObject.name + " Please add collider component for functionality");
            }

        }
        public void OnDestroy() {
            if (DMMap.instance == null) {
                return;
            }
            if (DMMap.instance != null) {
                DMMap.instance.shapes.Remove(this);
            }
        }
    }

    public enum DrawMode {
        Additive,
        Subtractive
    }
}
