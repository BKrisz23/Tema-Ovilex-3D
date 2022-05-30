using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using PG.GameBalance;

namespace PG
{
    /// <summary>
    /// Base window for creating vehicles.
    /// You can see how to prepare a car model in the documentation.
    /// </summary>
    public abstract class CreateVehicleWindow :EditorWindow
    {
        public abstract VehicleController RefController { get; }
        public abstract VehicleController SelectedController { get; }

        //Lights
        [Tooltip("Glass material, to determine the material index (If the field is null, then the index will be 0)")]
        public Material GlassMaterial;
        [Tooltip("Broken glass material (if the field is null, then after destroy the glass will disappear)")]
        public Material BrokenGlassMaterial;
        [Tooltip("Lights material, to determine the material index (If the field is null, then the index will be 0)")]
        public Material LightsMaterial;
        [Tooltip("Broken glass material (if the field is null, then after destroy the glass will disappear)")]
        public Material BrokenLightsMaterial;
        [Tooltip("The material of the enabled light, you can duplicate the LightsMaterial and add Emission to it.")]
        public Material OnLightsMaterial;
        [Tooltip("Car from which to copy parameters (Engine, steering, gearbox)")]
        public CarLightType LightType;

        //Other
        public string ColliderPrefix = "_Collider";

        //Helper variables
        protected Editor Editor;
        protected EditorHelperSettings EditorHelperSettings => EditorHelperSettings.GetSettings;
        protected bool PartsFoldout;
        protected bool LightsFoldout;
        protected bool WheelsFoldout;
        protected bool OtherFoldout;
        protected Vector2 scrollPos = Vector2.zero;

        protected Transform ViewTransform;
        protected Transform WheelsTransform;
        protected Dictionary<string, Transform> PartsDict = new Dictionary<string, Transform>();

        protected ScriptableObject Target;
        protected SerializedObject SerializedObject;

        SerializedProperty ColliderPrefixProperty;
        SerializedProperty GlassMaterialProperty;
        SerializedProperty BrokenGlassMaterialProperty;
        SerializedProperty LightsMaterialProperty;
        SerializedProperty BrokenLightsMaterialProperty;
        SerializedProperty OnLightsMaterialProperty;
        SerializedProperty LightTypeProperty;

        protected virtual void OnEnable ()
        {
            if (Target == null)
            {
                Debug.LogError ("Target is null");
                Target = this;
            }
            if (SerializedObject == null)
            {
                Debug.LogError ("SerializedObject is null");
                SerializedObject = new SerializedObject (Target);
            }

            ColliderPrefixProperty = SerializedObject.FindProperty ("ColliderPrefix");
            GlassMaterialProperty = SerializedObject.FindProperty ("GlassMaterial");
            BrokenGlassMaterialProperty = SerializedObject.FindProperty ("BrokenGlassMaterial");
            LightsMaterialProperty = SerializedObject.FindProperty ("LightsMaterial");
            BrokenLightsMaterialProperty = SerializedObject.FindProperty ("BrokenLightsMaterial");
            OnLightsMaterialProperty = SerializedObject.FindProperty ("OnLightsMaterial");
            LightTypeProperty = SerializedObject.FindProperty ("LightType");
        }

        protected virtual void OnGUI ()
        {
            if (Editor == null)
            {
                Editor = UnityEditor.Editor.CreateEditor (this);
            }

            EditorGUILayout.HelpBox ("You can see how to prepare a vehicle model in the documentation.", MessageType.Info);

            if (SelectedController != null)
            {
                if (ViewTransform == null || !ViewTransform.IsChildOf (SelectedController.transform))
                {
                    ViewTransform = SelectedController.transform.Find ("View");
                    if (ViewTransform == null)
                    {
                        ViewTransform = new GameObject ("View").transform;
                        ViewTransform.SetParent (SelectedController.transform);
                        ViewTransform.localPosition = Vector3.zero;
                        ViewTransform.localRotation = Quaternion.identity;
                    }
                    PartsDict.Clear ();
                }
                
                OnGUIParts ();
                OnGUILights ();
                OnGUIWheels ();
                OnGUIOther ();
            }
        }

