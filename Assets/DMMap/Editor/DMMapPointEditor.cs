using UnityEngine;
using System.Collections;
using UnityEditor;

namespace DMM {
    [CustomEditor(typeof(DMMapPoint))]
    public class DMMapPointEditor : Editor {

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (GUILayout.Button("New Point")) {
                DMMapPoint p = (DMMapPoint)target;
                GameObject v = p.parentShape.NewPoint();
                UnityEditor.Selection.activeGameObject = v;
            }
        }
    }
}


