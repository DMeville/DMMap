using UnityEngine;
using DMM;

namespace DMM {
    public class AssignMapFocus : MonoBehaviour {
        void Start() {
            if (DMMap.instance != null) {
                DMMap.instance.configs[0].objectToFocusOn = this.transform;
            }
        }
    }
}