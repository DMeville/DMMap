using UnityEngine;
using System.Collections;
using UnityEditor;

namespace DMM {
    [CustomEditor(typeof(DMMapShape))]
    public class DMMapShapeEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (GUILayout.Button("New point")) {
                DMMapShape mms = (DMMapShape)target;
                GameObject v = mms.NewPoint();
                UnityEditor.Selection.activeGameObject = v;
            }
            if (GUILayout.Button("Restore Parent Shapes References")) {
                DMMapShape mms = (DMMapShape)target;

                for (int i = 0; i < mms.verts.Count; i++) {
                    mms.verts[i].GetComponent<DMMapPoint>().parentShape = mms;
                }
            }
        }
    }
}