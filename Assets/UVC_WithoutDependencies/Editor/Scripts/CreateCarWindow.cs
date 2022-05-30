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
    public class CreateCarWindow :CreateVehicleWindow
    {
        [Tooltip("Car from which to copy parameters (Engine, steering, gearbox)")]
        public CarController RefCarController;
        [Tooltip("Editable car)")]
        public CarController SelectedCarController;
        public override VehicleController RefController => RefCarController;
        public override VehicleController SelectedController => SelectedCarController;

        SerializedProperty RefCarControllerProperty;
        SerializedProperty SelectedCarControllerProperty;

        protected override void OnEnable ()
        {
            Target = this;
            SerializedObject = new SerializedObject (Target);

            base.OnEnable ();

            RefCarControllerProperty = SerializedObject.FindProperty ("RefCarController");
            SelectedCarControllerProperty = SerializedObject.FindProperty ("SelectedCarController");
        }

        protected override void OnGUI ()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox ("It is not possible to create a vehicle in the playmode.", MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView (scrollPos, false, false, GUILayout.Width (this.position.width), GUILayout.Height (this.position.height));

            EditorGUILayout.PropertyField (RefCarControllerProperty);
            EditorGUILayout.PropertyField (SelectedCarControllerProperty);

            if (SelectedCarController == null)
            {
                if (RefController == null)
                {
                    EditorGUILayout.HelpBox ("Select RefController to copy car configuration", MessageType.Error);
                }
                else if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.HelpBox ("Select a game object to create a vehicle", MessageType.Error);
                }
                else
                {
                    if (GUILayout.Button ("Create Car"))
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

        protected override void OnGUIParts ()
        {
            EditorGUILayout.Space (10);
            GUILayout.BeginVertical (GUI.skin.textArea, GUILayout.MaxWidth (402));
            PartsFoldout = EditorGUILayout.Foldout (PartsFoldout, "-Body parts-");
            if (PartsFoldout)
            {
                base.OnGUIParts ();

                EditorGUILayout.Space (10);
                EditorGUILayout.LabelField ("Car parts:");

                EditorGUILayout.HelpBox ("After adding the part, it may be necessary to adjust the damage check points and joints.", MessageType.Info);

                GUIPartButton (EditorHelperSettings.BumperFront, 362, 50);

                EditorGUILayout.BeginHorizontal ();
                {
                    GUIPartButton (EditorHelperSettings.WingFrontLeft, 100, 50);
                    GUIPartButton (EditorHelperSettings.Hood, 100, 50);
                    GUIPartButton (EditorHelperSettings.WingFrontRight, 100, 50);
                }
                EditorGUILayout.EndHorizontal ();

                EditorGUILayout.BeginHorizontal ();
                {
                    EditorGUILayout.BeginVertical ();
                    {
                        GUIPartButton (EditorHelperSettings.DoorFrontLeft, 100, 50);
                        GUIPartButton (EditorHelperSettings.DoorRearLeft, 100, 50);
                    }
                    EditorGUILayout.EndVertical ();

                    GUIPartButton (EditorHelperSettings.Body, 100, 102);

                    EditorGUILayout.BeginVertical ();
                    {
                        GUIPartButton (EditorHelperSettings.DoorFrontRight, 100, 50);
                        GUIPartButton (EditorHelperSettings.DoorRearRight, 100, 50);
                    }
                    EditorGUILayout.EndVertical ();
                }
                EditorGUILayout.EndHorizontal ();

                GUIPartButton (EditorHelperSettings.Trunk, 362, 50);
                GUIPartButton (EditorHelperSettings.BumperRear, 362, 50);
            }
            
            GUILayout.EndVertical ();
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
                if (RefCarController == null)
                {
                    EditorGUILayout.HelpBox ("[Ref Controller] car is Null, Wheel settings are taken from the RefController car.", MessageType.Error);
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
                EditorGUILayout.HelpBox ("Wheel settings are taken from the RefController car", MessageType.Info);
                GUILayout.BeginHorizontal ();
                {
                    GUIWheelButton ("FL", "Wheel front left", new Vector2Int (-1, 1));
                    GUILayout.Space (20);
                    GUIWheelButton ("FR", "Wheel front right", new Vector2Int (1, 1));
                }
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                {
                    GUIWheelButton ("RL", "Wheel rear left", new Vector2Int (-1, -1));
                    GUILayout.Space (20);
                    GUIWheelButton ("RR", "Wheel rear right", new Vector2Int (1, -1));
                }
                GUILayout.EndHorizontal ();
            }
            GUILayout.EndVertical ();
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

                if (GUILayout.Button ("Select SteerWheel"))
                {
                    if (Selection.activeTransform && Selection.activeTransform != SelectedCarController.transform &&
                    Selection.activeTransform.IsChildOf (SelectedCarController.transform))
                    {
                        SelectedCarController.SteerWheel = Selection.activeTransform;
                        var damageCtrl = SelectedCarController.GetComponent<VehicleDamageController>();
                        var steerWheelMeshFilter = SelectedCarController.SteerWheel.GetComponent<MeshFilter>();
                        if (damageCtrl != null && steerWheelMeshFilter != null &&
                            !damageCtrl.IgnoreDeformMeshes.Contains (steerWheelMeshFilter))
                        {
                            damageCtrl.IgnoreDeformMeshes.Add (steerWheelMeshFilter);
                        }
                        var moveableDO = SelectedCarController.SteerWheel.gameObject.GetComponent<MoveableDO> ();
                        if (moveableDO == null)
                        {
                            moveableDO = SelectedCarController.SteerWheel.gameObject.AddComponent<MoveableDO> ();
                            moveableDO.Health = float.PositiveInfinity;
                        }
                        EditorUtility.SetDirty (SelectedCarController);
                    }
                    else
                    {
                        Debug.LogError ("The active object is null, or the active object is not a child of the SelectedController.");
                    }
                }

                if (GUILayout.Button (new GUIContent ("Select engine DamageableObject", "Transform in which engine damage passes")))
                {
                    DamageableObject damageableObject;
                    if (SelectedCarController.EngineDamageableObject == null)
                    {
                        damageableObject = new GameObject ("EngineDamageableObject").AddComponent<DamageableObject> ();
                        damageableObject.Health = 100;
                        damageableObject.MaxDamage = float.PositiveInfinity;
                        damageableObject.transform.SetParent (SelectedController.transform);
                        damageableObject.transform.localPosition = Vector3.zero;
                        SelectedCarController.EngineDamageableObject = damageableObject;
                        EditorUtility.SetDirty (SelectedController);
                    }
                    else
                    {
                        damageableObject = SelectedCarController.EngineDamageableObject;
                    }
                    Selection.activeObject = damageableObject;
                }

                var carVFX = SelectedCarController.GetComponentInChildren<CarVFX>();
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

        #region CreateCar

        protected override bool AddVehicleComponents ()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError ("Has no selected gameobject");
                return false;
            }

            SelectedCarController = Selection.activeGameObject.GetComponent<CarController> ();
            if (SelectedCarController == null)
            {
                SelectedCarController = Selection.activeGameObject.AddComponent<CarController> ();
            }

            base.AddVehicleComponents ();
            
            if (RefCarController)
            {
                SelectedCarController.SteerWheelMaxAngle = RefCarController.SteerWheelMaxAngle;
                SelectedCarController.Engine = DeepClone.GetClone (RefCarController.Engine);
                SelectedCarController.Steer = DeepClone.GetClone (RefCarController.Steer);
                SelectedCarController.Gearbox = DeepClone.GetClone (RefCarController.Gearbox);
            }

            PrefabUtility.InstantiatePrefab (EditorHelperSettings.CarSFXPrefab, SelectedController.transform);
            PrefabUtility.InstantiatePrefab (EditorHelperSettings.CarVFXPrefab, SelectedController.transform);
            SelectedController.gameObject.SetLayerRecursively (EditorHelperSettings.VehicleLayer);

            SelectedCarControllerProperty.objectReferenceValue = SelectedCarController;
            SerializedObject.ApplyModifiedProperties ();

            return true;
        }

        #endregion //CreateCar

        #region CreateWindow
        public static CreateCarWindow Window;

        [MenuItem ("Window/Perfect Games/Create Car Window")]
        public static void ShowWindow ()
        {
            if (Window != null)
            {
                Window.Close ();
            }
            Window = GetWindow<CreateCarWindow> ("Create Car Window");
        }
        #endregion //CreateWindow
    }
}
