using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace PG
{
    /// <summary>
    /// Wrapper for WheelCollider. The current slip, temperature, surface, etc. is calculated.
    /// </summary>
    [RequireComponent (typeof (WheelCollider))]
    public class Wheel :MoveableDO
    {
        [Range(-1f, 1f)]
        public float SteerPercent;                  //Percentage of wheel turns, 1 - maximum possible turn CarController.Steer.MaxSteerAngle, -1 negative wheel turn (For example, to turn the rear wheels).
        public bool DriveWheel;
        public float MaxBrakeTorque;
        public bool HandBrakeWheel;
        public Transform WheelView;                 //The object of which takes the position and rotation of the wheel.
        public Transform BrakeSupport;              //The object which occupies rotation only along the Y axis of the wheel.
        public float MaxVisualDamageAngle = 5f;     //The maximum offset angle for children to visualize damage.

        public float RPM { get { return WheelCollider.rpm; } }
        public float CurrentMaxSlip { get { return Mathf.Max (CurrentForwardSlip, CurrentSidewaysSlip); } }
        public float CurrentForwardSlip { get; private set; }
        public float CurrentSidewaysSlip { get; private set; }
        public float SlipNormalized { get; private set; }
        public float WheelTemperature { get; private set; }             //Temperature for visualizing tire smoke.
        public bool HasForwardSlip { get { return CurrentForwardSlip > WheelCollider.forwardFriction.asymptoteSlip; } }
        public bool HasSideSlip { get { return CurrentSidewaysSlip > WheelCollider.sidewaysFriction.asymptoteSlip; } }
        public WheelHit GetHit { get { return Hit; } }
        public Vector3 HitPoint { get; private set; }
        public bool IsGrounded { get { return !IsDead && WheelCollider.isGrounded; } }
        public bool StopEmitFX { get; set; }
        public float Radius { get { return WheelCollider.radius; } }
        public Vector3 LocalPositionOnAwake { get; private set; }       //For CarState
        
        public bool IsSteeringWheel { get { return !Mathf.Approximately (0, SteerPercent); } }

        GroundConfig _CurrentGroundConfig;
        public GroundConfig CurrentGroundConfig         //When the ground changes, the grip of the wheels changes.
        { 
            get 
            { 
                return _CurrentGroundConfig; 
            }
            set
            {
                if (_CurrentGroundConfig != value)
                {
                    _CurrentGroundConfig = value;
                    if (_CurrentGroundConfig != null)
                    {
                        var forwardFriction = WheelCollider.forwardFriction;
                        forwardFriction.stiffness = _CurrentGroundConfig.WheelStiffness;
                        WheelCollider.forwardFriction = forwardFriction;

                        var sidewaysFriction = WheelCollider.sidewaysFriction;
                        sidewaysFriction.stiffness = _CurrentGroundConfig.WheelStiffness;
                        WheelCollider.sidewaysFriction = sidewaysFriction;
                    }
                }
            }
        }

        protected Transform[] ViewChilds;
        protected WheelCollider WheelCollider;

        [System.NonSerialized]
        public Vector3 Position;
        [System.NonSerialized]
        public Quaternion Rotation;

        protected VehicleController Vehicle;
        WheelHit Hit;
        protected GroundConfig DefaultGroundConfig { get { return GroundDetection.GetDefaultGroundConfig; } }

        const float TemperatureChangeSpeed = 0.1f;

        public override void Awake ()
        {
            Vehicle = GetComponentInParent<VehicleController> ();
            if (Vehicle == null)
            {
                Debug.LogError ("[Wheel] Parents without CarController");
                Destroy (this);
            }

            WheelCollider = GetComponent<WheelCollider> ();
            WheelCollider.ConfigureVehicleSubsteps (40, 13, 8);

            LocalPositionOnAwake = transform.localPosition;

            InitDamageObject ();

            ViewChilds = new Transform[WheelView.childCount];
            for (int i = 0; i < ViewChilds.Length; i++)
            {
                ViewChilds[i] = WheelView.GetChild (i);
            }

            CurrentGroundConfig = DefaultGroundConfig;

            Vehicle.ResetVehicleAction += OnResetAction;
        }

        /// <summary>
        /// Update gameplay logic.
        /// </summary>
        public void FixedUpdate ()
        {
            float targetTemperature = 0;
            if (WheelCollider.GetGroundHit (out Hit))
            {
                //Calculation of the current friction.
                var prevForward = CurrentForwardSlip;
                var prevSide = CurrentSidewaysSlip;

                CurrentForwardSlip = (prevForward + Mathf.Abs (Hit.forwardSlip)) / 2;
                CurrentSidewaysSlip = (prevSide + Mathf.Abs (Hit.sidewaysSlip)) / 2;

                HitPoint = Hit.point;

                float forwardNormalized = ((CurrentForwardSlip - WheelCollider.forwardFriction.extremumSlip) /
                                        (WheelCollider.forwardFriction.asymptoteSlip - WheelCollider.forwardFriction.extremumSlip)).Clamp();
                float sidewayNormalized = ((CurrentSidewaysSlip - WheelCollider.sidewaysFriction.asymptoteSlip) /
                                        (WheelCollider.sidewaysFriction.asymptoteSlip - WheelCollider.sidewaysFriction.extremumSlip)).Clamp();

                SlipNormalized = forwardNormalized > sidewayNormalized ? forwardNormalized : sidewayNormalized;

                //Determining the type of surface under the wheel.
                var groundEntity = GroundDetection.GetGroundEntity(Hit.collider.gameObject);
                GroundConfig groundConfig = DefaultGroundConfig;

                if (groundEntity != null)
                {
                    groundConfig = groundEntity.GetGroundConfig (Hit.point);
                }

                targetTemperature = HasForwardSlip || HasSideSlip ? 1 : 0;
                CurrentGroundConfig = groundConfig;
            }
            else
            {
                CurrentForwardSlip = 0;
                CurrentSidewaysSlip = 0;
                SlipNormalized = 0;
                CurrentGroundConfig = DefaultGroundConfig;
            }

            WheelTemperature = Mathf.MoveTowards (WheelTemperature, targetTemperature, Time.fixedDeltaTime * TemperatureChangeSpeed);
        }

        /// <summary>
        /// Update visual logic (Transform).
        /// </summary>
        public virtual void Update ()
        {
            if (Vehicle.VehicleIsVisible)
            {
                WheelCollider.GetWorldPose (out Position, out Rotation);
                WheelView.position = Position;
                WheelView.rotation = Rotation;
                if (BrakeSupport)
                {
                    BrakeSupport.position = Position;
                    BrakeSupport.transform.localRotation = Quaternion.AngleAxis (WheelCollider.steerAngle, Vector3.up);
                }
            }
        }

        /// <summary>
        /// Transfer torque to the wheels.
        /// </summary>
        /// <param name="motorTorque">Motor torque</param>
        /// <param name="forceSetTroque">Force torque transfer ignoring the DriveWheel flag.</param>
        public void SetMotorTorque (float motorTorque, bool forceSetTroque = false)
        {
            if (DriveWheel || forceSetTroque)
            {
                WheelCollider.motorTorque = motorTorque;
            }
        }

        public void SetSteerAngle (float steerAngle)
        {
            if (IsSteeringWheel)
            {
                WheelCollider.steerAngle = steerAngle * SteerPercent;
            }
        }

        public void SetHandBrake (bool handBrake)
        {
            if (HandBrakeWheel && handBrake)
            {
                WheelCollider.brakeTorque = MaxBrakeTorque;
            }
            else
            {
                WheelCollider.brakeTorque = 0;
            }
        }

        public void SetBrakeTorque (float brakeTorque)
        {
            WheelCollider.brakeTorque = brakeTorque * MaxBrakeTorque;
        }

        /// <summary>
        /// Dealing damage. At full damage, the wheel is separated from the car.
        /// </summary>
        public override void SetDamage (float damage)
        {
            damage = GetClampedDamage (damage);
            base.SetDamage (damage);

            var rotation = Quaternion.AngleAxis(MaxVisualDamageAngle * (damage / InitHealth), Vector3.up);
            foreach (var viewChild in ViewChilds)
            {
                viewChild.localRotation *= rotation;
            }
        }

        void OnResetAction ()
        {
            CurrentForwardSlip = 0;
            CurrentSidewaysSlip = 0;
            SlipNormalized = 0;
            CurrentGroundConfig = DefaultGroundConfig;
        }

        /// <summary>
        /// Detach wheel, enable collider and Rigidbody.
        /// </summary>
        public override void DoDeath ()
        {
            base.DoDeath ();

            CurrentForwardSlip = 0;
            CurrentSidewaysSlip = 0;
            SlipNormalized = 0;
            Hit = new WheelHit ();

            enabled = false;
            WheelCollider.enabled = false;
            this.enabled = false;

            WheelView.parent = null;
            var meshCollider = WheelView.GetComponentInChildren<MeshCollider>(true);
            if (meshCollider)
            {
                meshCollider.enabled = true;
            }

            var wheelRB = WheelView.gameObject.AddComponent<Rigidbody> ();
            wheelRB.mass = WheelCollider.mass;
        }
    }
}
