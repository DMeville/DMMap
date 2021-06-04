using UnityEngine;
using System.Collections;

namespace DMM {
    [ExecuteInEditMode]
    [System.Serializable]
    public class DMMapPoint : MonoBehaviour {
        [HideInInspector]
        public DMMapShape parentShape;

        public void OnDestroy() {
            if(parentShape != null) parentShape.RemovePoint(this);
            parentShape = null;
        }
    }
}
