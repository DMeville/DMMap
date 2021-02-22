using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace DMM {
    [System.Serializable]
    public class DMMapConfig {
        /// <summary>
        /// A name for the config.  This can be used to load the config by name.  DMMap.instance.LoadConfig("Mininmap");
        /// </summary>
        public string name = "Minimap";
        /// <summary>
        /// A list of the materials used for rendering the mesh.  the material at index 0 will be assigned to the mesh at layer = 0
        /// If no material is found for a corresponding layer DMMap.instance.defaultMaterial is used.
        /// Note that transparency is NOT yet supported between layers.  
        /// (as it causes problems with the rendertexture and I'm no shader master ;D)
        /// </summary>
        public List<Material> meshLayerMaterial = new List<Material>();

        /// <summary>
        /// The background colour of the map. (transparency supported!) 
        /// </summary>
        public Color mapBackgroundColor;

        /// <summary>
        /// The opacity of the entire map.  Use this to make a semi-transparent map to overlay your game!
        /// </summary>
        [Range(0f, 1f)]
        public float opacity = 1f;

        /// <summary>
        /// The mask of the map,  Use this to create circular maps.
        /// The mask uses the alpha channel of a texture.
        /// </summary>
        public Sprite mask;

        /// <summary>
        /// The overlay of the map.  Use this to overlay graphics ontop of your map.
        /// </summary>
        public Texture2D overlay;

        /// <summary>
        /// The position of this map, using the uGUI RectTransform component.
        /// Create a new empty game object as a child of DMMap > Canvas object. 
        /// Then assign values and drag this instance to this box!
        /// (see docs for more info!)
        /// </summary>
        public RectTransform uiPosition;

        /// <summary>
        /// The zoom level of the map.  The smaller the number the larger the map appears
        /// </summary>
        public float zoom = 1f;

        /// <summary>
        /// Should the map rotate?  Uses the "objectToFocusOn" facing direction to determine map rotation.
        /// </summary>
        public bool rotate = false;

        /// <summary>
        /// An object to focus on.  Assign your player to this to have the map center on and follow the player as they move
        /// </summary>
        public Transform objectToFocusOn;

        //Icons
        /// <summary>
        /// The global scale of the icons
        /// </summary>
        public float globalIconScale = 10f;
        /// <summary>
        /// The scale mode of the icons.
        /// IconScaleMode.ScaleWithZoom scales the icons with the zoom level of the map.  The further you zoom out, the smaller the icons will be.
        /// IconScaleMode.NoScale does not scale the icons with the zoom.  Regardless of the map zoom level, the icons always remain the same size.
        /// IconScaleMode.DefinedPerIcon allows the icons to set their own scale mode.  This allows you to mix and match scale modes.  This is set on the DMMapIcon component, scaleWithZoom bool.
        /// </summary>
        public IconScaleMode iconScaleMode;

        public float iconDistanceThreshold = 1f;
        /// <summary>
        /// This is the distance from center that the icon needs to be before switching to it's "direction indicator" icon.  
        /// This is a screenspace measurement
        /// </summary>

        public void Apply() {
            if (mask != null) {
                DMMap.instance.mapImage.material.SetTexture("_Mask", mask.texture);
            } else {
                DMMap.instance.mapImage.material.SetTexture("_Mask", null);
            }

            DMMap.instance.iconContainer.GetComponent<Image>().sprite = mask;
            if (overlay == null) {
                DMMap.instance.overlayImage.gameObject.SetActive(false);
            } else {
                DMMap.instance.overlayImage.gameObject.SetActive(true);
                DMMap.instance.overlayImage.texture = overlay;
            }

            //aligning to the new UI position
            if (uiPosition != null) {
                DMMap.instance.mapImage.rectTransform.anchoredPosition = uiPosition.anchoredPosition;
                DMMap.instance.mapImage.rectTransform.anchoredPosition3D = uiPosition.anchoredPosition3D;
                DMMap.instance.mapImage.rectTransform.anchorMax = uiPosition.anchorMax;
                DMMap.instance.mapImage.rectTransform.anchorMin = uiPosition.anchorMin;
                DMMap.instance.mapImage.rectTransform.offsetMax = uiPosition.offsetMax;
                DMMap.instance.mapImage.rectTransform.offsetMin = uiPosition.offsetMin;
                DMMap.instance.mapImage.rectTransform.pivot = uiPosition.pivot;
                DMMap.instance.mapImage.rectTransform.position = uiPosition.position;
                DMMap.instance.mapImage.rectTransform.rotation = uiPosition.rotation;
                DMMap.instance.mapImage.rectTransform.localScale = uiPosition.localScale;
                DMMap.instance.mapImage.rectTransform.sizeDelta = uiPosition.sizeDelta;

                RectTransform ic = DMMap.instance.iconContainer.GetComponent<RectTransform>();
                ic.anchoredPosition = uiPosition.anchoredPosition;
                ic.anchoredPosition3D = uiPosition.anchoredPosition3D;
                ic.anchorMax = uiPosition.anchorMax;
                ic.anchorMin = uiPosition.anchorMin;
                ic.offsetMax = uiPosition.offsetMax;
                ic.offsetMin = uiPosition.offsetMin;
                ic.pivot = uiPosition.pivot;
                ic.position = uiPosition.position;
                ic.rotation = uiPosition.rotation;
                ic.localScale = uiPosition.localScale;
                ic.sizeDelta = uiPosition.sizeDelta;

                DMMap.instance.overlayImage.rectTransform.anchoredPosition = uiPosition.anchoredPosition;
                DMMap.instance.overlayImage.rectTransform.anchoredPosition3D = uiPosition.anchoredPosition3D;
                DMMap.instance.overlayImage.rectTransform.anchorMax = uiPosition.anchorMax;
                DMMap.instance.overlayImage.rectTransform.anchorMin = uiPosition.anchorMin;
                DMMap.instance.overlayImage.rectTransform.offsetMax = uiPosition.offsetMax;
                DMMap.instance.overlayImage.rectTransform.offsetMin = uiPosition.offsetMin;
                DMMap.instance.overlayImage.rectTransform.pivot = uiPosition.pivot;
                DMMap.instance.overlayImage.rectTransform.position = uiPosition.position;
                DMMap.instance.overlayImage.rectTransform.rotation = uiPosition.rotation;
                DMMap.instance.overlayImage.rectTransform.localScale = uiPosition.localScale;
                DMMap.instance.overlayImage.rectTransform.sizeDelta = uiPosition.sizeDelta;
            }

            //assign any custom materials on the mesh layers.
            DMMap.instance.UpdateMeshMaterials();
            DMMap.instance.Update();
        }
    }
}
