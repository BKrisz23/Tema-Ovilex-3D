using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using PG.GameBalance;

namespace PG
{
    /// <summary>
    /// A window for creating a car.
    /// You can see how to prepare a car model in the documentation.
    /// </summary>
    public class CreateBikeWindow :CreateVehicleWindow
    {
        [Tooltip("Bike from which to copy parameters (Bike, Engine, steering, gearbox)")]
        public BikeController RefBikeController;
        [Tooltip("Editable bike)")]
        public BikeController SelectedBikeController;
        public override VehicleController RefController => RefBikeController;
        public override VehicleController SelectedController => SelectedBikeController;

        SerializedProperty RefBikeControllerProperty;
        SerializedProperty SelectedBikeControllerProperty;
        SerializedProperty UseCustomAngleHandlebarProperty;
        SerializedProperty CustomAngleProperty;

        protected override void OnEnable ()
        {
            Target = this;
            SerializedObject = new SerializedObject (Target);

            base.OnEnable ();

            RefBikeControllerProperty = SerializedObject.FindProperty ("RefBikeController");
            SelectedBikeControllerProperty = SerializedObject.FindProperty ("SelectedBikeController");
            UseCustomAngleHandlebarProperty = SerializedObject.FindProperty ("UseCustomAngleHandlebar");
            CustomAngleProperty = SerializedObject.FindProperty ("CustomAngle");
        }

        protected override void OnGUI ()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox ("It is not possible to create a vehicle in the playmode.", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView (scrollPos, false, false, GUILayout.Width (this.position.width), GUILayout.Height (this.position.height));

            EditorGUILayout.PropertyField (RefBikeControllerProperty);
            EditorGUILayout.PropertyField (SelectedBikeControllerProperty);

            if (SelectedBikeController == null)
            {
                if (RefController == null)
                {
                    EditorGUILayout.HelpBox ("Select RefController to copy bike configuration", MessageType.Error);
                }
                else if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.HelpBox ("Select a game object to create a vehicle", MessageType.Error);
                }
                else
                {
                    if (GUILayout.Button ("Create Bike"))
                    {
                        AddVehicleComponents ();
                    }
                }
            }

            base.OnGUI ();

            EditorGUILayout.EndScrollView ();

            SerializedObject.ApplyModifiedProperties ();
        }

        #region Parts

        public bool UseCustomAngleHandlebar;
        public float CustomAngle;

