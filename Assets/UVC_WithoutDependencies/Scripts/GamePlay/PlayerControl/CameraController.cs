using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PG
{
    /// <summary>
    /// Move and rotation camera controller
    /// </summary>

    public class CameraController :InitializePlayer
    {
#pragma warning disable 0649

        [SerializeField] List<CameraPreset> CameraPresets = new List<CameraPreset>();
        [SerializeField] Transform HorizontalRotation;
        [SerializeField] Transform VerticalRotation;
        [SerializeField] Transform CameraViewTransform;                                 //Camera transform for change view.
        [SerializeField] Transform CameraShakeTransform;                                //Trasform for camera shake.
        [SerializeField] Camera MainCamera;

        [SerializeField] float ChangeCameraSpeed = 5;                                   //Camera switching speed for smooth switching.
        [SerializeField] float DistanceAfterMouseMove = 5;                              //If there was a manual rotation of the camera, after passing this distance by the car, the camera starts to rotate automatically.
        [SerializeField] LayerMask ObstacleMask;
        [SerializeField] float DistanceToObstacle = 0.1f;
        [Header ("Wind Sound")]
        [SerializeField] AudioSource SpeedWindSource;
        [SerializeField] float WindSoundStartSpeed = 20;
        [SerializeField] float WindSoundMaxSpeed = 100;
        [SerializeField] float WindSoundStartPitch = 0.6f;
        [SerializeField] float WindSoundMaxPitch = 1.5f;

        [Header ("SplitScreen settings")]
        [SerializeField] Vector2 CameraRectSize = new Vector2 (1, 0.5f);
        [SerializeField] Vector2 CameraRectPosP1 = new Vector2 (0, 0.5f);
        [SerializeField] Vector2 CameraRectPosP2 = new Vector2 (0, 0f);

#pragma warning restore 0649

        int ActivePresetIndex = -1;
        float SqrMinDistance;
        int CurrentFrame = 0;
        float CurrentDistanceAfterMouseMove;
        float SetCameraSpeed;
        Quaternion TargetHorizontalRotation;
        float CurrentManualRotationAngle;
        float TargetManualRotationAngle;
        float TargetVerticalRotation;
        Coroutine SoftMoveCameraCoroutine;
        UserInput UserControl;
        float CarSpeedDelta;
        float PrevCarSpeed;
        int PlayerIndex = 0;

        //The target point is calculated from velocity of car.
        Vector3 _TargetPoint;
        Vector3 TargetPoint
        {
            get
            {
                if (CurrentFrame != Time.frameCount) //Condition for ignoring the calculation in one frame several times.
                {
                    if (Vehicle == null || Vehicle.RB == null)
                    {
                        return transform.position;
                    }

                    if (ActivePreset.EnableVelocityOffset)
                    {
                        _TargetPoint = Vehicle.RB.velocity * ActivePreset.VelocityMultiplier;
                        _TargetPoint += Vehicle.transform.TransformPoint (Vehicle.Bounds.center);
                    }
                    else
                    {
                        _TargetPoint = Vehicle.transform.TransformPoint (Vehicle.Bounds.center);
                    }

                    CurrentFrame = Time.frameCount;
                }
                return _TargetPoint;
            }
        }
        public CameraPreset ActivePreset { get; private set; }

        Vector3 PrevTargetPoint;    //PrevTargetPoint The variable is needed for SuperSmoothLerp.
        Vector3 LocalCameraPos;
        Vector3 TargetShakeCameraPos;
        float RayDistance;
        RaycastHit RayHit;
        bool ManualRotation;

        public override bool Initialize (VehicleController vehicle)
        {
            if (Car != null)
            {
                Car.OnConnectTrailer -= SoftMoveCamera;
            }
            if (base.Initialize (vehicle) && Car)
            {
                Car.OnConnectTrailer += SoftMoveCamera;

                //Split screen logic
                if (GameController.SplitScreen)
                {
                    var rect = MainCamera.rect;
                    rect.size = CameraRectSize;
                    if (GameController.PlayerCar1 == Car)
                    {
                        rect.position = CameraRectPosP1;
                        PlayerIndex = 0;
                    }
                    else
                    {
                        rect.position = CameraRectPosP2;
                        PlayerIndex = 1;
                    }

                    MainCamera.rect = rect;
                }
            }

            return IsInitialized;
        }

        bool СameraRotatedManually
        {
            get { return CurrentDistanceAfterMouseMove > 0; }
        }

        protected void Awake ()
        {
            CameraPresets.ForEach (p => p.Init ());

            ActivePresetIndex = 0;
            UpdateActiveCamera (fastCameraRotation: true);
        }

        private IEnumerator Start ()
        {
            if (TargetVehicle && !IsInitialized)
            {
                Initialize (TargetVehicle);
            }

            //Waiting for initialization.
            while (Vehicle == null)
            {
                yield return null;
            }
           
            PrevTargetPoint = TargetPoint;

            transform.position = TargetPoint;
            
            TargetHorizontalRotation = Quaternion.LookRotation(Vehicle.transform.TransformDirection(Vector3.forward).ZeroHeight());
            HorizontalRotation.rotation = TargetHorizontalRotation;

            VerticalRotation.localRotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;

            Vehicle.ResetVehicleAction += OnResetCar;

            //Waiting for control initialization.
            while (Car.CarControl == null)
            {
                yield return null;
            }

            UserControl = Car.CarControl as UserInput;
            if (UserControl != null)
            {
                UserControl.OnChangeViewAction += SetNextCamera;
            }

            ActivePresetIndex = PlayerPrefs.GetInt ("CameraIndex" + PlayerIndex, 0);
            UpdateActiveCamera (fastCameraRotation: true);
        }

        private void FixedUpdate ()
        {
            var currentSpeed = Car.CurrentSpeed;
            CarSpeedDelta = Mathf.Lerp (CarSpeedDelta, currentSpeed - PrevCarSpeed, ActivePreset.GForceLerp);
            PrevCarSpeed = currentSpeed;
        }

        private void LateUpdate ()
        {
            if (ActivePreset.EnableRotation)
            {
                Vector2 mouseDelta = Vector2.zero;
                if (UserControl != null)
                {
                    mouseDelta = UserControl.ViewDelta;
                    if (Gamepad.current != null && UserControl.ViewDeltaDevice == Gamepad.current)
                    {
                        ManualRotation = !Mathf.Approximately (mouseDelta.sqrMagnitude, 0);
                    }
                    else
                    {
                        ManualRotation = UserControl.ManualCameraRotation;
                    }
                }

                if (ManualRotation)
                {
                    //Manual camera control.
                    if (!СameraRotatedManually)
                    {
                        TargetVerticalRotation = 0;
                    }
                    TargetManualRotationAngle += mouseDelta.x;
                    TargetVerticalRotation -= mouseDelta.y;

                    TargetVerticalRotation = Mathf.Clamp (TargetVerticalRotation, ActivePreset.MinVerticalAngle, ActivePreset.MaxVerticalAngle);

                    CurrentDistanceAfterMouseMove = DistanceAfterMouseMove;
                }
                else if (!СameraRotatedManually)
                {
                    //Automatic camera control.
                    var pos = transform.position.ZeroHeight ();
                    var target = TargetPoint.ZeroHeight ();

                    if ((target - pos).sqrMagnitude >= SqrMinDistance)
                    {
                        TargetVerticalRotation = Quaternion.LookRotation (TargetPoint - transform.position, Vector3.right).eulerAngles.x;
                        if (TargetVerticalRotation >= 180)
                        {
                            TargetVerticalRotation -= 360;
                        }
                        else if (TargetVerticalRotation <= -180)
                        {
                            TargetVerticalRotation += 360;
                        }

                        TargetVerticalRotation = TargetVerticalRotation.Clamp (ActivePreset.MinVerticalAngle, ActivePreset.MaxVerticalAngle);

                        TargetHorizontalRotation = Quaternion.LookRotation (target - pos, Vector3.up);
                        if (Vehicle.VehicleIsGrounded && Vehicle.VelocityAngle.Abs() < 120 && Vehicle.VelocityAngle.Abs() > 1)
                        {
                            //Turn the camera towards drift. 
                            TargetHorizontalRotation *= Quaternion.AngleAxis (-Vehicle.VelocityAngle * ActivePreset.AdditionalRotationMultiplier, Vector3.up);
                        }
                    }
                    TargetManualRotationAngle = HorizontalRotation.eulerAngles.y;
                    CurrentManualRotationAngle = TargetManualRotationAngle;
                }
                else
                {
                    //Counter of the distance traveled by the car.
                    CurrentDistanceAfterMouseMove -= Vehicle.CurrentSpeed * Time.deltaTime;
                }
            }
            else
            {
                TargetHorizontalRotation = Quaternion.identity;
                TargetVerticalRotation = 0;
            }

            //Applying camera movement and rotation.
            if (СameraRotatedManually)
            {
                CurrentManualRotationAngle = Mathf.Lerp (CurrentManualRotationAngle, TargetManualRotationAngle, Time.deltaTime * ActivePreset.SetRotationSpeed);
                HorizontalRotation.rotation = Quaternion.AngleAxis (CurrentManualRotationAngle, Vector3.up);
            }
            else
            {
                HorizontalRotation.rotation = Quaternion.Lerp (HorizontalRotation.rotation, TargetHorizontalRotation, Time.deltaTime * ActivePreset.SetRotationSpeed);
            }
            VerticalRotation.localRotation =
                    Quaternion.Lerp (VerticalRotation.localRotation, Quaternion.Euler (TargetVerticalRotation, 0, 0), Time.deltaTime * ActivePreset.SetRotationSpeed);

            //SuperSmoothLerp to smooth the movement with an unstable FPS.
            SetCameraSpeed = Mathf.Lerp (SetCameraSpeed, СameraRotatedManually? ActivePreset.RotatedManuallySetPositionSpeed: ActivePreset.SetPositionSpeed, Time.deltaTime * 5);
            if (Time.deltaTime > 0)
            {
                if (ActivePreset.EnableVelocityOffset)
                {
                    transform.position = VectorExtentions.SuperSmoothLerp (transform.position, PrevTargetPoint, TargetPoint, Time.deltaTime, SetCameraSpeed);
                }
                else
                {
                    transform.position = TargetPoint;
                }
            }

            PrevTargetPoint = TargetPoint;

            var targetFov = Mathf.Lerp(ActivePreset.StandardFOV, ActivePreset.BoostFOV, Car.InBoost? Car.CurrentAcceleration: 0);
            if (GameController.SplitScreen)
            {
                targetFov *= ActivePreset.SplitScreenFOVMultiplayer;
            }
            MainCamera.fieldOfView = Mathf.Lerp (MainCamera.fieldOfView, targetFov, ActivePreset.ChangeFovSpeed * Time.deltaTime);

            if (SoftMoveCameraCoroutine == null)
            {
                if (!CheckObstacles ())
                {
                    ApplyGForce ();
                }
            }

            UpdateEffects ();
        }

        private void OnDestroy ()
        {
            if (UserControl != null)
            {
                UserControl.OnChangeViewAction -= SetNextCamera;
            }

            if (Car)
            {
                Car.OnConnectTrailer -= SoftMoveCamera;
                Vehicle.ResetVehicleAction -= OnResetCar;
            }
        }

        void UpdateEffects ()
        {
            if (SpeedWindSource != null)
            {
                var curentSpeedNorm = Mathf.InverseLerp (WindSoundStartSpeed, WindSoundMaxSpeed, Car.CurrentSpeed);
                if (curentSpeedNorm > 0 && !SpeedWindSource.isPlaying)
                {
                    SpeedWindSource.Play ();
                }
                SpeedWindSource.volume = curentSpeedNorm;
                SpeedWindSource.pitch = Mathf.Lerp (WindSoundStartPitch, WindSoundMaxPitch, curentSpeedNorm);
            }

            if (ActivePreset.EnableShake)
            {
                float shakePower = ((Car.CurrentSpeed - ActivePreset.MinSpeedForStartShake) / (ActivePreset.MaxSpeedForMaxShake - ActivePreset.MinSpeedForStartShake)).Clamp();
                if (Car.CurrentSpeed < ActivePreset.MinSpeedForStartShake)
                {
                    TargetShakeCameraPos = Vector3.zero;
                    shakePower = 1;
                }
                else if (CameraShakeTransform.localPosition == TargetShakeCameraPos)
                {

                    TargetShakeCameraPos = new Vector3 (
                        UnityEngine.Random.Range(-ActivePreset.ShakeCameraRadius.x, ActivePreset.ShakeCameraRadius.x) * shakePower,
                        UnityEngine.Random.Range (-ActivePreset.ShakeCameraRadius.y, ActivePreset.ShakeCameraRadius.y) * shakePower,
                        0
                    );
                }

                CameraShakeTransform.localPosition = Vector3.MoveTowards (CameraShakeTransform.localPosition, TargetShakeCameraPos, ActivePreset.ShakeSpeed * shakePower * Time.deltaTime);
            }
            else
            {
                CameraShakeTransform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Switch to the next camera preset.
        /// </summary>
        public void SetNextCamera ()
        {
            ActivePresetIndex = MathExtentions.Repeat (ActivePresetIndex + 1, 0, CameraPresets.Count - 1);
            
            PlayerPrefs.SetInt ("CameraIndex" + PlayerIndex, ActivePresetIndex);

            SoftMoveCamera ();
        }

        public void SoftMoveCamera ()
        {
            UpdateActiveCamera (fastCameraRotation: false);
            if (SoftMoveCameraCoroutine != null)
            {
                StopCoroutine (SoftMoveCameraCoroutine);
            }

            SoftMoveCameraCoroutine = StartCoroutine (DoSoftMoveCamera ());
        }

        public void UpdateActiveCamera (bool fastCameraRotation)
        {
            ActivePreset = CameraPresets[ActivePresetIndex];

            SqrMinDistance = ActivePreset.MinDistanceForRotation * ActivePreset.MinDistanceForRotation;

            if (fastCameraRotation)
            {
                if (ActivePreset.EnableRotation)
                {
                    if (!Vehicle)
                    {
                        TargetHorizontalRotation = Quaternion.identity;
                    }
                    else
                    {
                        TargetHorizontalRotation = Quaternion.LookRotation (Vehicle.transform.TransformDirection (Vector3.forward).ZeroHeight ());
                    }
                    HorizontalRotation.rotation = TargetHorizontalRotation;

                    TargetVerticalRotation = 0;
                    VerticalRotation.localRotation = Quaternion.identity;
                }

                float carSize = 0;
                if (Car)
                {
                    carSize = Car.Size;
                    if (Car.ConnectedTrailer)
                    {
                        carSize += Car.ConnectedTrailer.Size;
                    }
                }

                CameraViewTransform.localPosition = ActivePreset.GetLocalPosition(carSize);
                CameraViewTransform.localRotation = ActivePreset.GetLocalRotation (carSize);
                LocalCameraPos = CameraViewTransform.localPosition;
                RayDistance = CameraViewTransform.localPosition.magnitude;
            }
        }

        /// <summary>
        /// Smooth camera movement between presets
        /// </summary>
        IEnumerator DoSoftMoveCamera ()
        {
            Transform camTR = CameraViewTransform;
            float carSize = Car.Size;
            if (Car.ConnectedTrailer)
            {
                carSize += Car.ConnectedTrailer.Size;
            }

            var targePos = ActivePreset.GetLocalPosition (carSize);
            var targetRot = ActivePreset.GetLocalRotation (carSize);
            Vector3 camPos = camTR.localPosition;
            Quaternion camRot = camTR.localRotation;

            while (camPos != targePos || camRot != targetRot)
            {
                camPos = Vector3.Lerp (camPos, targePos, Time.deltaTime * ChangeCameraSpeed);
                camRot = Quaternion.Lerp (camRot, targetRot, Time.deltaTime * ChangeCameraSpeed);

                camTR.localPosition = camPos;
                camTR.localRotation = camRot;

                LocalCameraPos = camTR.localPosition;
                RayDistance = camTR.localPosition.magnitude + DistanceToObstacle;

                if (!CheckObstacles ())
                {
                    ApplyGForce ();
                }

                yield return new WaitForEndOfFrame();
            }

            RayDistance = camTR.localPosition.magnitude;
            SoftMoveCameraCoroutine = null;
        }

        bool CheckObstacles ()
        {
            var position = transform.position;
            var direction = (CameraViewTransform.position - transform.position).normalized;
            if (Physics.Raycast(position, direction, out RayHit, RayDistance, ObstacleMask)) 
            {
                CameraViewTransform.position = Vector3.MoveTowards (RayHit.point, transform.position, DistanceToObstacle);
                return true;
            }
            else
            {
                CameraViewTransform.localPosition = LocalCameraPos;
                return false;
            }
        }

        void ApplyGForce ()
        {
            if (ActivePreset.EnableGForceOffset)
            {
                var localPos = CameraViewTransform.localPosition;
                localPos.z -= CarSpeedDelta > 0? (CarSpeedDelta * ActivePreset.AccelerationGForceMultiplier): (CarSpeedDelta * ActivePreset.BrakeGForceMultiplier);
                CameraViewTransform.localPosition = localPos;
            }
        }

        /// <summary>
        /// Instant change of position and rotation.
        /// </summary>
        void OnResetCar ()
        {
            transform.position = TargetPoint;
            if (ActivePreset.EnableRotation)
            {
                UpdateActiveCamera (fastCameraRotation: true);
            }
        }

        void OnStartRotateCamera (Vector2 pos)
        {
            ManualRotation = true;
        }

        void OnEndRotateCamera (Vector2 pos)
        {
            ManualRotation = false;
        }

        private void OnDrawGizmosSelected ()
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere (TargetPoint, 1);

            Gizmos.color = Color.white;
        }

        [System.Serializable]
        public class CameraPreset
        {
#pragma warning disable 0649

            [SerializeField] string PresetName;                 //To display the name in the editor
            [Header("Dependence on the size of the car")]
            [SerializeField] Transform MinCameraLocalPosition;  //Parent fo camera position, destroyed after initialization.
            [SerializeField] Transform MaxCameraLocalPosition;  //Parent fo camera position, destroyed after initialization.

#pragma warning restore 0649

            public float MinTargetVehicleSize = 5f;
            public float MaxTargetVehicleSize = 40f;

            [Header("Move Settings")]
            public bool EnableGForceOffset;
            [ShowInInspectorIf("EnableGForceOffset")] public float AccelerationGForceMultiplier = 20;       //The multiplier to move the camera (Back) when accelerating.
            [ShowInInspectorIf("EnableGForceOffset")] public float BrakeGForceMultiplier = 10;              //The force to move the camera (Forward) when braking.
            [ShowInInspectorIf("EnableGForceOffset")] public float GForceLerp = 0.01f;                      //Offset interpolation for smoothness.

            public bool EnableVelocityOffset = true;
            [ShowInInspectorIf("EnableVelocityOffset")] public float SetPositionSpeed = 10;                 //Change position speed.
            [ShowInInspectorIf("EnableVelocityOffset")] public float RotatedManuallySetPositionSpeed = 50;  //Change position speed when Camera rotated manually.
            [ShowInInspectorIf("EnableVelocityOffset")] public float VelocityMultiplier;                    //Velocity of car multiplier (To predict the tracking position).

            [Header("Rotation Settings")]
            public bool EnableRotation;
            public float MinVerticalAngle = -15;
            public float MaxVerticalAngle = 40;
            public float MinDistanceForRotation = 0.1f;         //Min distance for potation, To avoid uncontrolled rotation.
            public float SetRotationSpeed = 5;                  //Change rotation speed.
            public float AdditionalRotationMultiplier = 0.5f;

            [Header("FOV (Boost) Settings")]
            public float StandardFOV = 60;
            public float BoostFOV = 75;
            public float ChangeFovSpeed = 5;
            public float SplitScreenFOVMultiplayer = 0.66f;

            [Header("Shake Settings")]
            public bool EnableShake = true;
            [ShowInInspectorIf("EnableShake")] public Vector2 ShakeCameraRadius = new Vector3 (0.08f, 0.08f);
            [ShowInInspectorIf("EnableShake")] public float ShakeSpeed = 1;
            [ShowInInspectorIf("EnableShake")] public float MinSpeedForStartShake = 15;
            [ShowInInspectorIf("EnableShake")] public float MaxSpeedForMaxShake = 60;

            Vector3 GetMinLocalPosition;
            Quaternion GetMinLocalRotation;

            Vector3 GetMaxLocalPosition;
            Quaternion GetMaxLocalRotation;

            public void Init ()
            {
                GetMinLocalPosition = MinCameraLocalPosition.localPosition;
                GetMinLocalRotation = MinCameraLocalPosition.localRotation;

                GameObject.Destroy (MinCameraLocalPosition.gameObject);

                if (MaxCameraLocalPosition)
                {
                    GetMaxLocalPosition = MaxCameraLocalPosition.localPosition;
                    GetMaxLocalRotation = MaxCameraLocalPosition.localRotation;
                    GameObject.Destroy (MaxCameraLocalPosition.gameObject);
                }
                else
                {
                    GetMaxLocalPosition = GetMinLocalPosition;
                    GetMaxLocalRotation = GetMinLocalRotation;
                }
            }

            public Vector3 GetLocalPosition (float targetSize)
            {
                targetSize = targetSize.Clamp (MinTargetVehicleSize, MaxTargetVehicleSize);
                var t = (targetSize - MinTargetVehicleSize) / (MaxTargetVehicleSize - MinTargetVehicleSize);
                return Vector3.Lerp (GetMinLocalPosition, GetMaxLocalPosition, t);
            }

            public Quaternion GetLocalRotation (float targetSize)
            {
                targetSize = targetSize.Clamp (MinTargetVehicleSize, MaxTargetVehicleSize);
                var t = (targetSize - MinTargetVehicleSize) / (MaxTargetVehicleSize - MinTargetVehicleSize);
                return Quaternion.Lerp (GetMinLocalRotation, GetMaxLocalRotation, t);
            }
        }
    }
}
