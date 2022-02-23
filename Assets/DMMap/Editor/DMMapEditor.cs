using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace DMM {
    [CustomEditor(typeof(DMMap))]
    [System.Serializable]
    public class DMMapEditor : Editor {

        public override void OnInspectorGUI() {
            DMMap mm = (DMMap)target;
            DrawDefaultInspector();

            if (GUILayout.Button("Generate Map Mesh", GUILayout.Height(20f))) {
                mm.Generate();
            }
        }
    }
}