        protected override void OnGUIParts ()
        {
            EditorGUILayout.Space (10);
            GUILayout.BeginVertical (GUI.skin.textArea, GUILayout.MaxWidth (402));
            PartsFoldout = EditorGUILayout.Foldout (PartsFoldout, "-Bike Body parts-");
            if (PartsFoldout)
            {
                base.OnGUIParts ();

                EditorGUILayout.Space (10);
                EditorGUILayout.LabelField ("Bike parts:");

                EditorGUILayout.HelpBox ("After adding the part, it may be necessary to adjust the damage check points and joints.", MessageType.Info);

                EditorGUILayout.BeginHorizontal ();
                {
                    GUIPartButton (EditorHelperSettings.Bike_ForkFront, 100, 50, ForkFrontClickAction);
                    GUIPartButton (EditorHelperSettings.Bike_Handlebar, 100, 50, HandlebarClickAction);
                    GUIPartButton (EditorHelperSettings.Bike_ForkRear, 100, 50, ForkRearClickAction);
                }
                EditorGUILayout.EndHorizontal ();

                GUIPartButton (EditorHelperSettings.Bike_Body, 362, 50);

                EditorGUILayout.Space (10);
                EditorGUILayout.LabelField ("Handlebar settings:");

                EditorGUILayout.PropertyField (UseCustomAngleHandlebarProperty);
                if (UseCustomAngleHandlebar)
                {
                    EditorGUILayout.PropertyField (CustomAngleProperty);
                }
                if (SelectedBikeController.Handlebar)
                {
                    if (!UseCustomAngleHandlebar && Selection.activeGameObject == null)
                    {
                        EditorGUILayout.HelpBox ("No selected object", MessageType.Error);
                    }
                    else if (GUILayout.Button (UseCustomAngleHandlebar ? "Set custom Handlebar angle" : "Set Handlebar angle from selected object"))
                    {
                        SetHandlebarAngleClickAction ();
                        EditorUtility.SetDirty (SelectedBikeController);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox ("The Handlebar has not been created yet, please select the Handlebar object and click the [Handlebar] button in the [Body parts] section.", MessageType.Info);
                }
            }
            
            GUILayout.EndVertical ();
        }

        void ForkFrontClickAction ()
        {
            Transform forkFrontTR;
            PartsDict.TryGetValue (EditorHelperSettings.Bike_ForkFront.PartName, out forkFrontTR);
            if (forkFrontTR)
            {
                SelectedBikeController.FrontFork = forkFrontTR;
                if (SelectedBikeController.Wheels != null && SelectedBikeController.Wheels.Length > 0 && SelectedBikeController.FrontWheel != null && 
                    SelectedBikeController.FrontWheel.WheelView != null)
                {
                    SelectedBikeController.FrontWheel.WheelView.SetParent (forkFrontTR);
                }

                Transform HandlebarTR;
                if (PartsDict.TryGetValue (EditorHelperSettings.Bike_Handlebar.PartName, out HandlebarTR) && forkFrontTR != null)
                {
                    forkFrontTR.SetParent (HandlebarTR);
                }
            }
            EditorUtility.SetDirty (SelectedBikeController);
        }

        void HandlebarClickAction ()
        {
            Transform handlebarTR;
            PartsDict.TryGetValue (EditorHelperSettings.Bike_Handlebar.PartName, out handlebarTR);
            if (handlebarTR)
            {
                SelectedBikeController.Handlebar = handlebarTR;

                Transform forkFrontTR;
                if (PartsDict.TryGetValue (EditorHelperSettings.Bike_ForkFront.PartName, out forkFrontTR))
                {
                    forkFrontTR.SetParent (handlebarTR);
                }
            }
            EditorUtility.SetDirty (SelectedBikeController);
        }

        void ForkRearClickAction ()
        {
            Transform forkRearTR;
            PartsDict.TryGetValue (EditorHelperSettings.Bike_ForkRear.PartName, out forkRearTR);
            if (forkRearTR)
            {
                if (SelectedBikeController.RearForkParent == null)
                {
                    SelectedBikeController.RearForkParent = new GameObject (forkRearTR.name + "_Parent").transform;
                    SelectedBikeController.RearForkParent.SetParent (forkRearTR.parent);
                    SelectedBikeController.RearForkParent.localPosition = forkRearTR.localPosition;
                    SelectedBikeController.RearForkParent.localRotation = Quaternion.identity;
                    forkRearTR.SetParent (SelectedBikeController.RearForkParent);
                }

                SelectedBikeController.RearFork = forkRearTR;
                if (SelectedBikeController.Wheels != null && SelectedBikeController.Wheels.Length > 1 && SelectedBikeController.FrontWheel != null &&
                    SelectedBikeController.RearWheel.WheelView != null)
                {
                    SelectedBikeController.RearWheel.WheelView.SetParent (forkRearTR);
                }
            }
            EditorUtility.SetDirty (SelectedBikeController);
        }

        void SetHandlebarAngleClickAction ()
        {
            var handlebarTR = SelectedBikeController.Handlebar;
            var handlebarChilds = new Transform[handlebarTR.childCount];
            for (int i = 0; i < handlebarChilds.Length; i++)
            {
                handlebarChilds[i] = handlebarTR.GetChild (i);
            }
            for (int i = 0; i < handlebarChilds.Length; i++)
            {
                handlebarChilds[i].SetParent (SelectedBikeController.transform);
            }

            var localRotation = UseCustomAngleHandlebar? Quaternion.AngleAxis (CustomAngle, Vector3.right): Selection.activeTransform.localRotation;
            handlebarTR.localRotation = localRotation;

            for (int i = 0; i < handlebarChilds.Length; i++)
            {
                handlebarChilds[i].SetParent (handlebarTR);
            }
        }

        #endregion //Parts

        #region Wheels

        protected override void OnGUIWheels ()
        {
            base.OnGUIWheels ();

            EditorGUILayout.Space (10);
            GUILayout.BeginVertical (GUI.skin.textArea, GUILayout.MaxWidth (402));
            WheelsFoldout = EditorGUILayout.Foldout (WheelsFoldout, "-Wheels-");
            if (WheelsFoldout)
            {
                if (SelectedBikeController.Wheels == null || SelectedBikeController.Wheels.Length != 2)
                {
                    SelectedBikeController.Wheels = new Wheel[2];
                }
                if (RefBikeController == null)
                {
                    EditorGUILayout.HelpBox ("[Ref Bike Controller] bike is Null, Wheel settings are taken from the RefBikeController.", MessageType.Error);
                    GUILayout.EndVertical ();
                    return;
                }
                if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.HelpBox ("No selected object", MessageType.Error);
                    GUILayout.EndVertical ();
                    return;
                }
                CheckWheelsObject ();
                EditorGUILayout.HelpBox ("Wheel settings are taken from the RefBikeController", MessageType.Info);

                GUIBikeWheelButton ("FrontWheel", "Front wheel", 1);

                GUIBikeWheelButton ("RearWheel", "Rear wheel", -1);

            }
            GUILayout.EndVertical ();
        }

