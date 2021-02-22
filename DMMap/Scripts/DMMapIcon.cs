using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace DMM {
    public class DMMapIcon : MonoBehaviour {
        /// <summary>
        /// The icon texture.  
        /// </summary>
        public Sprite icon;
        /// <summary>
        /// The icon tint
        /// </summary>
        public Color32 tint;

        /// <summary>
        /// The gameobject we store a reference to.  This gameobject has a uGUI Image component, and is rendered in the DMMap Canvas.
        /// </summary>
        //[HideInInspector]
        public GameObject iconGO;
        /// <summary>
        /// If true, the icon will shrink as the map zooms out, and grow as the map zooms in.
        /// </summary>
        public bool scaleWithZoom = true;
        private Material mat;
        /// <summary>
        /// If true, the icon with rotate as the gameobject rotates.  Useful for player icons, to show which way the are facing.
        /// </summary>
        public bool rotate = false;
        /// <summary>
        /// If true, the icon with rotate with the map.  
        /// If false, the icon with not rotate with the map.  Useful for icons that must not rotate (words, numbers, symbols)
        /// </summary>
        public bool rotateWithMap = false;

        [HideInInspector]
        public Vector3 mapPosition = Vector3.zero;

        public bool useDirectionIndicator = false;
        public Sprite directionIcon;
        public float rotationOffset = -90f;

        /// <summary>
        /// Layer the icon should render at for multi-level maps.  (so icons only show up on one floor of your dungeon, not all of them)
        /// -1 is shown on all levels
        /// </summary>
        public int layer = -1; 

        public void Start() {
            if (LayerMask.NameToLayer("DMMap") == -1) {
                Debug.LogError("[DMMap] - Layer is missing.  See the readme.txt");
            }
            if (DMMap.instance == null) return; //icons exist in this scene, but no map does... Silently failing

            //DMMap.instance.CreateIconContainer();
            DMMap.instance.icons.Add(this);
            iconGO = new GameObject();
            iconGO.name = ("DMMapIcon_" + this.gameObject.name);
            iconGO.layer = LayerMask.NameToLayer("DMMap");
            iconGO.transform.parent = DMMap.instance.iconContainer.transform;

            Image img = iconGO.AddComponent<Image>();
            img.sprite = icon;
            img.color = tint;

            //Vector3 pos = this.gameObject.transform.position;
            RotateIcon();

            iconGO.transform.localScale = new Vector3(DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale, DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale, DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale);
        }


        private void OnDisable() {
            if(iconGO != null) this.iconGO.SetActive(false);
        }

        private void OnEnable() {
            if (iconGO != null) this.iconGO.SetActive(true);
        }

        private void RotateIcon() {
            if (rotateWithMap && rotate) {

                float angle = 0f;
                switch (DMMap.instance.orientation) {
                    case MapOrientation.XY:
                        angle = this.gameObject.transform.rotation.eulerAngles.z - DMMap.instance.DMMapCamera.transform.eulerAngles.z;
                        break;
                    case MapOrientation.XZ:
                        angle = -this.gameObject.transform.rotation.eulerAngles.y + DMMap.instance.DMMapCamera.transform.eulerAngles.y;
                        break;
                    case MapOrientation.YZ:
                        angle = this.gameObject.transform.rotation.eulerAngles.x + DMMap.instance.DMMapCamera.transform.eulerAngles.x;
                        break;
                }
                iconGO.GetComponent<Image>().transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);


            } else {
                if (rotate) {
                    float angle = 0f;
                    switch (DMMap.instance.orientation) {
                        case MapOrientation.XY:
                            angle = this.gameObject.transform.rotation.eulerAngles.z;
                            break;
                        case MapOrientation.XZ:
                            angle = -this.gameObject.transform.rotation.eulerAngles.y;
                            break;
                        case MapOrientation.YZ:
                            angle = -this.gameObject.transform.rotation.eulerAngles.x;
                            break;
                    }

                    iconGO.GetComponent<Image>().transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                } else if (rotateWithMap) {
                    float angle = 0f;
                    switch (DMMap.instance.orientation) {
                        case MapOrientation.XY:
                            angle = -DMMap.instance.DMMapCamera.transform.eulerAngles.z;
                            break;
                        case MapOrientation.XZ:
                            angle = DMMap.instance.DMMapCamera.transform.eulerAngles.y;
                            break;
                        case MapOrientation.YZ:
                            angle = -DMMap.instance.DMMapCamera.transform.eulerAngles.x;
                            break;
                    }

                    iconGO.GetComponent<Image>().transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                } else {
                    iconGO.GetComponent<Image>().transform.rotation = Quaternion.AngleAxis(0f, Vector3.forward);
                }
            }
        }

        public void UpdateIcons() {
            mapPosition = DMMap.instance.WorldtoUI(this.gameObject.transform.position);
            //since camera will always be aligned to the mapImage UI, just us that

            mapPosition.z = 0f;
            if (useDirectionIndicator) {
                float threshold = DMMap.instance.configs[DMMap.instance.loadedConfig].iconDistanceThreshold;
                //if the icon is further away from the center than allowed by the threshold value
                //this can be extended (or doing a similar check) to have the icon fade off or something.  
                //Perhaps by extending this class and overriding this function
                if (mapPosition.magnitude > threshold) {
                    iconGO.GetComponent<Image>().sprite = directionIcon;
                    mapPosition = Vector3.ClampMagnitude(mapPosition, threshold);
                    this.iconGO.transform.localPosition = mapPosition;
                    float angle = Mathf.Atan2(mapPosition.y, mapPosition.x) + (Mathf.Deg2Rad*rotationOffset);
                    iconGO.GetComponent<Image>().transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg*angle, Vector3.forward);
                } else {
                    RotateIcon();
                    iconGO.GetComponent<Image>().sprite = icon;
                    this.iconGO.transform.localPosition = mapPosition;
                }
            } else {
                RotateIcon();
                this.iconGO.transform.localPosition = mapPosition;
            }

            float scale;
            float zoom;
            switch (DMMap.instance.configs[DMMap.instance.loadedConfig].iconScaleMode) {
                case IconScaleMode.NoScale:
                    scale = DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale;
                    iconGO.transform.localScale = new Vector3(scale, scale, scale);
                    break;
                case IconScaleMode.ScaleWithZoom:
                    scale = DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale;
                    zoom = DMMap.instance.configs[DMMap.instance.loadedConfig].zoom;
                    if (Mathf.Abs(zoom) < 0.0001) {
                        zoom = 0.001f * Mathf.Sign(zoom); //prevent x/0 = inf errors
                    }
                    iconGO.transform.localScale = new Vector3(scale + (1 / zoom), scale + (1 / zoom), scale + (1 / zoom));
                    break;
                case IconScaleMode.DefinedPerIcon:
                    if (this.scaleWithZoom) {
                        scale = DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale;
                        zoom = DMMap.instance.configs[DMMap.instance.loadedConfig].zoom;
                        if (Mathf.Abs(zoom) < 0.0001) {
                            zoom = 0.001f * Mathf.Sign(zoom); //prevent x/0 = inf errors
                        }
                        iconGO.transform.localScale = new Vector3(scale + (1 / zoom), scale + (1 / zoom), scale + (1 / zoom));
                    } else {
                        scale = DMMap.instance.configs[DMMap.instance.loadedConfig].globalIconScale;
                        iconGO.transform.localScale = new Vector3(scale, scale, scale);
                    }
                    break;
            }
        }

        public void OnDestroy() {
            if (DMMap.instance == null) {
                return;
            }
            if (DMMap.instance.icons != null) {
                DMMap.instance.icons.Remove(this);
                DestroyImmediate(iconGO);
            }
        }
    }
}