using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DMM;

[RequireComponent(typeof(DMMapIcon))]
public class DMMapIconLabel : MonoBehaviour {

    public Vector3 offset;
    public Color color;
    public string text;
    public Font font;

    private Text ui;

    void Awake() {
        GameObject go = new GameObject("Icon_Label");
        ui = go.AddComponent<Text>();
        ui.text = text;
        ui.color = color;
        ui.font = font;
    }

	void Start () {
	}
	
	void Update () {
        if (DMMap.instance == null) return;
        if (DMMap.instance.configs[DMMap.instance.loadedConfig].name == "Fullscreen") {
            ui.enabled = true;
        } else {
            ui.enabled = false;
        }

        ui.rectTransform.SetParent(DMMap.instance.iconContainer.transform, false);
        ui.transform.localPosition = this.gameObject.GetComponent<DMMapIcon>().iconGO.transform.localPosition + offset;
	}
}