        void GUIBikeWheelButton (string name, string tooltip, int wheelDirection, int width = 100, int height = 30)
        {
            if (GUILayout.Button (new GUIContent (name, tooltip), GUILayout.Width (width * 0.8f), GUILayout.Height (height)))
            {
                var wheel = GetOrCreateBikeWheelObject (name, wheelDirection);
                foreach (var selectionObject in Selection.gameObjects)
                {
                    selectionObject.layer = EditorHelperSettings.WheelsLayer;
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
        }

        BikeWheel GetOrCreateBikeWheelObject (string name, int wheelDirection)
        {
            BikeWheel wheel = null;
            BikeWheel selectedWheelRef = null;
            if (wheelDirection == 1)
            {
                if (SelectedBikeController.Wheels != null && SelectedBikeController.Wheels.Length > 0)
                {
                    wheel = SelectedBikeController.FrontWheel as BikeWheel;
                }
                if (RefBikeController.Wheels != null && RefBikeController.Wheels.Length > 0)
                {
                    selectedWheelRef = RefBikeController.FrontWheel as BikeWheel;
                }
            }
            if (wheelDirection == -1)
            {
                if (SelectedBikeController.Wheels != null && SelectedBikeController.Wheels.Length > 1)
                {
                    wheel = SelectedBikeController.RearWheel as BikeWheel;
                }
                if (RefBikeController.Wheels != null && RefBikeController.Wheels.Length > 1)
                {
                    selectedWheelRef = RefBikeController.RearWheel as BikeWheel;
                }
            }

            if (selectedWheelRef == null)
            {
                Debug.LogErrorFormat ("Wheel for direction [{0}] not found", wheelDirection);
                selectedWheelRef = new BikeWheel ();
            }

            if (wheel == null)
            {
                wheel = GameObject.Instantiate (selectedWheelRef, WheelsTransform);
                wheel.name = name;
                wheel.transform.SetParent (WheelsTransform);
                wheel.transform.position = Selection.activeTransform.position;
                wheel.WheelView = null;
            }

            if (wheelDirection == 1 && SelectedBikeController.FrontWheel != wheel)
            {
                SelectedBikeController.Wheels[0] = wheel;
            }
            if (wheelDirection == -1 && SelectedBikeController.RearWheel != wheel)
            {
                SelectedBikeController.Wheels[1] = wheel;
            }

            var wheelTR = wheel.transform;

            if (wheel.WheelView == null)
            {
                wheel.WheelView = new GameObject (wheelDirection == 1 ? "Wheel_Front" : "Wheel_Rear").transform;
                wheel.WheelView.position = Selection.activeTransform.position;
                var damageController = SelectedBikeController.GetComponent<VehicleDamageController>();
                damageController.IgnoreFindInChildsMeshesAndColliders.Add (wheel.WheelView);
            }

            if (wheelDirection == 1)
            {
                if (SelectedBikeController.FrontFork)
                {
                    wheel.WheelView.SetParent (SelectedBikeController.FrontFork);
                }
                else
                {
                    wheel.WheelView.SetParent (wheelTR);
                }
            }
            else
            {
                if (SelectedBikeController.RearFork)
                {
                    wheel.WheelView.SetParent (SelectedBikeController.RearFork);
                }
                else
                {
                    wheel.WheelView.SetParent (wheelTR);
                }
            }

            wheel.gameObject.SetLayerRecursively (EditorHelperSettings.WheelsLayer);
            wheel.WheelView.gameObject.SetLayerRecursively (EditorHelperSettings.WheelsLayer);

            return wheel;
        }

        #endregion //Wheels

        #region Other

        protected override void OnGUIOther ()
        {
            EditorGUILayout.Space (10);
            GUILayout.BeginVertical (GUI.skin.textArea, GUILayout.MaxWidth (402));
            OtherFoldout = EditorGUILayout.Foldout (OtherFoldout, "-Other settings-");

            if (OtherFoldout)
            {
                base.OnGUIOther ();

                if (GUILayout.Button ("Select front COM"))
                {
                    if (SelectedBikeController.FrontComPosition == null)
                    {
                        SelectedBikeController.FrontComPosition = new GameObject ("FrontCOM").transform;
                        SelectedBikeController.FrontComPosition.SetParent (SelectedBikeController.transform);
                        SelectedBikeController.FrontComPosition.localPosition = SelectedBikeController.COM == null? Vector3.zero: SelectedBikeController.COM.localPosition;
                        EditorUtility.SetDirty (SelectedBikeController);
                    }
                    Selection.activeObject = SelectedBikeController.FrontComPosition;
                    EditorGUIUtility.PingObject (Selection.activeObject);
                }

                if (GUILayout.Button ("Select rear COM"))
                {
                    if (SelectedBikeController.RearComPosition == null)
                    {
                        SelectedBikeController.RearComPosition = new GameObject ("RearCOM").transform;
                        SelectedBikeController.RearComPosition.SetParent (SelectedBikeController.transform);
                        SelectedBikeController.RearComPosition.localPosition = SelectedBikeController.COM == null ? Vector3.zero : SelectedBikeController.COM.localPosition;
                        EditorUtility.SetDirty (SelectedBikeController);
                    }
                    Selection.activeObject = SelectedBikeController.RearComPosition;
                    EditorGUIUtility.PingObject (Selection.activeObject);
                }

                if (GUILayout.Button (new GUIContent ("Select engine DamageableObject", "Transform in which engine damage passes")))
                {
                    DamageableObject damageableObject;
                    if (SelectedBikeController.EngineDamageableObject == null)
                    {
                        damageableObject = new GameObject ("EngineDamageableObject").AddComponent<DamageableObject> ();
                        damageableObject.Health = 100;
                        damageableObject.MaxDamage = float.PositiveInfinity;
                        damageableObject.transform.SetParent (SelectedController.transform);
                        damageableObject.transform.localPosition = Vector3.zero;
                        SelectedBikeController.EngineDamageableObject = damageableObject;
                        EditorUtility.SetDirty (SelectedController);
                    }
                    else
                    {
                        damageableObject = SelectedBikeController.EngineDamageableObject;
                    }
                    Selection.activeObject = damageableObject;
                }

                var carVFX = SelectedBikeController.GetComponentInChildren<CarVFX>();
                if (carVFX)
                {
                    if (GUILayout.Button ("Select engine damage particles"))
                    {
                        Selection.objects = new Object[0];
                        if (carVFX.EngineHealth25Particles)
                        {
                            Selection.objects = Selection.gameObjects.Add (carVFX.EngineHealth25Particles.gameObject);
                        }
                        if (carVFX.EngineHealth50Particles)
                        {
                            Selection.objects = Selection.gameObjects.Add (carVFX.EngineHealth50Particles.gameObject);
                        }
                        if (carVFX.EngineHealth75Particles)
                        {
                            Selection.objects = Selection.gameObjects.Add (carVFX.EngineHealth75Particles.gameObject);
                        }
                    }
                    int particlesCount = Mathf.Max(
                        carVFX.ExhaustParticles.Count,
                        carVFX.BackFireParticles.Count,
                        carVFX.BoostParticles.Count);
                    for (int i = 0; i < particlesCount; i++)
                    {
                        GUIExhaustParticles (carVFX, i);
                    }
                }
            }
            GUILayout.EndVertical ();
        }

        void GUIExhaustParticles (CarVFX carVFX, int index)
        {
            GUILayout.BeginHorizontal ();

            if (GUILayout.Button (string.Format("Select all exhaust effects {0}", index + 1)))
            {
                Selection.objects = new Object[0];
                if (carVFX.ExhaustParticles != null && carVFX.ExhaustParticles.Count >= index + 1)
                {
                    Selection.objects = Selection.gameObjects.Add (carVFX.ExhaustParticles[index].gameObject);
                }
                if (carVFX.BackFireParticles != null && carVFX.BackFireParticles.Count >= index + 1)
                {
                    Selection.objects = Selection.gameObjects.Add (carVFX.BackFireParticles[index].gameObject);
                }
                if (carVFX.BoostParticles != null && carVFX.BoostParticles.Count >= index + 1)
                {
                    Selection.objects = Selection.gameObjects.Add (carVFX.BoostParticles[index].gameObject);
                }
            }

            if (GUILayout.Button (string.Format ("Mirror exhaust effects {0}", index + 1)))
            {
                ParticleSystem origin;
                ParticleSystem newParticles;
                Selection.objects = new Object[0];
                if (carVFX.ExhaustParticles != null && carVFX.ExhaustParticles.Count >= index + 1)
                {
                    origin = carVFX.ExhaustParticles[index];
                    newParticles = GameObject.Instantiate (origin, origin.transform.parent);
                    newParticles.transform.SetLocalX(-newParticles.transform.localPosition.x);
                    carVFX.ExhaustParticles.Add (newParticles);
                    Selection.objects = Selection.gameObjects.Add (newParticles.gameObject);
                }
                if (carVFX.BackFireParticles != null && carVFX.BackFireParticles.Count >= index + 1)
                {
                    origin = carVFX.BackFireParticles[index];
                    newParticles = GameObject.Instantiate (origin, origin.transform.parent);
                    newParticles.transform.SetLocalX (-newParticles.transform.localPosition.x);
                    carVFX.BackFireParticles.Add (newParticles);
                    Selection.objects = Selection.gameObjects.Add (newParticles.gameObject);
                }
                if (carVFX.BoostParticles != null && carVFX.BoostParticles.Count >= index + 1)
                {
                    origin = carVFX.BoostParticles[index];
                    newParticles = GameObject.Instantiate (origin, origin.transform.parent);
                    newParticles.transform.SetLocalX (-newParticles.transform.localPosition.x);
                    carVFX.BoostParticles.Add (newParticles);
                    Selection.objects = Selection.gameObjects.Add (newParticles.gameObject);
                }

                EditorUtility.SetDirty (carVFX);
            }

            GUILayout.EndHorizontal ();
        }

        #endregion //Other

        #region CreateBike

        protected override bool AddVehicleComponents ()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError ("Has no selected gameobject");
                return false;
            }

            SelectedBikeController = Selection.activeGameObject.GetComponent<BikeController> ();
            if (SelectedBikeController == null)
            {
                SelectedBikeController = Selection.activeGameObject.AddComponent<BikeController> ();
            }

            base.AddVehicleComponents ();
            
            if (RefBikeController)
            {
                SelectedBikeController.Bike = DeepClone.GetClone (RefBikeController.Bike);
                SelectedBikeController.Engine = DeepClone.GetClone (RefBikeController.Engine);
                SelectedBikeController.Steer = DeepClone.GetClone (RefBikeController.Steer);
                SelectedBikeController.Gearbox = DeepClone.GetClone (RefBikeController.Gearbox);
            }

            PrefabUtility.InstantiatePrefab (EditorHelperSettings.BikeSFXPrefab, SelectedController.transform);
            PrefabUtility.InstantiatePrefab (EditorHelperSettings.BikeVFXPrefab, SelectedController.transform);
            SelectedController.gameObject.SetLayerRecursively (EditorHelperSettings.VehicleLayer);
            
            if (SelectedBikeController.FrontComPosition == null)
            {
                var frontCOM = new GameObject ("FrontCOM").transform;
                frontCOM.SetParent (SelectedBikeController.transform);
                frontCOM.localPosition = Vector3.zero;
                frontCOM.localRotation = Quaternion.identity;

                if (RefBikeController)
                {
                    frontCOM.localPosition = RefBikeController.FrontComPosition.localPosition;
                }

                SelectedBikeController.FrontComPosition = frontCOM;
            }

            if (SelectedBikeController.RearComPosition == null)
            {
                var rearCOM = new GameObject ("RearCOM").transform;
                rearCOM.SetParent (SelectedBikeController.transform);
                rearCOM.localPosition = Vector3.zero;
                rearCOM.localRotation = Quaternion.identity;

                if (RefBikeController)
                {
                    rearCOM.localPosition = RefBikeController.RearComPosition.localPosition;
                }

                SelectedBikeController.RearComPosition = rearCOM;
            }

            SelectedBikeControllerProperty.objectReferenceValue = SelectedBikeController;
            SerializedObject.ApplyModifiedProperties ();

            return true;
        }

        #endregion //CreateCar

        #region CreateWindow
        public static CreateBikeWindow Window;

        [MenuItem ("Window/Perfect Games/Create Bike Window")]
        public static void ShowWindow ()
        {
            if (Window != null)
            {
                Window.Close ();
            }
            Window = GetWindow<CreateBikeWindow> ("Create Bike Window");
        }
        #endregion //CreateWindow
    }
}
