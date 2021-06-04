using UnityEngine;
using System.Collections;

namespace DMM {
    [ExecuteInEditMode]
    [System.Serializable]
    public class DMMapPoint : MonoBehaviour {
        [HideInInspector]
        public DMMapShape parentShape;

        public void OnDestroy() {
            parentShape.RemovePoint(this);
            parentShape = null;
        }
    }
}
