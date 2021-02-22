using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DMM {
    public class DPolygon {

        public List<DMMTriangleNet.Geometry.Vertex> points = new List<DMMTriangleNet.Geometry.Vertex>();
        public List<Vector2> edgeNormals = new List<Vector2>();
        public List<Vector2> vNormals = new List<Vector2>();
        public List<DPolygon> holes = new List<DPolygon>();
        public bool isHole = false;

        public override string ToString() {
            return ("Points: " + points.Count + " | holes: " + holes.Count + " | isHole: " + isHole);
        }
    }
}