        #region Parts

        protected virtual void OnGUIParts () 
        {
            EditorGUILayout.LabelField ("Collider settings:");

            EditorGUILayout.PropertyField (ColliderPrefixProperty);

            if (GUILayout.Button ("Convert all MeshColliders"))
            {
                var meshRenderers = SelectedController.transform.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in meshRenderers)
                {
                    if (renderer.name.Contains (ColliderPrefix))
                    {
                        var collider = renderer.GetComponent<MeshCollider>();
                        if (collider == null)
                        {
                            collider = renderer.gameObject.AddComponent<MeshCollider> ();
                        }
                        collider.convex = true;

                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null)
                        {
                            DestroyImmediate (filter);
                        }
                        DestroyImmediate (renderer);
                    }
                }
                EditorUtility.SetDirty (SelectedController);
            }
        }

        protected void GUIPartButton (EditorHelperSettings.VehiclePart carPart, int width = 100, int height = 50, System.Action onClickAction = null)
        {
            var style = new GUIStyle(GUI.skin.textField);
            style.padding.left = 1;
            style.padding.right = 1;
            style.padding.top = 1;
            style.padding.bottom = 1;
            EditorGUILayout.BeginHorizontal (style);

            style = new GUIStyle (GUI.skin.button);
            style.alignment = TextAnchor.MiddleLeft;
            style.padding.top = 10;
            style.padding.bottom = 10;
            if (GUILayout.Button (new GUIContent (carPart.PartCaption, carPart.PartButtonTexture, carPart.PartTooltip),
                style, GUILayout.MaxWidth (width), GUILayout.Height (height)))
            {
                CreatePart (carPart.PartName, carPart.PartPrefab);
                onClickAction.SafeInvoke ();
            }
            style = new GUIStyle (GUI.skin.button);
            style.padding.left = 2;
            style.padding.right = 2;
            style.padding.top = 2;
            style.padding.bottom = 2;

            var tooltip = string.Format ("Set the pivot point of the part to the pivot point of the selected object [{0}]",
                Selection.activeGameObject?
                Selection.activeGameObject.name:
                "Null"
                );

            if (GUILayout.Button (new GUIContent (EditorHelperSettings.PivotTex, tooltip),
                style, GUILayout.MaxWidth (20), GUILayout.Height (20)))
            {
                SetPivotForPart (carPart.PartName);
            }

            EditorGUILayout.EndHorizontal ();
        }

        protected void SetPivotForPart (string partName)
        {
            if (!Selection.activeGameObject)
            {
                Debug.LogError ("No active object");
                return;
            }

            Transform tr;

            if (!PartsDict.TryGetValue (partName, out tr))
            {
                tr = ViewTransform.Find (partName);
                if (tr == null)
                {
                    Debug.LogErrorFormat ("Does not have [{0}] part", partName);
                    return;
                }
            }

            var childs = new List<Transform>();

            for (int i = 0; i < tr.childCount; i++)
            {
                childs.Add (tr.GetChild (i));
            }

            childs.ForEach (c => c.SetParent (tr.parent));
            tr.localPosition = tr.parent.InverseTransformPoint (Selection.activeGameObject.transform.position);
            childs.ForEach (c => c.SetParent (tr));

            Selection.activeGameObject = tr.gameObject;
            EditorGUIUtility.PingObject (tr);
            EditorUtility.SetDirty (SelectedController);
        }

        void CreatePart (string partName, GameObject prefab)
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                Debug.LogErrorFormat ("No active objects selected for [{0}]", partName);
                return;
            }

            Transform partTR;

            if (!PartsDict.TryGetValue (partName, out partTR))
            {
                partTR = ViewTransform.Find (partName);
                if (partTR == null)
                {
                    GameObject partObj;
                    if (prefab)
                    {
                        partObj = Instantiate (prefab, ViewTransform);
                        partObj.name = partName;
                    }
                    else
                    {
                        partObj = new GameObject (partName);
                        partObj.transform.SetParent (ViewTransform);
                        partObj.transform.localPosition = Vector3.zero;
                        partObj.transform.localRotation = Quaternion.identity;
                    }

                    if (partName == EditorHelperSettings.Body.PartName)
                    {
                        partObj.transform.localPosition = Vector3.zero;
                        partObj.transform.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        partObj.transform.position = Selection.gameObjects[0].transform.position;
                        partObj.transform.rotation = Selection.gameObjects[0].transform.rotation;
                    }
                    
                    partTR = partObj.transform;

                    Debug.LogFormat ("Part [{0}] has create", partName);
                }
                PartsDict.Add (partName, partTR);
            }

            foreach (var go in Selection.gameObjects)
            {
                go.transform.SetParent (partTR);
            }

            Selection.activeGameObject = partTR.gameObject;
            EditorGUIUtility.PingObject (partTR);
            EditorUtility.SetDirty (SelectedController);
        }

        #endregion //Parts

        #region Lights

        void OnGUILights ()
        {
            EditorGUILayout.Space (10);
            GUILayout.BeginVertical (GUI.skin.textArea, GUILayout.MaxWidth (402));
            LightsFoldout = EditorGUILayout.Foldout (LightsFoldout, "-Lights Glass-");
            if (LightsFoldout)
            {
                EditorGUILayout.Space (10);

                EditorGUILayout.PropertyField (GlassMaterialProperty);
                EditorGUILayout.PropertyField (BrokenGlassMaterialProperty);

                if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                {
                    EditorGUILayout.HelpBox ("Select a game objects to create glass", MessageType.Error);
                }
                else
                {
                    if (GUILayout.Button (new GUIContent ("Add GlassDO", "Add GlassDO and shards for selected objects")))
                    {
                        AddGlass ();
                        EditorUtility.SetDirty (SelectedController);
                    }
                }

                EditorGUILayout.Space (10);

                EditorGUILayout.PropertyField (LightsMaterialProperty);
                EditorGUILayout.PropertyField (BrokenLightsMaterialProperty);
                EditorGUILayout.PropertyField (OnLightsMaterialProperty);

                if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
                {
                    EditorGUILayout.HelpBox ("Select a game objects to create light", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal ();

                    if (GUILayout.Button (new GUIContent ("Add LightObject", "Add LightObject and shards for selected objects")))
                    {
                        AddLight ();
                        EditorUtility.SetDirty (SelectedController);
                    }

                    EditorGUILayout.PropertyField (LightTypeProperty);

                    if (Selection.gameObjects != null && Selection.gameObjects.Length > 0 &&
                        Selection.gameObjects.Any (l => l.GetComponent<LightObject> () != null))
                    {
                        LightObject light;
                        if (GUILayout.Button (new GUIContent ("Add point light", "Add light GameObject (Light source)")))
                        {
                            foreach (var selectedObject in Selection.gameObjects)
                            {
                                light = selectedObject.GetComponent<LightObject> ();
                                if (light != null)
                                {
                                    AddLightSource (light, EditorHelperSettings.LightPintSource);
                                }
                            }
                            EditorUtility.SetDirty (SelectedController);
                        }
                        if (GUILayout.Button (new GUIContent ("Add spot light", "Add light GameObject (Spot light source) for head lights")))
                        {
                            foreach (var selectedObject in Selection.gameObjects)
                            {
                                light = selectedObject.GetComponent<LightObject> ();
                                if (light != null)
                                {
                                    AddLightSource (light, EditorHelperSettings.LightSpotSource);
                                }
                            }
                            EditorUtility.SetDirty (SelectedController);
                        }
                    }

                    EditorGUILayout.EndHorizontal ();
                }
            }
            GUILayout.EndVertical ();
        }

        void AddGlass ()
        {
            foreach (var go in Selection.gameObjects)
            {
                var glassDO = go.GetComponent<GlassDO>();
                if (glassDO != null)
                {
                    Debug.LogErrorFormat ("GlassDO already exist for [{0}]", go.name);
                    continue;
                }

                var renderer = go.GetComponent<MeshRenderer>();

                if (renderer == null)
                {
                    Debug.LogErrorFormat ("[{0}] Not found MeshRenderer", go.name);
                    continue;
                }

                glassDO = go.AddComponent<GlassDO> ();
                SetGlassDOParams (glassDO, renderer);
                EditorHelper.CreateGlassShards (glassDO);
            }
        }

        void AddLight ()
        {
            foreach (var go in Selection.gameObjects)
            {
                var lightObject = go.GetComponent<LightObject>();
                if (lightObject != null)
                {
                    Debug.LogErrorFormat ("LightObject already exist for [{0}]", go.name);
                    continue;
                }

                var renderer = go.GetComponent<MeshRenderer>();
                lightObject = go.AddComponent<LightObject> ();

                if (renderer == null)
                {
                    Debug.LogWarningFormat ("[{0}] Not found MeshRenderer", go.name);
                }

                SetLightObjectParams (lightObject, renderer);
                EditorHelper.CreateLightShards (lightObject);
            }
        }

        void SetGlassDOParams (GlassDO glassDO, MeshRenderer renderer)
        {
            glassDO.BrokenGlassMaterial = BrokenGlassMaterial;
            if (GlassMaterial != null && renderer.sharedMaterials.Length > 1)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == GlassMaterial)
                    {
                        glassDO.GlassMaterialIndex = i;
                        break;
                    }
                }
            }
            glassDO.DestroyClip = EditorHelperSettings.GlassDO.DestroyClip;
            glassDO.Health = EditorHelperSettings.GlassDO.Health;
            glassDO.MaxDamage = EditorHelperSettings.GlassDO.MaxDamage;
        }

        void SetLightObjectParams (LightObject lightObject, MeshRenderer renderer)
        {
            lightObject.BrokenGlassMaterial = BrokenLightsMaterial;
            if (renderer != null && LightsMaterial != null && renderer.sharedMaterials.Length > 1)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == LightsMaterial)
                    {
                        lightObject.GlassMaterialIndex = i;
                        break;
                    }
                }
            }

            lightObject.DestroyClip = EditorHelperSettings.LightObject.DestroyClip;
            lightObject.Health = EditorHelperSettings.LightObject.Health;
            lightObject.MaxDamage = EditorHelperSettings.LightObject.MaxDamage;

            lightObject.OnLightMaterial = OnLightsMaterial;
            lightObject.IsSoftSwitch = EditorHelperSettings.LightObject.IsSoftSwitch;
            lightObject.OnSwitchSpeed = EditorHelperSettings.LightObject.OnSwitchSpeed;
            lightObject.OffSwitchSpeed = EditorHelperSettings.LightObject.OffSwitchSpeed;
            lightObject.Intensity = EditorHelperSettings.LightObject.Intensity;
            lightObject.CarLightType = LightType;
        }

        Light AddLightSource (LightObject lightObject, Light refLight)
        {
            var light = GameObject.Instantiate(refLight, lightObject.transform);
            light.name = lightObject.name + "_LightSource";
            var lightPos = lightObject.transform.position;
            var carPos = SelectedController.transform.position;
            lightPos.y = 0;
            carPos.y = 0;
            var direction = (lightPos - carPos).normalized;
            light.transform.localPosition += direction * 0.2f;
            if (lightObject.CarLightType == CarLightType.TurnLeft || lightObject.CarLightType == CarLightType.TurnRight)
            {
                light.color = EditorHelperSettings.YelowLightColor;
            }
            if (lightObject.CarLightType == CarLightType.Brake || 
                lightObject.CarLightType == CarLightType.Main && lightObject.transform.localPosition.z < 0)
            {
                light.color = EditorHelperSettings.RedLightColor;
            }

            if (lightObject.LightGO)
            {
                GameObject.DestroyImmediate (lightObject.LightGO);
            }

            lightObject.LightGO = light;
            light.SetActive (false);
            return light;
        }

        #endregion //Lights

        #region Wheels

        protected virtual void OnGUIWheels () { }

        protected virtual void GUIWheelButton (string name, string tooltip, Vector2Int wheelDirectionPos, int width = 100, int height = 30)
        {
            if (GUILayout.Button (new GUIContent (string.Format("Wheel {0}", name), tooltip), GUILayout.Width (width * 0.8f), GUILayout.Height (height)))
            {
                var wheel = GetOrCreateWheelObject (name, wheelDirectionPos);
                foreach (var selectionObject in Selection.gameObjects)
                {
                    selectionObject.transform.SetParent (wheel.WheelView);
                    EditorGUIUtility.PingObject (selectionObject);
                }

                var firstChildren = wheel.WheelView.GetChild (0);
                var collider = firstChildren.GetComponent<MeshCollider> ();
                if (collider == null)
                {
                    collider = firstChildren.gameObject.AddComponent<MeshCollider> ();
                    collider.convex = true;
                }
                collider.enabled = false;

                EditorUtility.SetDirty (SelectedController);
            }

            if (!PartsDict.ContainsKey (name) && WheelsTransform != null)
            {
                var wheelTR = WheelsTransform.Find(name);
                if (wheelTR != null && wheelTR.GetComponent<Wheel>() != null)
                {
                    PartsDict[name] = wheelTR;
                }
            }

            if (PartsDict.ContainsKey (name))
            {
                if (GUILayout.Button (new GUIContent ("B", string.Format ("Brake support for [{0}]", string.Format ("Wheel {0}", name))), GUILayout.Width (width * 0.2f), GUILayout.Height (height)))
                {
                    var wheel = GetOrCreateWheelObject (name, wheelDirectionPos);
                    Selection.activeTransform.SetParent (wheel.transform);
                    wheel.BrakeSupport = Selection.activeTransform;
                    EditorUtility.SetDirty (SelectedController);
                }
            }
        }

        Wheel GetOrCreateWheelObject (string name, Vector2Int wheelDirectionPos)
        {
            Transform wheelTR;
            Wheel wheel = null;
            Wheel selectedWheelRef = null;
            if (!PartsDict.TryGetValue (name, out wheelTR))
            {
                wheelTR = WheelsTransform.Find (name);
            }

            if (wheelTR != null)
            {
                wheel = wheelTR.GetComponent<Wheel> ();
            }
            if (wheelTR == null || wheel == null)
            {
                foreach (var refWheel in RefController.Wheels)
                {
                    if (refWheel.transform.localPosition.x * wheelDirectionPos.x >= 0 &&
                        refWheel.transform.localPosition.z * wheelDirectionPos.y >= 0)
                    {
                        selectedWheelRef = refWheel;
                        break;
                    }
                }
                if (selectedWheelRef == null)
                {
                    selectedWheelRef = RefController.Wheels[0];
                    Debug.LogWarningFormat ("Wheel for direction position [{0}] not found", wheelDirectionPos);
                }

                wheel = GameObject.Instantiate (selectedWheelRef, WheelsTransform);
                wheel.name = name;
                wheelTR = wheel.transform;

                SelectedController.Wheels = SelectedController.Wheels.Add (wheel);

                List<GameObject> objectsForDelete = new List<GameObject>();

                for (int i = 0; i < wheelTR.childCount; i++)
                {
                    objectsForDelete.Add (wheelTR.GetChild(i).gameObject);
                }

                foreach (var objForDel in objectsForDelete)
                {
                    GameObject.DestroyImmediate (objForDel);
                }
                wheelTR.position = Selection.activeTransform.position;
                var wheelView = new GameObject("View").transform;
                wheelView.SetParent (wheelTR);
                wheelView.localPosition = Vector3.zero;
                wheelView.localRotation = Quaternion.identity;
                wheel.WheelView = wheelView;
                wheel.gameObject.SetLayerRecursively (EditorHelperSettings.WheelsLayer);

                PartsDict[name] = wheelTR;
            }

            return wheel;
        }

        protected void CheckWheelsObject ()
        {
            if (WheelsTransform == null || WheelsTransform.IsChildOf (SelectedController.transform))
            {
                WheelsTransform = SelectedController.transform.Find ("Wheels");
                if (WheelsTransform == null)
                {
                    WheelsTransform = new GameObject ("Wheels").transform;
                    WheelsTransform.SetParent (SelectedController.transform);
                    WheelsTransform.localPosition = Vector3.zero;
                    WheelsTransform.localRotation = Quaternion.identity;
                }
            }
        }

        #endregion //Wheels

        #region Other

        protected virtual void OnGUIOther ()
        {
            EditorGUILayout.HelpBox ("Don't forget to move the COM to the correct location.", MessageType.Info);
            if (GUILayout.Button ("Select COM"))
            {
                if (SelectedController.COM == null)
                {
                    SelectedController.COM = new GameObject ("COM").transform;
                    SelectedController.COM.SetParent (SelectedController.transform);
                    SelectedController.COM.localPosition = Vector3.zero;
                    EditorUtility.SetDirty (SelectedController);
                }
                Selection.activeObject = SelectedController.COM;
                EditorGUIUtility.PingObject (Selection.activeObject);
            }

            if (GUILayout.Button ("Find the biggest Mesh and use it as a base view"))
            {
                var renderers = SelectedController.transform.GetComponentsInChildren<MeshRenderer>();
                var biggestRenderer = renderers[0];
                float maxSize = 0;
                foreach (var renderer in renderers)
                {
                    if (renderer.bounds.size.sqrMagnitude > maxSize)
                    {
                        biggestRenderer = renderer;
                        maxSize = renderer.bounds.size.sqrMagnitude;
                    }
                }
                if (SelectedController.BaseViews == null || !SelectedController.BaseViews.Contains (biggestRenderer))
                {
                    SelectedController.BaseViews = SelectedController.BaseViews.Add (biggestRenderer);
                    EditorUtility.SetDirty (SelectedController);
                }
            }
        }

        #endregion //Other

        #region CreateVehicle

        protected virtual bool AddVehicleComponents ()
        {
            if (SelectedController == null)
            {
                Debug.LogError ("Has no selected VehicleComponent");
                return false;
            }

            SelectedController.VehicleName = SelectedController.gameObject.name;
            SelectedController.gameObject.AddComponent<CarLighting> ();
            
            VehicleDamageController damageCtrl = null;
            if (RefController)
            {
                var refDamageCtrl = RefController.GetComponent<VehicleDamageController>();
                if (refDamageCtrl)
                {
                    damageCtrl = SelectedController.gameObject.AddComponent<VehicleDamageController> ();
                    damageCtrl.CopySettings (refDamageCtrl);
                }

                var selectedRB = SelectedController.GetComponent<Rigidbody>();
                var refRB = RefController.GetComponent<Rigidbody>();
                selectedRB.mass = refRB.mass;
                selectedRB.drag = refRB.drag;
                selectedRB.angularDrag = refRB.angularDrag;
                selectedRB.interpolation = refRB.interpolation;
            }

            ViewTransform = SelectedController.transform.Find ("View");
            if (ViewTransform == null)
            {
                ViewTransform = new GameObject ("View").transform;
                ViewTransform.SetParent (SelectedController.transform);
                ViewTransform.localPosition = Vector3.zero;
                ViewTransform.localRotation = Quaternion.identity;
            }

            var com = SelectedController.transform.Find ("COM");
            if (com == null)
            {
                com = new GameObject ("COM").transform;
                com.SetParent (SelectedController.transform);
                com.localPosition = Vector3.zero;
                com.localRotation = Quaternion.identity;

                if (RefController)
                {
                    com.localPosition = RefController.COM.localPosition;
                }
            }

            CheckWheelsObject ();

            if (damageCtrl != null)
            {
                damageCtrl.IgnoreFindInChildsMeshesAndColliders.Add (WheelsTransform);
            }

            SelectedController.COM = com;

            return true;
        }

        #endregion //CreateCar
    }
}
