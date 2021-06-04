using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using ClipperLib;
using DMMTriangleNet.Geometry;
using UnityEngine.UI;

namespace DMM {
    public class DMMap : MonoBehaviour {
        #region Private Variables
        [HideInInspector]
        public List<DMMapShape> shapes = new List<DMMapShape>();
        [HideInInspector]
        public List<DMMapIcon> icons = new List<DMMapIcon>();
        private List<DMMapShape> additive = new List<DMMapShape>();
        private List<DMMapShape> subtractive = new List<DMMapShape>();
        private float yNormalOffset = 0.1f;
        private GameObject meshContainer;
        private GameObject[] meshLayers;
        [HideInInspector]
        public static DMMap instance {
            get {
                if (_instance == null) {
                    _instance = (DMMap)FindObjectOfType(typeof(DMMap));
                }
                return _instance;
            }
            set {
                _instance = value;
            }
        }
        private static DMMap _instance = null;
        [HideInInspector]
        public RenderTexture mainRenderTexture;
        public RenderTexture fullscreenRenderTexture;
        private bool started = false;
        private bool initialized = false;
        #endregion

        /// <summary>
        /// Toggle this off to disable all debug messages.
        /// </summary>
        public bool DebugMessages = true;

        [Header("Required Objects ")]
        // These 5 objects should be set by default.  If they are not please see the docs on how to fix this! (or reimport the DMMap package!)

        /// <summary>
        /// The material used for the generated meshes.  This Should be set to the material found in DMMap/Materials/2DPolygonOutline
        /// </summary>
        public List<Material> defaultMaterial;

        /// <summary>
        /// The camera used to render the minimap.  This should be set to the Camera object found on the DMMap prefab "DMMapCamera"
        /// DMMap > DMMapCamera
        /// </summary>
        public Camera DMMapCamera;


        /// <summary>
        /// The canvas object used by DMMap.
        /// </summary>
        public Canvas canvas;

        /// <summary>
        /// The container that all created icons are added to
        /// </summary>
        public GameObject iconContainer;

        /// <summary>
        /// The image to use for displaying the rendered minimap.  This should be set to the RawImage found on the DMMap Prefab.
        /// DMMap > Canvas > Map
        /// </summary> 
        public RawImage mapImage;

        /// <summary>
        /// The image to use for displaying the overlay of the minimap.  This should be set to the RawImage found on the DMMap Prefab.
        /// DMMap > Canvas > Overlay
        /// </summary>
        public RawImage overlayImage;

        [Header("Map Options")]
        /// <summary>
        /// If true, map mesh generation will start automatically as the scene loads.
        /// Otherwise, if you want to use map meshes, you must either
        ///  - Call DMMap.instance.Generate() yourself, at some point
        ///  - Click the "Generate Map Mesh" button on the DMMap Component while in edit mode, which effectively precaches the meshes.
        /// </summary>
        public bool generateOnStart = true;

        /// <summary>
        /// This value is used when generating the map meshes, higher values mean more precise generation.
        /// This is because the clipping library uses int values for the points so we lose precision.
        /// A triangulationScale of 100 gives us 2 decimal points of precision, which is in most cases more than enough.
        /// </summary>
        public float triangulationScale = 100f;

        //
        public bool useCustomOrientation = false;
        //public Vector3 cameraOrientation = new Vector3(0, 1f, 0f); //default for top down, change this to any angle for something like orthographic
        //public float cameraOffset = 25f; //how far from the target (or 0,0,0) in the camraOrientation direction should the camera be placed?

        /// <summary>
        /// The orientation of the map.  Note that the map is always rendered facing the POSITIVE direction
        /// ie; using MapOrientation.XZ, the map will be rendered facing the +Y direction.
        /// </summary>
        public MapOrientation orientation = MapOrientation.XZ;

        
        /// <summary>
        /// This is the currently loaded config, which is set when calling DMMap.instance.LoadConfig(int).
        /// This is also the config which is loaded at startup.
        /// </summary>
        public int loadedConfig = 0;

        /// <summary>
        /// This is the list of configs used to set up different layouts of the same map.
        /// For example, you could create just one config and use that as your minimap.
        /// Or you could create two: One for a minimap and another for a fullscreen map, and toggle bettween them.
        /// The DMMapConfig class also holds all the useful properties that define the look and feel of the map (colors, zooms, etc)
        /// You can also change the properties on the config values to update the values in real time.
        /// ie; DMMap.instance.configs[loadedConfig].zoom += 10f; will increase the zoom.
        /// For a working example at this load any of the demo projects found in DMMap/Demo and take a look at the DMMapUIControls script (DMMap/Demos/DemoAssets/DMMapUIControls.cs)
        /// </summary>
        public List<DMMapConfig> configs = new List<DMMapConfig>();

        /// <summary>
        /// Loads a DMMapConfig and loads all the data into the current layout of the map
        /// </summary>
        /// <param name="config">The config to load</param>
        public void LoadConfig(int config) {
            if (config < 0 || config >= configs.Count) Debug.LogError(string.Format("[DMMap] - Error: Can not load config {0}, config missing", config));
            loadedConfig = config;
            configs[config].Apply();
        }

