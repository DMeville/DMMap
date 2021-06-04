using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DMM;

public class DMMapUIControls : MonoBehaviour {

    public Transform followTarget;

    public void Awake() {
       // EventSystem.current.sendNavigationEvents = false;
    }

    public void SetZoom(float value) {
        DMMap.instance.configs[DMMap.instance.loadedConfig].zoom = value;
    }

    public void ToggleRotate(bool value) {
        DMMap.instance.configs[DMMap.instance.loadedConfig].rotate = value;
        if(followTarget != null) DMMap.instance.configs[DMMap.instance.loadedConfig].objectToFocusOn = followTarget;
    }

    public void ToggleFollow(bool value) {
        if (value) {
            if (followTarget != null) DMMap.instance.configs[DMMap.instance.loadedConfig].objectToFocusOn = followTarget;
        } else {
            DMMap.instance.configs[DMMap.instance.loadedConfig].objectToFocusOn = null;
        }
    }

    public void SetOpacity(float value) {
        DMMap.instance.configs[DMMap.instance.loadedConfig].opacity = value;
    }

    public void SetBackgroundOpacity(float value) {
        Color c = DMMap.instance.configs[DMMap.instance.loadedConfig].mapBackgroundColor;
        c.a = value;
        DMMap.instance.configs[DMMap.instance.loadedConfig].mapBackgroundColor = c;
    }

    public void SetMinimap() {
        DMMap.instance.LoadConfig(0);
    }

    public void SetFullscreen() {
        DMMap.instance.LoadConfig(1);
    }

    public void NextDemo() {
        int loadedLevel = Application.loadedLevel;
        loadedLevel++;
        if (loadedLevel >= 4) loadedLevel = 0;
        Application.LoadLevel(loadedLevel);
    }
    public bool toggle = false;

    public void Update() {
        //if (Input.GetKeyDown(KeyCode.Space)) {
        //    toggle = !toggle;
        //    DMMap.instance.gameObject.SetActive(toggle);
        //}
    }
}
