using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace DMM {
    public class IconFlash : MonoBehaviour {

        private Image ui;
        public float flashSpeed = 1f;
        private float t = 0f;

        void Start() {
        }

        void Update() {
            if (DMMap.instance == null) return;
            if (ui == null) { 
                ui = this.gameObject.GetComponent<DMMapIcon>().iconGO.GetComponent<Image>();
            }
            t += Time.deltaTime*flashSpeed;
            Color c = ui.color;
            c.a = Mathf.Clamp01(Mathf.Cos(Mathf.Deg2Rad * t));
            ui.color = c;
        }
    }

}