        /// <summary>
        /// Loads a DMMapConfig and loads all the data into the current layout of the map
        /// </summary>
        /// <param name="config">The config to load</param>
        public void LoadConfig(string config) {
            for (int i = 0; i < configs.Count; i++) {
                if (configs[i].name == config) {
                    configs[i].Apply();
                    return;
                }
            }
            Debug.LogError(string.Format("[DMMap] - Error: Can not load config {0}, no config with that name", config));
        }
        /// <summary>
        /// Sets the current active layer, turning off all layers except for one.
        /// Using this function you can easily create multi-layer maps, and only show the map of the layer you are on.
        /// </summary>
        /// <param name="layer"> The layer to set as active.</param>
        public void SetActiveLayer(int layer) {
            DisableAllLayers();
            if (layer >= 0 && layer < meshLayers.Length) {
                if (meshLayers[layer] != null) {
                    meshLayers[layer].SetActive(true);
                }
            }
            for (int i = 0; i < icons.Count; i++) {
                if (icons[i].layer == -1 || icons[i].layer == layer) {
                    icons[i].gameObject.SetActive(true);
                }
            }
        }
        /// <summary>
        /// Disables all layers, resulting in no map layers being rendered.
        /// </summary>
        public void DisableAllLayers() {
            if (meshLayers.Length == 0) {
                return;
            }
            for (int i = 0; i < meshLayers.Length; i++) {
                if (meshLayers[i] != null) {
                    meshLayers[i].SetActive(false);
                }
            }
            for (int i = 0; i < icons.Count; i++) {
                icons[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Begins the generation of the map layers.  Can be called at runtime, or in the editor by clicking the "Generate Map Mesh" button on the DMMap Component
        /// </summary>
        public void Generate() {
            CreateMeshContainer();
            int highestLayer = 0;
            DMMapShape[] sha = GameObject.FindObjectsOfType<DMMapShape>();
            shapes = new List<DMMapShape>();
            for (int i = 0; i < sha.Length; i++) {
                shapes.Add(sha[i]);
            }

            for (int i = 0; i < shapes.Count; i++) {
                if (shapes[i].layer > highestLayer) {
                    highestLayer = shapes[i].layer;
                }
            }

            meshLayers = new GameObject[highestLayer + 1];
            Debug.Log("MeshLayers: " + meshLayers.Length);
            for (int i = 0; i < meshLayers.Length; i++) {
                GenerateMeshLayer(i);
            }
        }

        /// <summary>
        /// Transforms an icon worldposition to UI space so that it aligns with the Map UI component
        /// </summary>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        public Vector3 WorldtoUI(Vector3 worldPoint) {
            Vector3 camera = DMMapCamera.WorldToViewportPoint(worldPoint);
            camera = camera - new Vector3(0.5f, 0.5f, 0f);

            RectTransform imageRT = mapImage.GetComponent<RectTransform>();

            Vector3 scale = new Vector3(imageRT.rect.width, imageRT.rect.height, 0f);
            Vector3 r = new Vector3(camera.x * scale.x, camera.y * scale.y, camera.z * scale.z);

            return r;
        }
        /// <summary>
        /// Transforms an icon UI position to world-position
        /// </summary>
        /// <param name="screenPoint"></param> Usually Input.mouse.position
        /// <returns></returns>
        public Vector3 UIToWorld(Vector3 screenPoint) {
            RectTransform imageRT = mapImage.GetComponent<RectTransform>();
            //Debug.Log("screen point: " + screenPoint);
            //Debug.Log("UI center: " + imageRT.transform.position);

            Vector3 mapViewportPosition = new Vector3((screenPoint.x - imageRT.transform.position.x) / (imageRT.rect.width * canvas.scaleFactor),
                                                              (screenPoint.y - imageRT.transform.position.y) / (imageRT.rect.height * canvas.scaleFactor), 0f);
            Vector3 offset = new Vector3(0.5f, 0.5f, 0f);
            mapViewportPosition = mapViewportPosition + offset;
            Vector3 worldPoint = DMMap.instance.DMMapCamera.ViewportToWorldPoint(mapViewportPosition);
            return worldPoint;
        }

        /// <summary>
        /// Creates an instance of a gameobject at the world-position represented on the minimap.
        /// </summary>
        /// <param name="waypoint"></param> The gameobject you wish to instantiate (which has a DMMapIcon component.
        /// Using a gameobject here allows you to create a visual representation of the waypoint in your game (like a beam of light?)
        /// <param name="screenPoint"></param> the screen point of where you want to create the waypoint.  Usually Input.mousePosition.
        /// We convert this internally to the proper map-image coordinate space.
        /// <returns></returns>
        public bool CreateWaypoint(GameObject waypoint, Vector3 screenPoint) {
            Vector3 worldPoint = UIToWorld(screenPoint);
            RectTransform imageRT = mapImage.GetComponent<RectTransform>();

           if (Mathf.Abs(screenPoint.x - imageRT.transform.position.x) < (imageRT.rect.width * canvas.scaleFactor) / 2f &&
               Mathf.Abs(screenPoint.y - imageRT.transform.position.y) < (imageRT.rect.height * canvas.scaleFactor) / 2f) {
                   ((GameObject)Instantiate(waypoint)).transform.position = worldPoint;
               return true;
           } else {
               //waypoint creation failed because we didn't click within the map bounds
               return false;
           }
        }

        /// -----------------------------------------------------
        /// ----------- INTERNAL STUFF BELOW --------------------
        /// -----------------------------------------------------
        /// -----------------------------------------------------
        /// The stuff below is internal stuff you shouldn't have to worry about.
        /// It's *mostly* undocumenated, but feel free to poke around if you want! :D

        #region INTERNAL_STUFF

        public void UpdateMeshMaterials() {
            if (meshLayers == null) return;

            //lazy
            for (int i = 0; i < meshLayers.Length; i++) {
                try {
                    meshLayers[i].GetComponent<Renderer>().material = configs[loadedConfig].meshLayerMaterial[i];
                }
                catch {
                    try {
                        meshLayers[i].GetComponent<Renderer>().materials = defaultMaterial.ToArray();
                    }
                    catch {
                    }
                }
            }
        }

        public void DMMapDebug(string msg) {
            if (DebugMessages) {
                Debug.Log("[DMMap] - " + msg);
            }
        }
        public void Awake() {
            shapes = new List<DMMapShape>();
            icons = new List<DMMapIcon>();
            DMMap.instance = this;

            //if (LayerMask.NameToLayer("DMMap") == -1) {
            //    Debug.LogError("[DMMap] - Layer is missing.  See the readme.txt");
            //}
        }
        public void Start() {

            Initialize();
        }

        public void Initialize() {
            if (!initialized) {
                initialized = true;
                Setup();
            }
        }

        private void Setup() {
            if (configs.Count <= 0) Debug.LogError("[DMMap] - Error:  Need at least one config to load!");
            LayerMask layer = LayerMask.NameToLayer("DMMap");
            if (layer.value == -1) Debug.LogError("DMMap Layer was not found.  Please follow the instructions in the Readme.txt to set up a DMMap layer!");
            DMMapCamera.cullingMask = 1 << layer.value;

            started = true;
            LoadConfig(loadedConfig);

            this.gameObject.layer = LayerMask.NameToLayer("DMMap");
            DMMapCamera.gameObject.layer = LayerMask.NameToLayer("DMMap");

            SetupCamera();

            if (generateOnStart) {
                Generate();
            } else {
                int highestLayer = 0;
                for (int i = 0; i < shapes.Count; i++) {
                    if (shapes[i].layer > highestLayer) {
                        highestLayer = shapes[i].layer;
                    }
                }
                meshLayers = new GameObject[highestLayer + 1];
                Transform m_container = this.transform.Find("DMMap Mesh");
                Transform child;
                if (m_container != null) {
                    for (int i = 0; i < meshLayers.Length; i++) {
                        child = m_container.Find("Mesh_" + i);
                        if (child != null) {
                            meshLayers[i] = child.gameObject;
                        }
                    }
                }
            }

            UpdateMeshMaterials();

        }

        private void SetupCamera() {
            InitializeRenderTexture();
            DMMapCamera.targetTexture = mainRenderTexture;
            mapImage.texture = mainRenderTexture;
            mapImage.material.SetTexture("_MainTex", DMMapCamera.targetTexture);
            DMMapCamera.backgroundColor = configs[loadedConfig].mapBackgroundColor;
            DMMapCamera.orthographicSize = configs[loadedConfig].zoom;

            //setup default position

                Vector3 camPos = Vector3.zero;
                switch(orientation) {
                    case MapOrientation.XZ:
                        camPos.y = 25f;
                        break;
                    case MapOrientation.XY:
                        camPos.z = -25f;
                        break;
                    case MapOrientation.YZ:
                        camPos.x = 25f;
                        break;
                }
                GetCameraTarget().position = camPos;


                //setup default rotation
                Vector3 rot = new Vector3();
                Quaternion q = GetCameraTarget().rotation;
                switch(orientation) {
                    case MapOrientation.XZ:
                        rot = new Vector3(90f, 0f, 0f);
                        break;
                    case MapOrientation.XY:
                        rot = new Vector3(0f, 0f, 0f);
                        break;
                    case MapOrientation.YZ:
                        rot = new Vector3(0f, 270f, 0f);
                        break;
                }
                q.eulerAngles = rot;
                GetCameraTarget().rotation = q;
            

        }

        //if using custom orientation returns cam.parent, otherwise returns cam
        public Transform GetCameraTarget() {
            if(useCustomOrientation) return DMMapCamera.transform.parent;
            else return DMMapCamera.transform;
        }

        public void Update() {
            if (!started) {
                Setup();
            }

            //should probably not do this every frame
            if(configs[loadedConfig].name == "Fullscreen") {
                if(DMMapCamera.targetTexture != fullscreenRenderTexture) {
                    DMMapCamera.targetTexture = fullscreenRenderTexture;
                    mapImage.texture = fullscreenRenderTexture;
                    mapImage.material.SetTexture("_MainTex", DMMapCamera.targetTexture);
                }
            } else {
                if(DMMapCamera.targetTexture != mainRenderTexture) {
                    DMMapCamera.targetTexture = mainRenderTexture;
                    mapImage.texture = mainRenderTexture;
                    mapImage.material.SetTexture("_MainTex", DMMapCamera.targetTexture);
                }
            }

            //following and rotating a target
            if (configs[loadedConfig].objectToFocusOn != null) {
                //clamp the camera to specific values on a specific axis
                Vector3 camPos = new Vector3(configs[loadedConfig].objectToFocusOn.position.x, configs[loadedConfig].objectToFocusOn.position.y, configs[loadedConfig].objectToFocusOn.position.z);
                switch (orientation) {
                    case MapOrientation.XZ:
                        camPos.y = 25f;
                        break;
                    case MapOrientation.XY:
                        camPos.z = -25f;
                        break;
                    case MapOrientation.YZ:
                        camPos.x = 25f;
                        break;
                }

                GetCameraTarget().position = camPos;
                if (configs[loadedConfig].rotate) {
                    //depending on the MapOrientation rotate a different axis
                    Vector3 rot = new Vector3();
                    Quaternion q = DMMapCamera.transform.rotation;
                    switch (orientation) {
                        case MapOrientation.XZ:
                            rot = new Vector3(90f, configs[loadedConfig].objectToFocusOn.transform.rotation.eulerAngles.y, 0f);
                            break;
                        case MapOrientation.XY:
                            rot = new Vector3(0f, 0f, configs[loadedConfig].objectToFocusOn.transform.rotation.eulerAngles.z);
                            break;
                        case MapOrientation.YZ:
                            rot = new Vector3(0f, 270f, -configs[loadedConfig].objectToFocusOn.transform.rotation.eulerAngles.x);
                            break;
                    }
                    q.eulerAngles = rot;
                    GetCameraTarget().rotation = q;
                }
            }

            //set the zoom
            DMMapCamera.orthographicSize = configs[loadedConfig].zoom;

            //update opacity
            if (mapImage != null) {
                mapImage.material.SetFloat("_Opacity", configs[loadedConfig].opacity);
            }

            //update background color
            DMMapCamera.backgroundColor = configs[loadedConfig].mapBackgroundColor;

            //update icons 
            foreach (DMMapIcon i in icons) {
                i.UpdateIcons();
            }

        }

        private void InitializeRenderTexture() {
            DMMapDebug("Initializing RenderTexture");
            float rtscale = 0.25f;
            mainRenderTexture = new RenderTexture((int)(1024*rtscale), (int)(1024*rtscale), 24, RenderTextureFormat.ARGB32);
            mainRenderTexture.wrapMode = TextureWrapMode.Clamp;
            mainRenderTexture.antiAliasing = 8;

            mainRenderTexture.name = "DMMap_RenderTexture";
            mainRenderTexture.Create();

            //fullscreen render texture. Swaps to this when using a config with .name = "Fullscreen". This makes it so the minimap is smooth (antialiased) and the fullscreen map is still crisp
            fullscreenRenderTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            fullscreenRenderTexture.wrapMode = TextureWrapMode.Clamp;
            fullscreenRenderTexture.antiAliasing = 8;
            fullscreenRenderTexture.name = "DMMap_FullscreenRenderTexture";
            fullscreenRenderTexture.Create();
        }

        private void CreateMeshContainer() {
            if (meshContainer == null) {
                Transform child = this.transform.Find("DMMap Mesh");
                if (child != null) {
                    meshContainer = child.gameObject;
                } else {
                    meshContainer = new GameObject("DMMap Mesh");
                    meshContainer.layer = LayerMask.NameToLayer("DMMap");
                    meshContainer.transform.parent = this.transform;
                }
            }
        }

        private void GenerateMeshLayer(int layer = 0) {
            #region Generate
            DMMapDebug("Generating mesh layer [" + layer + "]");

            additive = new List<DMMapShape>();
            subtractive = new List<DMMapShape>();

            if (shapes.Count <= 0) {
                DMMapDebug("You can't generate a map with no shapes!");
                return;
            }

            DMMapDebug("Generating map with [" + shapes.Count + "] shapes");
            for (int i = 0; i < shapes.Count; i++) {
                if (shapes[i].mode == DrawMode.Additive && shapes[i].layer == layer) {
                    additive.Add(shapes[i]);
                } else if (shapes[i].mode == DrawMode.Subtractive && shapes[i].layer == layer) {
                    subtractive.Add(shapes[i]);
                }
            }
            if (DebugMessages) {
                DMMapDebug("Generating map with shapes: Additive [" + additive.Count + "] | Subtractive [" + subtractive.Count + "]");
            }
            if (additive.Count <= 0) {
                return;
            }


            Transform child = meshContainer.transform.Find("Mesh_" + layer);
            GameObject meshObject;
            if (child == null) {
                meshObject = new GameObject("Mesh_" + layer);
            } else {
                meshObject = child.gameObject;
            }
            meshObject.layer = LayerMask.NameToLayer("DMMap");
            meshObject.transform.parent = meshContainer.transform;
            meshLayers[layer] = meshObject;
            float layerYScale = 0.01f;

            switch (orientation) {
                case MapOrientation.XZ:
                    meshObject.gameObject.transform.localScale = new Vector3(1f, -1f, 1f);
                    meshObject.gameObject.transform.position = new Vector3(0f, 1f * layer*layerYScale, 0f);
                    break;
                case MapOrientation.XY:
                    meshObject.gameObject.transform.localScale = new Vector3(1f, -1f, 1f);
                    meshObject.gameObject.transform.position = new Vector3(0f, 0f, 1f * layer * layerYScale);
                    meshObject.gameObject.transform.rotation = Quaternion.AngleAxis(-90f, Vector3.right);

                    break;
                case MapOrientation.YZ:
                    meshObject.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                    meshObject.gameObject.transform.position = new Vector3(1f* layer * layerYScale, 0f, 0f);
                    meshObject.gameObject.transform.rotation = Quaternion.AngleAxis(90f, Vector3.forward);

                    break;
            }
            //Clipping

            Clipper c = new Clipper();
            //List<List<IntPoint>> subj = new List<List<IntPoint>>();
            //List<List<IntPoint>> clip = new List<List<IntPoint>>();
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            PolyTree ptSolution = new PolyTree();

            //the last execute needs to get the polytree computed..
            bool hasSubtractive = (subtractive.Count > 0) ? true : false;

            if (additive.Count > 0) {
                c.AddPath(ShapeToCPolygon(additive[0]), PolyType.ptSubject, true);
                if (additive.Count == 1) {
                    c.Execute(ClipType.ctUnion, ptSolution);
                }

            }

            for (int i = 1; i < additive.Count; i++) {
                if (i == additive.Count - 1 && !hasSubtractive) {
                    c.AddPath(ShapeToCPolygon(additive[i]), PolyType.ptClip, true);
                    c.Execute(ClipType.ctUnion, ptSolution);
                } else {
                    c.AddPath(ShapeToCPolygon(additive[i]), PolyType.ptClip, true);
                    c.Execute(ClipType.ctUnion, solution);
                    c.Clear();
                    c.AddPaths(solution, PolyType.ptSubject, true);
                }
            }

            if (hasSubtractive) {
                for (int i = 0; i < subtractive.Count; i++) {
                    if (i == subtractive.Count - 1) {
                        c.AddPath(ShapeToCPolygon(subtractive[i]), PolyType.ptClip, true);
                        c.Execute(ClipType.ctDifference, ptSolution);
                    } else {
                        c.AddPath(ShapeToCPolygon(subtractive[i]), PolyType.ptClip, true);
                        c.Execute(ClipType.ctDifference, solution);
                        c.Clear();
                        c.AddPaths(solution, PolyType.ptSubject, true);
                    }
                }
            }
            c.Clear();

            List<DPolygon> dPolygons = new List<DPolygon>();
            for (int childsCount = 0; childsCount < ptSolution.ChildCount; childsCount++) {
                PolyTreeToDPolygonList(ptSolution.Childs[childsCount], ref dPolygons);
            }

            //Triangulation
            List<DMMTriangleNet.Mesh> tMesh = new List<DMMTriangleNet.Mesh>();
            //List<DMMTriangleNet.Geometry.Polygon> tp = new List<Polygon>();
            for (int i = 0; i < dPolygons.Count; i++) {
                if (!dPolygons[i].isHole) {
                    DMMTriangleNet.Geometry.Polygon tnPoly = new Polygon();
                    tnPoly.AddContour(dPolygons[i].points, 0, false);
                    for (int j = 0; j < dPolygons[i].holes.Count; j++) {
                        tnPoly.AddContour(dPolygons[i].holes[j].points, 0, true);
                    }
                    DMMTriangleNet.Meshing.GenericMesher gm = new DMMTriangleNet.Meshing.GenericMesher();
                    gm.Triangulate(tnPoly);
                    tMesh.Add((DMMTriangleNet.Mesh)tnPoly.Triangulate());
                }
            }


            //Edge normals 
            #region Normals
            foreach (DPolygon poly in dPolygons) {
                for (int i = 0; i < poly.points.Count; i++) {
                    int prev, next = 0;
                    prev = i - 1;
                    next = i + 1;
                    if (prev < 0) {
                        prev = poly.points.Count - 1;
                    }
                    if (next >= poly.points.Count) {
                        next = 0;
                    }

                    Vector2 eNormal = CalculateOutwardNormal(poly.points[i], poly.points[prev], poly.points[next]);
                    eNormal.Normalize();
                    poly.edgeNormals.Add(eNormal);
                }
                foreach (DPolygon hole in poly.holes) {
                    for (int i = 0; i < hole.points.Count; i++) {
                        int prev, next = 0;
                        prev = i - 1;
                        next = i + 1;
                        if (prev < 0) {
                            prev = hole.points.Count - 1;
                        }
                        if (next >= hole.points.Count) {
                            next = 0;
                        }

                        Vector2 eNormal = CalculateOutwardNormal(hole.points[i], hole.points[prev], hole.points[next]);
                        eNormal.Normalize();
                        hole.edgeNormals.Add(eNormal);
                    }
                }
            }

            //vertex normals
            for (int j = 0; j < dPolygons.Count; j++) {
                DPolygon poly = dPolygons[j];
                for (int i = 0; i < poly.points.Count; i++) {
                    int prev, next = 0;
                    prev = i - 1;
                    next = i + 1;
                    if (prev < 0) {
                        prev = poly.points.Count - 1;
                    }
                    if (next >= poly.points.Count) {
                        next = 0;
                    }
                    Vector2 sum = poly.edgeNormals[i] + poly.edgeNormals[next];
                    sum.Normalize();
                    poly.vNormals.Add(sum);
                }
                foreach (DPolygon hole in poly.holes) {
                    for (int i = 0; i < hole.points.Count; i++) {
                        int prev, next = 0;
                        prev = i - 1;
                        next = i + 1;
                        if (prev < 0) {
                            prev = hole.points.Count - 1;
                        }
                        if (next >= hole.points.Count) {
                            next = 0;
                        }
                        Vector2 sum = hole.edgeNormals[i] + hole.edgeNormals[next];
                        sum.Normalize();
                        hole.vNormals.Add(sum);
                    }
                }
            }
            #endregion

            //Mesh calculation
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            //List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            Mesh mesh = new Mesh();

            for (int i = 0; i < dPolygons.Count; i++) {
                for (int j = 0; j < tMesh[i].Triangles.Count(); j++) {


                    Vertex p = tMesh[i].vertices[tMesh[i].Triangles.ElementAt(j).P0];
                    Vector3 p3 = new Vector3();
                    p3 = new Vector3((float)p.x / triangulationScale, 0f, (float)p.y / triangulationScale);

                    //switch (orientation) {
                    //    case MapOrientation.XZ:
                    //        p3 = new Vector3((float)p.x / triangulationScale, 0f, (float)p.y / triangulationScale);
                    //        break;
                    //    case MapOrientation.XY:
                    //        p3 = new Vector3((float)p.x / triangulationScale, (float)p.y / triangulationScale, 0f);
                    //        break;
                    //    case MapOrientation.YZ:
                    //        p3 = new Vector3(0f, (float)p.x / triangulationScale, (float)p.y / triangulationScale);
                    //        break;
                    //}


                    if (verts.Contains(p3)) {
                        int index = verts.IndexOf(p3);
                        tris.Add(index);
                    } else {
                        verts.Add(p3);
                        tris.Add(verts.Count - 1);
                        Vector2 norm = Vector2.zero;
                        if (dPolygons[i].points.Contains(p)) {
                            norm = dPolygons[i].vNormals[dPolygons[i].points.IndexOf(p)];
                        } else {
                            foreach (DPolygon hole in dPolygons[i].holes) {
                                if (hole.points.Contains(p)) {
                                    norm = hole.vNormals[hole.points.IndexOf(p)];
                                    break;
                                }
                            }
                        }

                        if (norm == Vector2.zero) {
                            Debug.LogWarning("Normal was null... there was a problem finding it...");
                        }
                        normals.Add(new Vector3(norm.x, yNormalOffset, norm.y));

                        //switch (orientation) {
                        //    case MapOrientation.XZ:
                        //        normals.Add(new Vector3(norm.x, yNormalOffset, norm.y));
                        //        break;
                        //    case MapOrientation.XY:
                        //        normals.Add(new Vector3(norm.x, norm.y, -yNormalOffset));
                        //        break;
                        //    case MapOrientation.YZ:
                        //        normals.Add(new Vector3(-yNormalOffset, norm.x, norm.y));
                        //        break;
                        //}
                    }

                    //point 2

                    p = tMesh[i].vertices[tMesh[i].Triangles.ElementAt(j).P1];
                    p3 = new Vector3((float)p.x / triangulationScale, 0f, (float)p.y / triangulationScale);

                    //switch (orientation) {
                    //    case MapOrientation.XZ:
                    //        p3 = new Vector3((float)p.x / triangulationScale, 0f, (float)p.y / triangulationScale);
                    //        break;
                    //    case MapOrientation.XY:
                    //        p3 = new Vector3((float)p.x / triangulationScale, (float)p.y / triangulationScale, 0f);
                    //        break;
                    //    case MapOrientation.YZ:
                    //        p3 = new Vector3(0f, (float)p.x / triangulationScale, (float)p.y / triangulationScale);
                    //        break;
                    //}
                    if (verts.Contains(p3)) {
                        int index = verts.IndexOf(p3);
                        tris.Add(index);
                    } else {
                        verts.Add(p3);
                        tris.Add(verts.Count - 1);
                        Vector2 norm = Vector2.zero;
                        if (dPolygons[i].points.Contains(p)) {
                            norm = dPolygons[i].vNormals[dPolygons[i].points.IndexOf(p)];
                        } else {
                            foreach (DPolygon hole in dPolygons[i].holes) {
                                if (hole.points.Contains(p)) {
                                    norm = hole.vNormals[hole.points.IndexOf(p)];
                                    break;
                                }
                            }
                        }

                        if (norm == Vector2.zero) {
                            Debug.LogWarning("Normal was null... there was a problem finding it...");
                        }
                        normals.Add(new Vector3(norm.x, yNormalOffset, norm.y));

                        //switch (orientation) {
                        //    case MapOrientation.XZ:
                        //        normals.Add(new Vector3(norm.x, yNormalOffset, norm.y));
                        //        break;
                        //    case MapOrientation.XY:
                        //        normals.Add(new Vector3(norm.x, norm.y, -yNormalOffset));
                        //        break;
                        //    case MapOrientation.YZ:
                        //        normals.Add(new Vector3(-yNormalOffset, norm.x, norm.y));
                        //        break;
                        //}
                    }

                    //point 3
                    p = tMesh[i].vertices[tMesh[i].Triangles.ElementAt(j).P2];
                    p3 = new Vector3((float)p.x / triangulationScale, 0f, (float)p.y / triangulationScale);

                    //switch (orientation) {
                    //    case MapOrientation.XZ:
                    //        p3 = new Vector3((float)p.x / triangulationScale, 0f, (float)p.y / triangulationScale);
                    //        break;
                    //    case MapOrientation.XY:
                    //        p3 = new Vector3((float)p.x / triangulationScale, (float)p.y / triangulationScale, 0f);
                    //        break;
                    //    case MapOrientation.YZ:
                    //        p3 = new Vector3(0f, (float)p.x / triangulationScale, (float)p.y / triangulationScale);
                    //        break;
                    //}
                    if (verts.Contains(p3)) {

                        int index = verts.IndexOf(p3);
                        tris.Add(index);

                    } else {
                        verts.Add(p3);
                        tris.Add(verts.Count - 1);
                        Vector2 norm = Vector2.zero;
                        if (dPolygons[i].points.Contains(p)) {
                            norm = dPolygons[i].vNormals[dPolygons[i].points.IndexOf(p)];
                        } else {
                            foreach (DPolygon hole in dPolygons[i].holes) {
                                if (hole.points.Contains(p)) {
                                    norm = hole.vNormals[hole.points.IndexOf(p)];
                                    break;
                                }
                            }
                        }

                        if (norm == Vector2.zero) {
                            Debug.LogWarning("Normal was null... there was a problem finding it...");
                        }
                        normals.Add(new Vector3(norm.x, yNormalOffset, norm.y));

                        //switch (orientation) {
                        //    case MapOrientation.XZ:
                        //        normals.Add(new Vector3(norm.x, yNormalOffset, norm.y));
                        //        break;
                        //    case MapOrientation.XY:
                        //        normals.Add(new Vector3(norm.x, norm.y, -yNormalOffset));
                        //        break;
                        //    case MapOrientation.YZ:
                        //        normals.Add(new Vector3(-yNormalOffset, norm.x, norm.y));
                        //        break;
                        //}
                    }

                }
            }

            mesh.vertices = verts.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = tris.ToArray();

            MeshFilter mf = meshObject.GetComponent<MeshFilter>();
            if (mf == null) {
                mf = meshObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshrenderer = meshObject.GetComponent<MeshRenderer>();
            if (meshrenderer == null) {
                meshrenderer = meshObject.AddComponent<MeshRenderer>();
                meshrenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshrenderer.receiveShadows = false;
            }


            meshrenderer.materials = defaultMaterial.ToArray();
            mf.mesh = mesh;
            DMMapDebug("Map Generation Complete: Layer[" + layer + "]");

            #endregion
        }

        #region MapMeshGen Helper functions
        private Vector2 CalculateOutwardNormal(Vertex _a, Vertex _b, Vertex _c) {
            Vector2 a = new Vector2((float)_a.X, (float)_a.Y);
            Vector2 b = new Vector2((float)_b.X, (float)_b.Y);
            Vector2 c = new Vector2((float)_c.X, (float)_c.Y);

            Vector2 AB = b - a;
            Vector2 AC = c - a;
            Vector2 nAB = new Vector2(-AB.y, AB.x);
            float dot = Vector2.Dot(nAB, AC);

            if (dot > 0) {
                return nAB;
            } else {
                return nAB;
            }
        }
        private void AddPolygonWithHoles2(PolyNode node, ref Polygon p) {
            List<Vertex> contour = new List<Vertex>();
            for (int i = 0; i < node.Contour.Count; i++) {
                contour.Add(new Vertex((double)node.Contour[i].X, (double)node.Contour[i].Y));
            }

            p.AddContour(contour, 0, node.IsHole);
            for (int i = 0; i < node.ChildCount; i++) {
                AddPolygonWithHoles2(node.Childs[i], ref p);
            }
        }
        private void PolyTreeToDPolygonList(PolyNode node, ref List<DPolygon> dPolygons) {

            DPolygon poly = new DPolygon();
            poly.points = new List<Vertex>();

            poly.isHole = false;
            if (!node.IsHole) {
                dPolygons.Add(poly);
            }

            for (int i = 0; i < node.Contour.Count; i++) {
                poly.points.Add(new Vertex((double)node.Contour[i].X, (double)node.Contour[i].Y));
            }
            for (int i = 0; i < node.ChildCount; i++) {
                if (node.Childs[i].IsHole) {
                    DPolygon hole = new DPolygon();
                    for (int j = 0; j < node.Childs[i].Contour.Count; j++) {
                        hole.points.Add(new Vertex((double)node.Childs[i].Contour[j].X, (double)node.Childs[i].Contour[j].Y));
                    }
                    poly.holes.Add(hole);
                }
                PolyTreeToDPolygonList(node.Childs[i], ref dPolygons);
            }
        }

        private List<IntPoint> ShapeToCPolygon(DMMapShape shape) {
            List<IntPoint> points = new List<IntPoint>();
            for (int i = 0; i < shape.verts.Count; i++) {
                switch (orientation) {
                    case MapOrientation.XZ:
                        points.Add(new IntPoint(shape.verts[i].transform.position.x * triangulationScale, shape.verts[i].transform.position.z * triangulationScale));
                        break;
                    case MapOrientation.XY:
                        points.Add(new IntPoint(shape.verts[i].transform.position.x * triangulationScale, shape.verts[i].transform.position.y * triangulationScale));
                        break;
                    case MapOrientation.YZ:
                        points.Add(new IntPoint(shape.verts[i].transform.position.y * triangulationScale, shape.verts[i].transform.position.z * triangulationScale));
                        break;

                }

            }
            return points;
        }
        #endregion

        private void DrawPolytree(List<DPolygon> plist, bool drawNormals = false) {
            foreach (DPolygon p in plist) {
                for (int i = 0; i < p.points.Count - 1; i++) {
                    Vector3 p0 = new Vector3();
                    Vector3 p1 = new Vector3();

                    switch (orientation) {
                        case MapOrientation.XZ:
                            p0 = new Vector3((float)p.points[i].X, 10f, (float)p.points[i].Y);
                            p1 = new Vector3((float)p.points[i + 1].X, 10f, (float)p.points[i + 1].Y);
                            break;
                        case MapOrientation.XY:
                            p0 = new Vector3((float)p.points[i].X, (float)p.points[i].Y, 10f);
                            p1 = new Vector3((float)p.points[i + 1].X, (float)p.points[i + 1].Y, 10f);
                            break;

                        case MapOrientation.YZ:
                            p0 = new Vector3(10f, (float)p.points[i].X, (float)p.points[i].Y);
                            p1 = new Vector3(10f, (float)p.points[i + 1].X, (float)p.points[i + 1].Y);
                            break;
                    }

                    Debug.DrawLine(p0, p1, Color.cyan, float.MaxValue);

                    //draw edge normals
                    if (drawNormals) {
                        Vector3 dir = new Vector3(p.vNormals[i].x, 0f, p.vNormals[i].y);
                        Debug.DrawRay(p0, dir * 100f, Color.white, float.MaxValue);
                    }
                }
                Vector3 p2 = new Vector3();
                Vector3 p3 = new Vector3();
                switch (orientation) {
                    case MapOrientation.XZ:
                        p2 = new Vector3((float)p.points[0].X, 10f, (float)p.points[0].Y);
                        p3 = new Vector3((float)p.points[p.points.Count - 1].X, 10f, (float)p.points[p.points.Count - 1].Y);
                        break;
                    case MapOrientation.XY:
                        p2 = new Vector3((float)p.points[0].X, (float)p.points[0].Y, 10f);
                        p3 = new Vector3((float)p.points[p.points.Count - 1].X, (float)p.points[p.points.Count - 1].Y, 10f);
                        break;

                    case MapOrientation.YZ:
                        p2 = new Vector3(10f, (float)p.points[0].X, (float)p.points[0].Y);
                        p3 = new Vector3(10f, (float)p.points[p.points.Count - 1].X, (float)p.points[p.points.Count - 1].Y);
                        break;
                }

                Debug.DrawLine(p2, p3, Color.cyan, float.MaxValue);


                if (p.holes != null) {
                    foreach (DPolygon h in p.holes) {
                        for (int i = 0; i < h.points.Count - 1; i++) {
                            Vector3 p0 = new Vector3();
                            Vector3 p1 = new Vector3();
                            switch (orientation) {
                                case MapOrientation.XZ:
                                    p0 = new Vector3((float)p.points[i].X, 10f, (float)p.points[i].Y);
                                    p1 = new Vector3((float)p.points[i + 1].X, 10f, (float)p.points[i + 1].Y);
                                    break;
                                case MapOrientation.XY:
                                    p0 = new Vector3((float)p.points[i].X, (float)p.points[i].Y, 10f);
                                    p1 = new Vector3((float)p.points[i + 1].X, (float)p.points[i + 1].Y, 10f);
                                    break;

                                case MapOrientation.YZ:
                                    p0 = new Vector3(10f, (float)p.points[i].X, (float)p.points[i].Y);
                                    p1 = new Vector3(10f, (float)p.points[i + 1].X, (float)p.points[i + 1].Y);
                                    break;
                            }
                            Debug.DrawLine(p0, p1, Color.red, float.MaxValue);

                            if (drawNormals) {
                                Vector3 dir = new Vector3(h.vNormals[i].x, 0f, h.vNormals[i].y);
                                Debug.DrawRay(p0, dir * 100f, Color.white, float.MaxValue);
                            }

                        }
                        Vector3 p4 = new Vector3();
                        Vector3 p5 = new Vector3();
                        switch (orientation) {
                            case MapOrientation.XZ:
                                p4 = new Vector3((float)p.points[0].X, 10f, (float)p.points[0].Y);
                                p5 = new Vector3((float)p.points[p.points.Count - 1].X, 10f, (float)p.points[p.points.Count - 1].Y);
                                break;
                            case MapOrientation.XY:
                                p4 = new Vector3((float)p.points[0].X, (float)p.points[0].Y, 10f);
                                p5 = new Vector3((float)p.points[p.points.Count - 1].X, (float)p.points[p.points.Count - 1].Y, 10f);
                                break;

                            case MapOrientation.YZ:
                                p4 = new Vector3(10f, (float)p.points[0].X, (float)p.points[0].Y);
                                p5 = new Vector3(10f, (float)p.points[p.points.Count - 1].X, (float)p.points[p.points.Count - 1].Y);
                                break;
                        }
                        Debug.DrawLine(p4, p5, Color.red, float.MaxValue);

                    }
                }
            }
        }
        #endregion
    }

    public enum IconScaleMode {
        ScaleWithZoom,
        NoScale,
        DefinedPerIcon
    }

    public enum MapOrientation {
        XZ,
        XY,
        YZ,
    }
}

